using CommentService.Domain.Entities;

namespace CommentService.Application.Interfaces;

public interface ICommentCache
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SetArticleCommentsAsync(int articleId, IReadOnlyList<Comment> comments, CancellationToken ct = default);
}
