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
            // 1) Call ProfanityService (protected by Retry + CircuitBreaker)
            try
            {
                var result = await _profanity.FilterAsync(new FilterRequestDto(comment.Content), ct);
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
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync(ct);
            return comment;
        }

        public async Task<IEnumerable<Comment>> GetComments()
            => await _db.Comments.ToListAsync();

        public async Task<Comment?> GetCommentById(int id)
            => await _db.Comments.FindAsync(id);

        public async Task<Comment?> UpdateComment(int id, Comment comment, CancellationToken ct = default)
        {
            var existing = await _db.Comments.FindAsync(id);
            if (existing == null) return null;

            existing.Content = comment.Content;
            existing.AuthorId  = comment.AuthorId;
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteComment(int id, CancellationToken ct = default)
        {
            var existing = await _db.Comments.FindAsync(id);
            if (existing == null) return false;

            _db.Comments.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
