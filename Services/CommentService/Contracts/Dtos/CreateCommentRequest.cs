namespace CommentService.Contracts.Dtos;

public record CreateCommentRequest(int AuthorId, string Content);
