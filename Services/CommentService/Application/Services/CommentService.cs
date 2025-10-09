using CommentService.Exceptions;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;
using CommentService.Domain.Entities;
using CommentService.Infrastructure;
using CommentService.Application.Interfaces;
using CommentService.Infrastructure.Profanity.Dtos;
using Serilog;

namespace CommentService.Application.Services
{
    public class CommentService(CommentDbContext db, IProfanityService profanity, ICommentCache cache) : ICommentService
    {

        public async Task<Comment> CreateComment(Comment comment, CancellationToken ct = default)
        {
            // 1) Call ProfanityService (protected by Retry + CircuitBreaker)
            try
            {
                var result = await profanity.FilterAsync(new FilterRequestDto(comment.Content), ct);
                comment.Content = result.CleanedText;     // overwrite with filtered text
            }
            catch (BrokenCircuitException ex)
            {
                // Circuit is OPEN → fail fast (let controller map to 503)
                throw new ProfanityUnavailableException(inner: ex);
            }
            catch (TaskCanceledException ex)
            {
                // Timeout → treat as unavailable
                throw new ProfanityUnavailableException(inner: ex);
            }
            catch (HttpRequestException ex)
            {
                // Network failure → treat as unavailable
                throw new ProfanityUnavailableException(inner: ex);
            }

            // 2) Persist
            db.Comments.Add(comment);
            await db.SaveChangesAsync(ct);

            return comment;
        }

        public async Task<IEnumerable<Comment>> GetComments()
            => await db.Comments.ToListAsync();

        public async Task<Comment?> GetCommentById(int id, CancellationToken ct = default)
        {
            // Try to fetch from cache first
            var cached = await cache.GetByIdAsync(id, ct);
            if (cached is not null)
            {
                Log.Information("Cache hit for CommentId: {CommentId}", id);
                return cached;
            }

            Log.Information("Cache miss for CommentId: {CommentId}", id);

            // Miss: Try to fetch from DB
            var existing = await db.Comments.FindAsync(id);
            if (existing is null) return null;

            // Fetch all comments for that article
            var list = await db.Comments
                .Where(c => c.ArticleId == existing.ArticleId)
                .OrderByDescending(c => c.PublishedAt)
                .ToListAsync(ct);

            // Store all comments for that article in the cache
            await cache.SetArticleCommentsAsync(existing.ArticleId, list, ct);

            Log.Information("Filled Cache with {Count} comments from Article: {ArticleId}", list.Count, existing.ArticleId);
            return existing;
        }


        public async Task<Comment?> UpdateComment(int id, Comment comment, CancellationToken ct = default)
        {
            var existing = await db.Comments.FindAsync(id);
            if (existing == null) return null;

            existing.Content = comment.Content;
            existing.AuthorId  = comment.AuthorId;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteComment(int id, CancellationToken ct = default)
        {
            var existing = await db.Comments.FindAsync(id);
            if (existing == null) return false;

            db.Comments.Remove(existing);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
