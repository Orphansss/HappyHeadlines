namespace PublisherService.Infrastructure.Profanity.Dtos;

public sealed record FilterResultDto(string CleanedText, bool HadProfanity);