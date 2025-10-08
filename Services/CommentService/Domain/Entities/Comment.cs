namespace CommentService.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public int AuthorId { get; set; }    
    public string Content { get; set; } 
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow; 
}