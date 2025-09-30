namespace PublisherService.Infrastructure.Profanity;

public sealed record FilterRequestDto(string Text);
public sealed record FilterResultDto(string CleanedText, bool HadProfanity);