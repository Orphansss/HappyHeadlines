using ArticleService.Application.Interfaces;
using ArticleService.Infrastructure.Caching;
using Serilog;

namespace ArticleService.Infrastructure.Hosted;

public sealed class ArticleCacheWarmupWorker : BackgroundService
{
    private readonly IArticleCache _articleCache;
    private readonly ArticleCacheOptions _options;

    public ArticleCacheWarmupWorker(IArticleCache articleCache, ArticleCacheOptions options)
    {
        _articleCache = articleCache;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddDays(-14);

            try
            {
                Log.Information("ArticleBatch: start range=[{From:u}, {To:u}]", from, now);

                var started = DateTimeOffset.UtcNow;
                // Make WarmLast14DaysAsync return an int (count of items warmed)
                var count = await _articleCache.WarmLast14DaysAsync(stoppingToken);
                var took = DateTimeOffset.UtcNow - started;

                Log.Information("ArticleBatch: prefilled {PrefilledCount} items in {DurationMs} ms",
                    count, took.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "ArticleBatch: warmup failed");
            }

            await Task.Delay(_options.WarmInterval, stoppingToken);
        }
    }
}