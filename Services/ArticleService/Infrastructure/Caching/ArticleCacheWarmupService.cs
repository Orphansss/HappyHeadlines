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
            log.LogWarning("ArticleCacheWarmupService.ExecuteAsync ENTERED");

            var interval = TimeSpan.FromMinutes(cfg.GetValue("Cache:WarmupIntervalMinutes", 15));
            var windowDays = cfg.GetValue("Cache:WarmupWindowDays", 14);


            // one-off warmup at startup
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ArticleDbContext>();

                var since = DateTimeOffset.UtcNow.AddDays(-windowDays);

                var recent = await db.Articles
                    .AsNoTracking()
                    .Where(a => a.PublishedAt >= since)
                    .OrderByDescending(a => a.PublishedAt)
                    .ToListAsync(stoppingToken);

                await Task.WhenAll(recent.Select(a => cache.SetArticleAsync(a, stoppingToken)));

                log.LogWarning("Article warmup (startup) complete. Cached {Count} recent articles.", recent.Count);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Article warmup (startup) failed.");
            }

            while (!stoppingToken.IsCancellationRequested)
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
                        .ToListAsync(stoppingToken);

                    // preload cache for all articles in the last 14 days
                    var tasks = recent.Select(a => cache.SetArticleAsync(a, stoppingToken));
                    await Task.WhenAll(tasks);

                    log.LogWarning("Article warmup complete. Cached {Count} recent articles.", recent.Count);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Article warmup failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}
