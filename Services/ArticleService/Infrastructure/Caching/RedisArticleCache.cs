using System.Text.Json;
using ArticleService.Api.Contracts.Dtos;
using ArticleService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ArticleService.Infrastructure.Caching;

public sealed class RedisArticleCache : IArticleCache
{
    private readonly IDatabase _database;
    private readonly ArticleCacheOptions _options;
    private readonly IDbContextFactory<ArticleDbContext> _globalDbFactory;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public RedisArticleCache(IConnectionMultiplexer mux, ArticleCacheOptions options, IDbContextFactory<ArticleDbContext> globalDbFactory)
    {
        _database = mux.GetDatabase();
        _options = options;
        _globalDbFactory = globalDbFactory; // region = Global
    }
    
    private string KeyById(int id) => $"{_options.KeyPrefix}:{id}";
    private string KeyLatest(int count) => $"{_options.KeyPrefix}:latest:{count}";
    private string KeyWarmIndex() => $"{_options.KeyPrefix}:last14days";


    public async Task<ArticleResponse?> TryGetByIdAsync(int id, CancellationToken ct = default)
    {
        var v = await _database.StringGetAsync(KeyById(id));
        if (v.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<ArticleResponse>(v!, JsonOpts);
    }

    public async Task SetByIdAsync(ArticleResponse article, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(article, JsonOpts);
        await _database.StringSetAsync(KeyById(article.Id), json, ttl ?? _options.ArticleTtl);
    }

    public Task RemoveByIdAsync(int id, CancellationToken ct = default)
        => _database.KeyDeleteAsync(KeyById(id));

    public async Task<IReadOnlyList<ArticleResponse>?> TryGetLatestAsync(int count, CancellationToken ct = default)
    {
        var v = await _database.StringGetAsync(KeyLatest(count));
        if (v.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<List<ArticleResponse>>(v!, JsonOpts);
    }

    public async Task SetLatestAsync(int count, IReadOnlyList<ArticleResponse> items, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(items, JsonOpts);
        await _database.StringSetAsync(KeyLatest(count), json, ttl ?? _options.ArticleTtl);
    }

    public async Task RefreshSingleAsync(int id, CancellationToken ct = default)
    {
        // Pull fresh from DB -> to DTO -> set
        await using var db = await _globalDbFactory.CreateDbContextAsync(ct);  // ✅ create DbContext
        var a = await db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) { await RemoveByIdAsync(id, ct); return; }

        var dto = new ArticleResponse(a.Id, a.AuthorId, a.Title, a.Summary, a.Content, a.PublishedAt);
        await SetByIdAsync(dto, _options.ArticleTtl, ct);
    }

    public async Task WarmLast14DaysAsync(CancellationToken ct = default)
    {
        await using var db = await _globalDbFactory.CreateDbContextAsync(ct);  // ✅ create DbContext
        var since = DateTimeOffset.UtcNow.AddDays(-_options.WarmWindowDays);

        // Page by PublishedAt desc
        int page = 0;
        while (true)
        {
            var batch = await db.Articles.AsNoTracking()
                .Where(a => a.PublishedAt >= since)
                .OrderByDescending(a => a.PublishedAt)
                .Skip(page * _options.WarmBatchSize)
                .Take(_options.WarmBatchSize)
                .Select(a => new ArticleResponse(a.Id, a.AuthorId, a.Title, a.Summary, a.Content, a.PublishedAt))
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            // store each with TTL
            var tasks = batch
                .Select(dto =>
                {
                    var json = JsonSerializer.Serialize(dto, JsonOpts);
                    return _database.StringSetAsync(KeyById(dto.Id), json, _options.ArticleTtl);
                })
                .ToArray();                          

            await Task.WhenAll(tasks);

            // warm index (for ops/demo) – use the array overload explicitly
            RedisValue[] ids = batch.Select(b => (RedisValue)b.Id).ToArray();
            _ = await _database.SetAddAsync(KeyWarmIndex(), ids);

            page++;
        }

        // Store timestamp for monitoring
        await _database.StringSetAsync($"{_options.KeyPrefix}:batch_last_run_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    
}