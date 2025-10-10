namespace CommentService.Models;

public class Comment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public int AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}