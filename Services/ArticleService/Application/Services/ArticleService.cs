using ArticleService.Api.Contracts.Dtos;
using ArticleService.Api.Contracts.Mappings;
using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using ArticleService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ArticleService.Application.Services;

public sealed class ArticleService(ArticleDbContext db) : IArticleService
{
    public async Task<ArticleResponse> CreateAsync(CreateArticleRequest input, CancellationToken ct = default)
    {
        Log.Information("CreateArticle requested by AuthorId={AuthorId} Title='{Title}'", input.AuthorId, input.Title);

        if (string.IsNullOrWhiteSpace(input.Title))
            throw new ArgumentException("Title is required.", nameof(input.Title));

        var entity = new Article
        {
            AuthorId = input.AuthorId,
            Title = input.Title.Trim(),
            Summary = input.Summary,
            Content = input.Content,
            PublishedAt = DateTimeOffset.UtcNow
        };

        db.Articles.Add(entity);
        await db.SaveChangesAsync(ct);

        Log.Information("Article created ArticleId={ArticleId} AuthorId={AuthorId}", entity.Id, entity.AuthorId);
        return entity.ToResponse();
    }

    public async Task<IReadOnlyList<ArticleResponse>> GetAllAsync(CancellationToken ct = default)
    {
        Log.Debug("GetAllArticles requested");
        var list = await db.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => a.ToResponse())
            .ToListAsync(ct);

        Log.Debug("GetAllArticles returning Count={Count}", list.Count);
        return list;
    }

    public async Task<IReadOnlyList<ArticleResponse>> GetLatestAsync(int count, CancellationToken ct = default)
    {
        Log.Debug("GetLatestArticles requested Count={Count}", count);
        var list = await db.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Take(count)
            .Select(a => a.ToResponse())
            .ToListAsync(ct);

        Log.Debug("GetLatestArticles returning Count={Count}", list.Count);
        return list;
    }

    public async Task<ArticleResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Log.Debug("GetArticleById requested Id={ArticleId}", id);
        var a = await db.Articles.FindAsync([id], ct);

        if (a is null)
        {
            Log.Information("GetArticleById not found Id={ArticleId}", id);
            return null;
        }

        Log.Debug("GetArticleById found Id={ArticleId}", id);
        return a.ToResponse();
    }

    public async Task<ArticleResponse?> UpdateAsync(int id, UpdateArticleRequest input, CancellationToken ct = default)
    {
        Log.Information("UpdateArticle requested Id={ArticleId}", id);
        var a = await db.Articles.FindAsync([id], ct);
        if (a is null)
        {
            Log.Information("UpdateArticle not found Id={ArticleId}", id);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(input.Title)) a.Title = input.Title.Trim();
        if (input.Summary is not null) a.Summary = input.Summary;
        if (input.Content is not null) a.Content = input.Content;

        await db.SaveChangesAsync(ct);
        Log.Information("UpdateArticle succeeded Id={ArticleId}", id);
        return a.ToResponse();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        Log.Information("DeleteArticle requested Id={ArticleId}", id);
        var a = await db.Articles.FindAsync([id], ct);
        if (a is null)
        {
            Log.Information("DeleteArticle not found Id={ArticleId}", id);
            return false;
        }

        db.Articles.Remove(a);
        await db.SaveChangesAsync(ct);
        Log.Information("DeleteArticle succeeded Id={ArticleId}", id);
        return true;
    }

}
