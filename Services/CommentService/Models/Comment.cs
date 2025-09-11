namespace CommentService.Models;

public class Comment
{
    public int Id { get; set; }
    public string Author { get; set; } // Might be unnecessary - full name of the author
    public string Content { get; set; } // Content of the comment
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow; // Timestamp for publishing comment
    // public int ArticleId { get; set; } // Not foreign key - placeholder for potential tags
}