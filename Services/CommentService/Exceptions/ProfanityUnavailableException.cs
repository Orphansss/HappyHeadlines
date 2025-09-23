namespace CommentService.Exceptions;

public sealed class ProfanityUnavailableException : Exception
{
    public ProfanityUnavailableException(string? message = null, Exception? inner = null)
        : base(message ?? "Profanity service is unavailable.", inner) { }
}