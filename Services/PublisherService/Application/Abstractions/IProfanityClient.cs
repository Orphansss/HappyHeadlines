using System.Threading;
using System.Threading.Tasks;

namespace PublisherService.Application.Abstractions;

public interface IProfanityClient
{
    /// <summary>
    /// Returns the cleaned text with profanity removed/replaced.
    /// Throws ProfanityUnavailableException if the dependency is unhealthy.
    /// </summary>
    Task<string> FilterAsync(string text, CancellationToken ct = default);
}