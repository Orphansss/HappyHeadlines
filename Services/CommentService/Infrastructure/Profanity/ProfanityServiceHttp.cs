using CommentService.Application.Interfaces;
using CommentService.Infrastructure.Profanity.Dtos;

namespace CommentService.Infrastructure.Profanity;

public class ProfanityServiceHttp : IProfanityService
{
    private readonly HttpClient _httpClient;
    private const string FilterPath = "api/Profanity/filter";

    public ProfanityServiceHttp(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<FilterResultDto> FilterAsync(FilterRequestDto request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(FilterPath, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FilterResultDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Empty response from ProfanityService");
    }

}