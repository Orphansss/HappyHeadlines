namespace ArticleService.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Body { get; set; }
        public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
