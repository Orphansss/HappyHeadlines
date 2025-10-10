using ArticleService.Application.Interfaces; // IArticleService, IArticleCache
using ArticleService.Domain.Entities;
using ArticleService.Infrastructure;         // ArticleDbContext
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ArticleService.Application.Services;

public sealed class ArticleService(ArticleDbContext db, IArticleCache cache) : IArticleService
{
    public async Task<Article> CreateAsync(Article input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
            throw new ArgumentException("Title is required.", nameof(input.Title));

        // ensure EF generates the key
        input.Id = 0;

        // default PublishedAt if not set
        if (input.PublishedAt == default)
            input.PublishedAt = DateTimeOffset.UtcNow;

        Log.Information("Creating Article with ArticleId: {ArticleId}, AuthorId: {AuthorId}, Title: {Title}",
            input.Id, input.AuthorId, input.Title);

        db.Articles.Add(input);
        await db.SaveChangesAsync(ct);

        // write-through: cache the freshly created article
        await cache.SetArticleAsync(input, ct);

        Log.Information("Article created with ArticleId: {ArticleId}", input.Id);
        return input;
    }

    // Requirement-only: list endpoint queries DB (no list caching needed)
    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default) =>
        await db.Articles
            .AsNoTracking()
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(ct);

    // Cache-aside per-article read
    public async Task<Article?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var (cached, hit) = await cache.TryGetArticleAsync(id, ct);
        if (hit)
        {
            Log.Debug("Article {ArticleId} cache HIT", id);
            return cached;
        }

        Log.Debug("Article {ArticleId} cache MISS — querying database", id);

        var dbItem = await db.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (dbItem is not null)
            await cache.SetArticleAsync(dbItem, ct);

        return dbItem;
    }

    public async Task<Article?> UpdateAsync(int id, Article input, CancellationToken ct = default)
    {
        var existing = await db.Articles.FindAsync([id], ct);
        if (existing is null) return null;

        if (!string.IsNullOrWhiteSpace(input.Title))
            existing.Title = input.Title.Trim();

        existing.Summary = input.Summary;   // null allowed
        existing.Content = input.Content;   // null allowed

        await db.SaveChangesAsync(ct);

        // write-through: refresh cache
        await cache.SetArticleAsync(existing, ct);
        Log.Information("Article {ArticleId} updated and cache refreshed", id);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Articles.FindAsync([id], ct);
        if (existing is null) return false;

        db.Articles.Remove(existing);
        await db.SaveChangesAsync(ct);

        // invalidate cache
        await cache.InvalidateArticleAsync(id, ct);
        Log.Information("Article {ArticleId} deleted and cache invalidated", id);

        return true;
    }
}
