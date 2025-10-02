using Microsoft.AspNetCore.Mvc;
using PublisherService.Api.Dtos;
using PublisherService.Application.Common;
using PublisherService.Application.UseCases.PublishArticle;

namespace PublisherService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PublishController : ControllerBase
{
    private readonly PublishArticleHandler _handler;

    public PublishController(PublishArticleHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Publishes an article after profanity filtering.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PublishResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PublishResponseDto>> PublishArticle(
        [FromBody] PublishRequestDto request,
        CancellationToken ct)
    {
        // Read optional Idempotency-Key header
        var idempotencyKey = HttpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

        try
        {
            var cmd = new PublishArticleCommand(
                AuthorId: request.AuthorId,
                Title: request.Title,
                Summary: request.Summary,
                Content: request.Content,
                Region: request.Region,
                IdempotencyKey: idempotencyKey
            );

            var result = await _handler.HandleAsync(cmd, ct);

            var response = new PublishResponseDto(result.PublicationId, result.AcceptedAt);
            return AcceptedAtAction(nameof(PublishArticle), new { id = result.PublicationId }, response);
        }
        catch (ProfanityUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "ProfanityService unavailable. Please try again shortly."
            });
        }
        catch (Exception ex)
        {
            // fallback error handling
            return BadRequest(new { error = ex.Message });
        }
    }
}
