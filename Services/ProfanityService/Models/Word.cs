namespace ProfanityService.Models
{
    public class Word
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    // DTO'er til filter-endpointet
    public record FilterRequest(string Text);
    public record FilterResponse(bool IsClean, string CleanedText, IEnumerable<string> Hits);
}