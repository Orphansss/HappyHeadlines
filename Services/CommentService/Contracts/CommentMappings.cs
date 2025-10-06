using CommentService.Contracts.Dtos;
using CommentService.Models;

namespace CommentService.Contracts;

public static class CommentMappings
{
    public static CommentResponse ToResponse(this Comment c) =>
        new(c.Id, c.ArticleId, c.AuthorId, c.Content, c.PublishedAt);

    public static IEnumerable<CommentResponse> ToResponse(this IEnumerable<Comment> list) =>
        list.Select(ToResponse);

    public static Comment ToEntity(this CreateCommentRequest req, int articleId) =>
        new Comment
        {
            ArticleId = articleId,
            AuthorId  = req.AuthorId,
            Content   = req.Content
            // PublishedAt defaults in entity
        };
}