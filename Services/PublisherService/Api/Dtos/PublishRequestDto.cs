using PublisherService.Domain.Entities;

namespace PublisherService.Api.Dtos;

/// <summary>
/// Input payload for publishing an article.
/// </summary>
public sealed record PublishRequestDto(
    int AuthorId,
    string Title,
    string? Summary,
    string Content,
    string Region
);