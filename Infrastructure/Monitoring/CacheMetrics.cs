using Prometheus;  

namespace Monitoring;

/// <summary>
/// Centralized, tiny helper around Prometheus metrics for our caches.
/// Services call these methods at specific points (cache lookup, hit, eviction, batch complete)
/// and the prometheus-net library handles the rest. Values are scraped via GET /metrics.
/// </summary>
public static class CacheMetrics
{
    // COUNTER: how many times we *attempted* to read from a cache.
    // Label `layer` lets us separate "comments" vs "articles" in one metric.
    private static readonly Counter CacheRequests = Metrics.CreateCounter(
        "cache_requests_total",
        "Total number of cache lookups (read attempts).",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // COUNTER: how many times the cache actually returned a value (a hit).
    // Hit ratio = hits / requests (we’ll compute that in Grafana later).
    private static readonly Counter CacheHits = Metrics.CreateCounter(
        "cache_hits_total",
        "Total number of cache hits.",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // COUNTER: number of LRU evictions we performed (only relevant to CommentCache).
    private static readonly Counter CacheEvictions = Metrics.CreateCounter(
        "cache_evictions_total",
        "Total number of cache evictions (LRU).",
        new CounterConfiguration { LabelNames = new[] { "layer" } });

    // GAUGE: a value that can go up/down. We store the *timestamp* (as seconds since epoch)
    // of the last successful Article batch refresh. Useful to see if the worker runs.
    private static readonly Gauge BatchLastRunTs = Metrics.CreateGauge(
        "batch_refresh_last_run_timestamp",
        "Unix timestamp of the last successful batch refresh.",
        new GaugeConfiguration { LabelNames = new[] { "layer" } });

    // --- Public methods the services call ---

    /// <summary>
    /// Call once right before you try to read from the cache.
    /// Example: CacheMetrics.Request("comments");
    /// </summary>
    public static void Request(string layer) => CacheRequests.WithLabels(layer).Inc();

    /// <summary>
    /// Call only if the cache lookup returned data (a hit).
    /// Example: CacheMetrics.Hit("comments");
    /// </summary>
    public static void Hit(string layer)     => CacheHits.WithLabels(layer).Inc();

    /// <summary>
    /// Call when you evict an entry due to LRU (CommentCache only).
    /// Example: CacheMetrics.Evicted("comments");
    /// </summary>
    public static void Evicted(string layer) => CacheEvictions.WithLabels(layer).Inc();

    /// <summary>
    /// Call at the end of a successful batch prefill run (ArticleCache only).
    /// Example: CacheMetrics.SetBatchLastRun("articles", DateTimeOffset.UtcNow);
    /// </summary>
    public static void SetBatchLastRun(string layer, DateTimeOffset when)
        => BatchLastRunTs.WithLabels(layer).Set(when.ToUnixTimeSeconds());
}
