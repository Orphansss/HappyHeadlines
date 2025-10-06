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

        public CommentService(CommentDbContext db, IProfanityService profanity)
        {
            _db = db;
            _profanity = profanity;
        }

        public async Task<Comment> CreateComment(Comment comment, CancellationToken ct = default)
        {
            // Sanitize via ProfanityService (CircuitBreaker + retries handled in HttpClient)
            try
            {
                var result = await _profanity.FilterAsync(new FilterRequestDto(comment.Content), ct);
                comment.Content = result.CleanedText;
            }
            catch (BrokenCircuitException ex)      { throw new ProfanityUnavailableException(inner: ex); }
            catch (TaskCanceledException ex)       { throw new ProfanityUnavailableException(inner: ex); }
            catch (HttpRequestException ex)        { throw new ProfanityUnavailableException(inner: ex); }

            // Persist
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync(ct);
            return comment;
        }

        public async Task<IEnumerable<Comment>> GetCommentsForArticle(int articleId, CancellationToken ct = default)
        {
            // Newest first, no tracking (read-only)
            return await _db.Comments
                .AsNoTracking()
                .Where(c => c.ArticleId == articleId)
                .OrderByDescending(c => c.PublishedAt)
                .ToListAsync(ct);
        }

        public async Task<Comment?> GetCommentById(int id, CancellationToken ct = default)
            => await _db.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<Comment?> UpdateComment(int id, Comment comment, CancellationToken ct = default)
        {
            var existing = await _db.Comments.FindAsync([id], ct);
            if (existing is null) return null;

            // Keep ArticleId stable; only update allowed fields
            existing.Content = comment.Content;
            existing.AuthorId  = comment.AuthorId;
            
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteComment(int id, CancellationToken ct = default)
        {
            var existing = await _db.Comments.FindAsync([id], ct);
            if (existing == null) return false;

            _db.Comments.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
