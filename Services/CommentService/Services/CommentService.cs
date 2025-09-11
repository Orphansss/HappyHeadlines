using CommentService.Data;
using CommentService.Models;
using CommentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Services
{
    public class CommentService : ICommentService
    {
        private readonly CommentDbContext _db;

        public CommentService(CommentDbContext db)
        {
            _db = db;
        }

        public async Task<Comment> CreateComment(Comment comment)
        {
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
            return comment;
        }

        public async Task<IEnumerable<Comment>> GetComments()
        {
            return await _db.Comments.ToListAsync();
        }

        public async Task<Comment?> GetCommentById(int id)
        {
            return await _db.Comments.FindAsync(id);
        }

        public async Task<Comment?> UpdateComment(int id, Comment comment)
        {
            var existing = await _db.Comments.FindAsync(id);
            if (existing == null) return null;

            existing.Content = comment.Content;
            existing.Author = comment.Author;
            await _db.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteComment(int id)
        {
            var existing = await _db.Comments.FindAsync(id);
            if (existing == null) return false;

            _db.Comments.Remove(existing);
            await _db.SaveChangesAsync();

            return true;
        }
    }
}