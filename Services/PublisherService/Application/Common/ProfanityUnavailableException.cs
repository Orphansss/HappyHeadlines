using System;

namespace PublisherService.Application.Common;

/// <summary>
/// Signals that ProfanityService is unavailable (timeout/circuit/network).
/// Controllers map this to 503 Service Unavailable.
/// </summary>
public sealed class ProfanityUnavailableException : Exception
{
    public ProfanityUnavailableException(string? message = null, Exception? inner = null)
        : base(message ?? "Profanity service is unavailable.", inner) { }
}