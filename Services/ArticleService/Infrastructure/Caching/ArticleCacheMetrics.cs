using Prometheus;

namespace ArticleService.Caching;

public static class ArticleCacheMetrics
{
    public static readonly Counter Hits = Metrics.CreateCounter("article_cache_hits_total", "Article cache hits");
    public static readonly Counter Miss = Metrics.CreateCounter("article_cache_misses_total", "Article cache misses");
    public static readonly Counter Warm = Metrics.CreateCounter("article_cache_warmfilled_total", "Articles filled by warmup");

    public static void Register() { /* touching static ctor is enough */ }
}