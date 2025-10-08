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

    // Batch last run (you already had this)
    private static readonly Gauge BatchLastRunTsCompat = Metrics.CreateGauge(
        "batch_refresh_last_run_timestamp",
        "Unix timestamp of the last successful batch refresh.",
        new GaugeConfiguration { LabelNames = new[] { "layer" } });

    // Alias with more common name used in dashboards
    private static readonly Gauge BatchLastRunTs = Metrics.CreateGauge(
        "batch_last_run_timestamp",
        "Unix timestamp of the last successful batch refresh.",
        new GaugeConfiguration { LabelNames = new[] { "layer" } });

    // Batch duration histogram
    private static readonly Histogram BatchDuration = Metrics.CreateHistogram(
        "batch_refresh_duration_seconds",
        "Duration of the batch prefill.",
        new HistogramConfiguration
        {
            LabelNames = new[] { "layer" },
            Buckets = Histogram.ExponentialBuckets(0.05, 2, 10) // 50ms → ~25s
        });

    // --- Public API ---

    public static void Request(string layer) => CacheRequests.WithLabels(layer).Inc();
    public static void Hit(string layer)     => CacheHits.WithLabels(layer).Inc();
    public static void Miss(string layer)    => CacheMisses.WithLabels(layer).Inc();
    public static void Evicted(string layer) => CacheEvictions.WithLabels(layer).Inc();

    /// <summary>Call at the end of a successful batch run.</summary>
    public static void SetBatchLastRun(string layer, DateTimeOffset when)
    {
        var ts = when.ToUnixTimeSeconds();
        BatchLastRunTsCompat.WithLabels(layer).Set(ts);
        BatchLastRunTs.WithLabels(layer).Set(ts);
    }

    /// <summary>
    /// Measure batch duration with a using-pattern:
    /// using var t = CacheMetrics.MeasureBatch(CacheLayers.Articles); ... 
    /// </summary>
    public static IDisposable MeasureBatch(string layer) =>
        BatchDuration.WithLabels(layer).NewTimer();
}
