using CommentService.Models;

namespace CommentService.Interfaces
{
    public interface ICommentService
    {
        Task<Comment> CreateComment(Comment comment, CancellationToken ct = default);
        Task<IEnumerable<Comment>> GetCommentsForArticle(int articleId, CancellationToken ct = default);
        Task<Comment?> GetCommentById(int id, CancellationToken ct = default);
        Task<Comment?> UpdateComment(int id, Comment comment, CancellationToken ct = default);
        Task<bool> DeleteComment(int id, CancellationToken ct = default);
    }

}