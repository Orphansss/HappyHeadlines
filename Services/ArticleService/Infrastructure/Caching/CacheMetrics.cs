// ArticleService.Infrastructure/Caching/CacheMetrics.cs
using Prometheus;

namespace ArticleService.Infrastructure.Caching;

public static class CacheMetrics
{
    // cache_hits_total{layer,op}
    public static readonly Counter Hits = Metrics.CreateCounter(
        "cache_hits_total",
        "Total cache hits.",
        new CounterConfiguration { LabelNames = new[] { "layer", "op" } });

    // cache_misses_total{layer,op}
    public static readonly Counter Misses = Metrics.CreateCounter(
        "cache_misses_total",
        "Total cache misses.",
        new CounterConfiguration { LabelNames = new[] { "layer", "op" } });
}
