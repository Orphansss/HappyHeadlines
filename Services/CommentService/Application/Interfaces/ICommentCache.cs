using CommentService.Domain.Entities;

namespace CommentService.Application.Interfaces;
public interface ICommentCache
{
    Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SetByIdAsync(Comment comment, CancellationToken ct = default);
    Task RemoveByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Comment>?> GetAllAsync(CancellationToken cc = default);
    Task SetAllAsync (IReadOnlyList<Comment> comments, CancellationToken ct = default);
    Task RemoveAllAsync(CancellationToken ct = default);
}