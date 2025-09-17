using CommentService.Models;

namespace CommentService.Interfaces
{
    public interface ICommentService
    {
        Task<Comment> CreateComment(Comment comment);
        Task<IEnumerable<Comment>> GetComments();
        Task<Comment?> GetCommentById(int id);
        Task<Comment?> UpdateComment(int id, Comment comment);
        Task<bool> DeleteComment(int id);
    }
}