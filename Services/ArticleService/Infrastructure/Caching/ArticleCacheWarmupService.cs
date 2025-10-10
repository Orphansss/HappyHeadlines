using ArticleService.Application.Interfaces;
using ArticleService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ArticleService.Infrastructure.Caching
{
    public sealed class ArticleCacheWarmupService(
        ILogger<ArticleCacheWarmupService> log,
        IConfiguration cfg,
        IServiceProvider sp,
        IArticleCache cache) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            log.LogInformation("ArticleCacheWarmupService starting...");

            var interval = TimeSpan.FromMinutes(cfg.GetValue("Cache:WarmupIntervalMinutes", 15));
            var windowDays = cfg.GetValue("Cache:WarmupWindowDays", 14);

            // Initial warmup at startup
            await WarmupCacheAsync(windowDays, stoppingToken);

            // Periodic warmup loop
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(interval, stoppingToken);
                await WarmupCacheAsync(windowDays, stoppingToken);
            }
        }

        private async Task WarmupCacheAsync(int windowDays, CancellationToken ct)
        {
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ArticleDbContext>();

                var since = DateTimeOffset.UtcNow.AddDays(-windowDays);

                var recent = await db.Articles
                    .AsNoTracking()
                    .Where(a => a.PublishedAt >= since)
                    .OrderByDescending(a => a.PublishedAt)
                    .ToListAsync(ct);

                // Cache individual articles
                await Task.WhenAll(recent.Select(a => cache.SetArticleAsync(a, ct)));

                // Cache the latest articles list
                await cache.SetLatestAsync(recent, ct);

                // Optionally cache hot article IDs (if needed for your use case)
                var hotIds = recent.Take(50).Select(a => a.Id);
                await cache.UpsertHotSetAsync(hotIds, ct);

                log.LogInformation("Article cache warmup complete. Cached {Count} articles from the last {Days} days.", 
                    recent.Count, windowDays);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Article cache warmup failed.");
            }
        }
    }
}
