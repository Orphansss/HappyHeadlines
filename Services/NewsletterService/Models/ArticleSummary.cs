namespace NewsletterService.Models
{
    public class ArticleSummary
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public DateTime PublishedAt { get; set; }
    }
}
