using CommentService.Infrastructure.Profanity.Dtos;

namespace CommentService.Application.Interfaces;

public interface IProfanityService
{
    Task<FilterResultDto> FilterAsync(FilterRequestDto request, CancellationToken ct = default);
}