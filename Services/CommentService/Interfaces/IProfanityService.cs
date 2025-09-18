using CommentService.Profanity.Dtos;

namespace CommentService.Interfaces;

public interface IProfanityService
{
    Task<FilterResultDto> FilterAsync(FilterRequestDto request, CancellationToken ct = default);
}