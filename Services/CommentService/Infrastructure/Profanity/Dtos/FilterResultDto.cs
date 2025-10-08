namespace CommentService.Infrastructure.Profanity.Dtos;

public record FilterResultDto(string CleanedText, bool HadProfanity);
