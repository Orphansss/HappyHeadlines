using Prometheus;

namespace Monitoring;

public sealed class CacheMetrics : ICacheMetrics
{
    private readonly string _serviceName;

    private static readonly Counter Hits = Metrics.CreateCounter(
        "cache_hits_total", "Number of cache hits",
        new CounterConfiguration { LabelNames = new[] { "service", "cache" } });

    private static readonly Counter Misses = Metrics.CreateCounter(
        "cache_misses_total", "Number of cache misses",
        new CounterConfiguration { LabelNames = new[] { "service", "cache" } });

    private static readonly Gauge CacheSize = Metrics.CreateGauge(
        "cache_size", "Current number of items in cache",
        new GaugeConfiguration { LabelNames = new[] { "service", "cache" } });

    private static readonly Counter Evictions = Metrics.CreateCounter(
        "cache_evictions_total", "Number of cache evictions",
        new CounterConfiguration { LabelNames = new[] { "service", "cache" } });

    public CacheMetrics(string serviceName)
    {
        _serviceName = serviceName;
    }

    public void Hit(string cacheName) => Hits.WithLabels(_serviceName, cacheName).Inc();
    public void Miss(string cacheName) => Misses.WithLabels(_serviceName, cacheName).Inc();
    
    public void SetSize(string cacheName, int size) => CacheSize.WithLabels(_serviceName, cacheName).Set(size);
    public void Evict(string cacheName) => Evictions.WithLabels(_serviceName, cacheName).Inc();
}
    