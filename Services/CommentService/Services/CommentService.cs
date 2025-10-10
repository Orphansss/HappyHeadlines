using CommentService.Data;
using CommentService.Models;
using CommentService.Interfaces;
using CommentService.Exceptions;
using CommentService.Profanity.Dtos;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;

namespace CommentService.Services
{
    public class CommentService : ICommentService
    {
        private readonly CommentDbContext _db;
        private readonly IProfanityService _profanity;
        private readonly ICommentCache _cache;

        public CommentService(CommentDbContext db, IProfanityService profanity, ICommentCache cache)
        {
            _db = db;
            _profanity = profanity;
            _cache = cache;
        }

        public async Task<Comment> CreateComment(Comment comment, CancellationToken ct = default)
        {
            // 1) Call ProfanityService (protected by Retry + CircuitBreaker)
            try
            {
                var result = await _profanity.FilterAsync(new FilterRequestDto(comment.Content), ct);
                comment.Content = result.CleanedText;
            }
            catch (BrokenCircuitException ex)
            {
                throw new ProfanityUnavailableException(inner: ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new ProfanityUnavailableException(inner: ex);
            }
            catch (HttpRequestException ex)
            {
                throw new ProfanityUnavailableException(inner: ex);
            }

            // 2) Persist
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync(ct);

            // 3) Invalidate article cache (cache-miss approach: don't populate until requested)
            await _cache.InvalidateArticleAsync(comment.ArticleId, ct);

            return comment;
        }

        public async Task<IEnumerable<Comment>> GetComments()
        {
            return await _db.Comments.ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByArticleId(int articleId, CancellationToken ct = default)
        {
            // Check cache 
            var cached = await _cache.GetCommentsByArticleIdAsync(articleId, ct);
            if (cached != null)
                return cached;

            // Cache miss, populate cache
            var comments = await _db.Comments
                .Where(c => c.ArticleId == articleId)
                .ToListAsync(ct);

            await _cache.SetCommentsByArticleIdAsync(articleId, comments, ct);
            return comments;
        }

        public async Task<Comment?> GetCommentById(int id)
        {
            // Check cache first
            var cached = await _cache.GetByIdAsync(id);
            if (cached != null)
                return cached;

            // Cache miss, populate cache
            var comment = await _db.Comments.FindAsync(id);
            if (comment != null)
                await _cache.SetByIdAsync(comment);

            return comment;
        }

        public async Task<Comment?> UpdateComment(int id, Comment comment, CancellationToken ct = default)
        {
            var existing = await _db.Comments.FindAsync(id);
            if (existing == null) return null;

            existing.Content = comment.Content;
            existing.AuthorId = comment.AuthorId;
            await _db.SaveChangesAsync(ct);

            // Invalidate both individual comment and article cache
            await _cache.RemoveByIdAsync(id, ct);
            await _cache.InvalidateArticleAsync(existing.ArticleId, ct);

            return existing;
        }

        public async Task<bool> DeleteComment(int id, CancellationToken ct = default)
        {
            var existing = await _db.Comments.FindAsync(id);
            if (existing == null) return false;

            _db.Comments.Remove(existing);
            await _db.SaveChangesAsync(ct);

            // Invalidate both individual comment and article cache
            await _cache.RemoveByIdAsync(id, ct);
            await _cache.InvalidateArticleAsync(existing.ArticleId, ct);

            return true;
        }
    }
}