using Prometheus;

namespace Monitoring;

public static class CacheLayers
{
    public const string Articles = "articles";
    public const string Comments = "comments";
}

/// <summary>
/// Centralized helper around Prometheus metrics for cache layers.
/// </summary>
public static class CacheMetrics
{
    // Cache tries
    private static readonly Counter CacheRequests = Metrics.CreateCounter(
        "cache_requests_total",
        "Total number of cache lookups (read attempts).",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // Cache hits
    private static readonly Counter CacheHits = Metrics.CreateCounter(
        "cache_hits_total",
        "Total number of cache hits.",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // Cache misses
    private static readonly Counter CacheMisses = Metrics.CreateCounter(
        "cache_misses_total",
        "Total number of cache misses.",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // LRU evictions (comments only)
    private static readonly Counter CacheEvictions = Metrics.CreateCounter(
        "cache_evictions_total",
        "Total number of cache evictions (LRU).",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // --- Public API ---
    public static void Request(string layer) => CacheRequests.WithLabels(layer).Inc();
    public static void Hit(string layer)     => CacheHits.WithLabels(layer).Inc();
    public static void Miss(string layer)    => CacheMisses.WithLabels(layer).Inc();
    public static void Evicted(string layer, int count = 1)  => CacheEvictions.WithLabels(layer).Inc(count);
}
