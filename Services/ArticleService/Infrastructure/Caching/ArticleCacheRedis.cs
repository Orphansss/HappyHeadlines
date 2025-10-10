using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;
using Monitoring;


namespace ArticleService.Infrastructure.Caching
{
    public sealed class ArticleCacheRedis : IArticleCache
    {
        private readonly IDistributedCache _cache;
        private readonly IDatabase _redis;
        private readonly string _ns;
        private readonly ILogger<ArticleCacheRedis> _log;
        private readonly int _articleTtlMinutes;
        private readonly int _listTtlMinutes;
        private readonly ICacheMetrics _metrics;

        private const string HotSetKey = "articles:hot";
        private const string LatestListKey = "articles:latest:list";
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public ArticleCacheRedis(
            IDistributedCache cache,
            IConnectionMultiplexer mux,
            IConfiguration cfg,
            ILogger<ArticleCacheRedis> log,
            ICacheMetrics metrics) 
        {
            _cache = cache;
            _redis = mux.GetDatabase();
            _ns = cfg.GetValue<string>("Redis:InstanceName") ?? "happy:articles:";
            _log = log;
            _metrics = metrics;  

            _articleTtlMinutes = cfg.GetValue("Cache:PerArticleTtlMinutes", 15);
            _listTtlMinutes = cfg.GetValue("Cache:ListTtlMinutes", 5);
        }

        private string AK(int id) => $"{_ns}article:{id}";
        private string NK(string raw) => $"{_ns}{raw}";

        public async Task<(Article? article, bool hit)> TryGetArticleAsync(int id, CancellationToken ct = default)
        {
            var payload = await _cache.GetStringAsync(AK(id), ct);
            if (payload is null)
            {
                _metrics.Miss("article_by_id");     
                return (null, false);
            }

            _metrics.Hit("article_by_id");          
            return (JsonSerializer.Deserialize<Article>(payload, JsonOpts), true);
        }

        public async Task SetArticleAsync(Article article, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(article, JsonOpts);
            await _cache.SetStringAsync(
                AK(article.Id),
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_articleTtlMinutes)
                },
                ct);
        }

        public Task InvalidateArticleAsync(int id, CancellationToken ct = default)
            => _cache.RemoveAsync(AK(id), ct);

        public async Task<(IReadOnlyList<Article> items, bool hit)> TryGetLatestAsync(int take, CancellationToken ct = default)
        {
            var payload = await _cache.GetStringAsync(NK(LatestListKey), ct);
            if (payload is null)
            {
                _metrics.Miss("article_latest_list");  
                return (Array.Empty<Article>(), false);
            }

            _metrics.Hit("article_latest_list");       
            var all = JsonSerializer.Deserialize<List<Article>>(payload, JsonOpts) ?? [];
            return (all.Take(take).ToList(), true);
        }

        public async Task SetLatestAsync(IReadOnlyList<Article> items, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(items, JsonOpts);
            await _cache.SetStringAsync(
                NK(LatestListKey),
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_listTtlMinutes)
                },
                ct);
        }

        public async Task UpsertHotSetAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            var k = NK(HotSetKey);
            var trans = _redis.CreateTransaction();
            _ = trans.KeyDeleteAsync(k);
            _ = trans.SetAddAsync(k, ids.Select(i => (RedisValue)i).ToArray());
            await trans.ExecuteAsync();
        }
    }
}
