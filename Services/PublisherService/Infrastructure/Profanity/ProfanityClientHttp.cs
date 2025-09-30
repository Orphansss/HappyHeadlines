using PublisherService.Application.Abstractions;
using PublisherService.Application.Common;
using PublisherService.Infrastructure.Profanity.Dtos;

namespace PublisherService.Infrastructure.Profanity;

public sealed class ProfanityClientHttp : IProfanityClient
{
    private readonly HttpClient _http;
    private const string FilterPath = "api/Profanity/filter";

    public ProfanityClientHttp(HttpClient http) => _http = http;

    public async Task<string> FilterAsync(string text, CancellationToken ct = default)
    {
        try
        {
            using var res = await _http.PostAsJsonAsync(FilterPath, new FilterRequestDto(text), ct);
            res.EnsureSuccessStatusCode();

            var dto = await res.Content.ReadFromJsonAsync<FilterResultDto>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Empty response from ProfanityService");

            return dto.CleanedText;
        }
        catch (TaskCanceledException ex) { throw new ProfanityUnavailableException(inner: ex); }
        catch (HttpRequestException ex)  { throw new ProfanityUnavailableException(inner: ex); }
    }
}