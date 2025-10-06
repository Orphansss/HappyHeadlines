namespace CommentService.Contracts.Dtos;

public record CommentResponse(int Id, int ArticleId, int AuthorId, string Content, DateTimeOffset PublishedAt);
