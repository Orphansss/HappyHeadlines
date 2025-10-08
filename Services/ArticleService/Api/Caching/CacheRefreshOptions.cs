namespace ArticleService.Api.Caching;
public sealed class CacheRefreshOptions
{
    public bool Enabled { get; set; } = true;
    public int WindowDays { get; set; } = 14;
    public double FrequencyHours { get; set; } = 24;
    public int MaxRows { get; set; } = 5000;         
}
