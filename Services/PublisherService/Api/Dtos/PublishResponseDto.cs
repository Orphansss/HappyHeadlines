namespace PublisherService.Api.Dtos;

/// <summary>
/// Response when an article is accepted for publication.
/// </summary>
public sealed record PublishResponseDto(
    int PublicationId,
    DateTimeOffset AcceptedAt,
    string Status = "Queued"
);