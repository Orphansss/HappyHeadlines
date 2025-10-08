using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using ArticleService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace ArticleService.Api.Caching;

public sealed class CacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly CacheRefreshOptions _opt;

    public CacheRefreshService(
        IServiceProvider sp,
        IOptions<CacheRefreshOptions> opt)
    {
        _sp = sp;
        _opt = opt.Value;
    }

    /// <summary>
    /// Starts loading recent articles into the cache if enabled.
    /// Starts the timer to repeat the process every FrequencyHours.
    /// Pauses the while loop between each tick from the timer.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_opt.Enabled)
        {
            Log.Information("Cache prewarm is disabled.");
            return;
        }

        await BatchRefreshAsync(ct);

        var period = TimeSpan.FromHours(Math.Max(1, _opt.FrequencyHours));
        using var timer = new PeriodicTimer(period);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await BatchRefreshAsync(ct);
        }
    }

    /// <summary>
    /// Pulls recent articles from the Global DB and puts each of them in the cache.
    /// </summary>
    private async Task BatchRefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var db = RegionDbContextFactory.CreateDbContext("Global", cfg);
            var cache = scope.ServiceProvider.GetRequiredService<IArticleCache>();

            var cutoff = DateTimeOffset.UtcNow.AddDays(-_opt.WindowDays);
            Log.Information("Prewarm cutoff: {Cutoff:u}", cutoff);

            List<Article> recent = await db.Articles
                .Where(a => a.PublishedAt >= cutoff)
                .OrderByDescending(a => a.PublishedAt)
                .Take(_opt.MaxRows)
                .ToListAsync(ct);

            foreach (var a in recent)
                await cache.SetByIdAsync(a, ct);

            Log.Information("Prewarmed {Count} recent articles (last {Days} days).",
                recent.Count, _opt.WindowDays);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cache prewarm encountered an error.");
        }
    }
}
