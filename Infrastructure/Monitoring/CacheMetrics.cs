using Prometheus;

namespace Monitoring;

public sealed class CacheMetrics : ICacheMetrics
{
    private static readonly Counter Hits = Metrics.CreateCounter(
        "cache_hits_total", "Number of cache hits",
        new CounterConfiguration { LabelNames = new[] { "cache" } });

    private static readonly Counter Misses = Metrics.CreateCounter(
        "cache_misses_total", "Number of cache misses",
        new CounterConfiguration { LabelNames = new[] { "cache" } });

    public void Hit(string cacheName) => Hits.WithLabels(cacheName).Inc();
    public void Miss(string cacheName) => Misses.WithLabels(cacheName).Inc();
}
