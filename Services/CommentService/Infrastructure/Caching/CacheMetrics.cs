using Prometheus;

namespace CommentService.Infrastructure.Caching;

public static class CacheMetrics
{
    public static readonly Counter Hits = Metrics.CreateCounter(
        "cache_hits_total",
        "Total cache hits.",
        new CounterConfiguration { LabelNames = new[] { "layer", "op" } });

    public static readonly Counter Misses = Metrics.CreateCounter(
        "cache_misses_total",
        "Total cache misses.",
        new CounterConfiguration { LabelNames = new[] { "layer", "op" } });
}
