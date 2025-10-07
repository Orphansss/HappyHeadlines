using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ArticleService.Domain.Entities;
using ArticleService.Application.Interfaces;

namespace ArticleService.Infrastructure.Caching;

public sealed class ArticleCache(IDistributedCache cache) : IArticleCache
{
    /// <summary>
    /// Gives more options for JsonSerializer
    /// </summary>
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <summary>
    /// Cache entry options for a single article or a list of articles.
    /// How long a aricle should live in the cache.
    /// </summary>
    private static readonly DistributedCacheEntryOptions ItemTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };


    /// <summary>
    /// The name of the keys for articles in the cache.
    /// </summary>
    private static string KeyById(int id) => $"articles:{id}";

    /// <summary>
    /// Get a article from the cache from its Id.
    /// Use JsonSerializer to Deserlize the cached article from bytes to article object.
    /// </summary>
    public async Task<Article?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(KeyById(id), ct);
        return bytes is null ? null : JsonSerializer.Deserialize<Article>(bytes, _json);
    }

    /// <summary>
    /// Remove a article from the cache by its Id.
    /// </summary>
    public Task RemoveByIdAsync(int id, CancellationToken ct = default) =>
        cache.RemoveAsync(KeyById(id), ct);

    /// <summary>
    /// Place a article in the cache.
    /// Use JsonSerializer to Serilize the cached article from article object to byte.
    /// </summary>
    public Task SetByIdAsync(Article article, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(article, _json);
        return cache.SetAsync(KeyById(article.Id), bytes, ItemTtl, ct);
    }
}
