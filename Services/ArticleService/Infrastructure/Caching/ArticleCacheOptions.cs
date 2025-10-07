namespace ArticleService.Infrastructure.Caching;

public sealed class ArticleCacheOptions
{
    public string KeyPrefix { get; init; } = "hh:v1:article";
    public TimeSpan ArticleTtl { get; init; } = TimeSpan.FromMinutes(45);
    public TimeSpan LatestTtl { get; init; } = TimeSpan.FromMinutes(3);
    public int WarmWindowDays { get; init; } = 14;
    public TimeSpan WarmInterval { get; init; } = TimeSpan.FromMinutes(5);
    public int WarmBatchSize { get; init; } = 200;
}