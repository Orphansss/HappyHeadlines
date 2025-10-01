namespace ArticleService.Domain.Entities
{
    public class Article
    {
        public int Id { get; set; }
        public int AuthorId { get; set; }
        public string Title { get; set; }
        public string? Summary { get; set; }
        public string Content { get; set; }
        public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
