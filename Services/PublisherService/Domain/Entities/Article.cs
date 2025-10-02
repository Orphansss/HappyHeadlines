namespace PublisherService.Domain.Entities;

public sealed class Article
{
    public int Id { get; }
    public int AuthorId { get; }
    public string Title { get; }
    public string? Summary { get; }
    public string Content { get; }
    public DateTimeOffset PublishedAt { get; }
    public Region Region { get; set; }

    private Article(int id, int authorId, string title, string? summary, string content, DateTimeOffset publishedAt, Region region)
    {
        Id = id;
        AuthorId = authorId;
        Title = title;
        Summary = summary;
        Content = content;
        PublishedAt = publishedAt;
        Region = region;
    }

    /// <summary>
    /// Factory for a *published* article. Caller passes already-cleaned content.
    /// </summary>
    public static Article CreatePublished(int id, int authorId, string title, string? summary, string cleanedContent, Region region)
    {
        Guard.Positive(id, nameof(id));
        Guard.Positive(authorId, nameof(authorId));
        Guard.NotNullOrWhiteSpace(title, nameof(title));
        Guard.MaxLength(title, 200, nameof(title));
        Guard.MaxLength(summary, 500, nameof(summary));
        Guard.NotNullOrWhiteSpace(cleanedContent, nameof(cleanedContent));

        return new Article(
            id: id,
            authorId: authorId,
            title: title.Trim(),
            summary: string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
            content: cleanedContent,
            region: region,
            publishedAt: DateTimeOffset.UtcNow
        );
    }
}