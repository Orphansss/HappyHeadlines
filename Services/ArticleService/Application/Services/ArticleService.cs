using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using ArticleService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection.Metadata.Ecma335;

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

        Log.Information("Creating Article with ArticleId: {ArticleId}, AuthorId: {AuthorId}, Title: {Title}",input.Id, input.AuthorId, input.Title);

        db.Articles.Add(input);
        await db.SaveChangesAsync(ct);
        
        Log.Information("Article created with ArticleId: {ArticleId}", input.Id);

        return input;
    }

    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default) =>
        await db.Articles
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(ct);

    public async Task<Article?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        //Try to Fetch from Cache
        var cached = await cache.GetByIdAsync(id, ct);
        if (cached is not null)
        {
            Log.Information("Cache hit for ArticleId: {ArticleId}", id);
            return cached;
        }

        //If cache miss, fetch from db
        var existing = await db.Articles.FindAsync([id], ct);
        return existing;
    }

    public async Task<Article?> UpdateAsync(int id, Article input, CancellationToken ct = default)
    {
        var existing = await db.Articles.FindAsync([id], ct);
        if (existing is null) return null;

        if (!string.IsNullOrWhiteSpace(input.Title))
            existing.Title = input.Title.Trim();

        existing.Summary = input.Summary; // null allowed
        existing.Content = input.Content; // consider null/empty rules

        // we keep PublishedAt as-is
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Articles.FindAsync([id], ct);
        if (existing is null) return false;

        db.Articles.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}