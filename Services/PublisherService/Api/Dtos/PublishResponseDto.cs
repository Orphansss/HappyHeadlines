namespace PublisherService.Api.Dtos;

/// <summary>
/// Response when an article is accepted for publication.
/// </summary>
public sealed record PublishResponseDto(
    Guid PublicationId,
    DateTimeOffset AcceptedAt,
    string Status = "Queued"
);