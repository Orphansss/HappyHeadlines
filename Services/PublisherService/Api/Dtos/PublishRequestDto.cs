namespace PublisherService.Api.Dtos;

/// <summary>
/// Input payload for publishing an article.
/// </summary>
public sealed record PublishRequestDto(
    Guid AuthorId,
    string Title,
    string? Summary,
    string Content
);