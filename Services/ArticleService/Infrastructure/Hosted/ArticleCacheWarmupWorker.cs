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
            try
            {
                var started = DateTimeOffset.UtcNow;
                await _articleCache.WarmLast14DaysAsync(stoppingToken);
                var took = DateTimeOffset.UtcNow - started;
                Log.Information("Article warmup completed in {Durationms} ms", took.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Article warmup failed...");
            }
            await Task.Delay(_options.WarmInterval, stoppingToken);
        }
    }
}