namespace PublisherService.Domain.Entities;

public sealed class Article
{
    public Guid Id { get; }
    public Guid AuthorId { get; }
    public string Title { get; }
    public string? Summary { get; }
    public string Content { get; }
    public DateTimeOffset PublishedAt { get; }

    private Article(Guid id, Guid authorId, string title, string? summary, string content, DateTimeOffset publishedAt)
    {
        Id = id;
        AuthorId = authorId;
        Title = title;
        Summary = summary;
        Content = content;
        PublishedAt = publishedAt;
    }

    /// <summary>
    /// Factory for a *published* article. The caller (Application layer)
    /// must pass already-filtered/cleaned content.
    /// </summary>
    public static Article CreatePublished(Guid id, Guid authorId, string title, string? summary, string cleanedContent)
    {
        Guard.NotEmpty(id, nameof(id));
        Guard.NotEmpty(authorId, nameof(authorId));
        Guard.NotNullOrWhiteSpace(title, nameof(title));
        Guard.MaxLength(title, 200, nameof(title));
        Guard.MaxLength(summary, 500, nameof(summary));
        Guard.NotNullOrWhiteSpace(cleanedContent, nameof(cleanedContent));

        return new Article(
            id: id,
            authorId: authorId,
            title: title.Trim(),
            summary: string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
            content: cleanedContent,                            // already profanity-filtered
            publishedAt: DateTimeOffset.UtcNow                  // spec: set on creation
        );
    }
}