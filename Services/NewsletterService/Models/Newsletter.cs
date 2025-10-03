namespace NewsletterService.Models
{
    public class Newsletter
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<ArticleSummary> Articles { get; set; } = new();
    }
}