using CommentService.Models;

namespace CommentService.Interfaces;

public interface ICommentCache
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SetByIdAsync(Comment comment, CancellationToken ct = default);
    Task RemoveByIdAsync(int id, CancellationToken ct = default);

    // Article-scoped operations (for LRU cache)
    Task<IEnumerable<Comment>?> GetCommentsByArticleIdAsync(int articleId, CancellationToken ct = default);
    Task SetCommentsByArticleIdAsync(int articleId, IEnumerable<Comment> comments, CancellationToken ct = default);
    Task InvalidateArticleAsync(int articleId, CancellationToken ct = default);

    // Global operations (optional for compatibility)
    Task<IEnumerable<Comment>?> GetAllAsync(CancellationToken ct = default);
    Task SetAllAsync(IEnumerable<Comment> comments, CancellationToken ct = default);
    Task InvalidateAllAsync(CancellationToken ct = default);
}