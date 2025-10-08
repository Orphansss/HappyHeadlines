using CommentService.Application.Interfaces;
using CommentService.Domain.Entities;

namespace CommentService.Infrastructure.Caching;

public class CommentCache : ICommentCache
{
    public Task<IReadOnlyList<Comment>?> GetAllAsync(CancellationToken cc = default)
    {
        throw new NotImplementedException();
    }

    public Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task SetAllAsync(IReadOnlyList<Comment> comments, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task SetByIdAsync(Comment comment, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}