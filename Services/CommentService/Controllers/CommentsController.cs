using CommentService.Contracts;
using CommentService.Contracts.Dtos;
using CommentService.Exceptions;
using CommentService.Interfaces;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Controllers;

[ApiController]
[Route("api/comments")] // keep for id-based endpoints
public class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;

    public CommentsController(ICommentService comments)
    {
        _comments = comments;
    }

    // Article-scoped endpoints (cache targets)

    // GET /api/articles/{articleId}/comments
    [HttpGet("~/api/articles/{articleId:int}/comments")]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetCommentsForArticle(
        [FromRoute] int articleId,
        CancellationToken ct = default)
    {
        if (articleId <= 0) return BadRequest("articleId must be a positive integer.");

        var comments = await _comments.GetCommentsForArticle(articleId, ct);
        return Ok(comments.ToResponse());
    }

    // POST /api/articles/{articleId}/comments
    [HttpPost("~/api/articles/{articleId:int}/comments")]
    public async Task<ActionResult<CommentResponse>> CreateForArticle(
        [FromRoute] int articleId,
        [FromBody] CreateCommentRequest body,
        CancellationToken ct)
    {
        if (articleId <= 0) return BadRequest("articleId must be a positive integer.");
        if (string.IsNullOrWhiteSpace(body.Content)) return BadRequest("Content is required.");

        try
        {
            var created = await _comments.CreateComment(body.ToEntity(articleId), ct);
            var resp = created.ToResponse();

            return CreatedAtAction(
                nameof(GetCommentById),
                new { id = created.Id },
                resp
            );
        }
        catch (ProfanityUnavailableException)
        {
            // strict fail-fast 
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "ProfanityService unavailable. Please try again shortly."
            });
        }
    }

    // Id-based endpoints

    // GET /api/comments/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommentResponse>> GetCommentById(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        if (id <= 0) return BadRequest("id must be a positive integer.");

        var comment = await _comments.GetCommentById(id, ct);
        if (comment is null) return NotFound();
        return Ok(comment.ToResponse());
    }

    // PUT /api/comments/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CommentResponse>> UpdateComment(
        [FromRoute] int id,
        [FromBody] CreateCommentRequest body, // reuse simple shape (author/content)
        CancellationToken ct)
    {
        if (id <= 0) return BadRequest("id must be a positive integer.");
        if (string.IsNullOrWhiteSpace(body.Content)) return BadRequest("Content is required.");

        // service will keep ArticleId from existing entity
        var updated = await _comments.UpdateComment(id, new Comment
        {
            Id       = id,
            Content  = body.Content,
            AuthorId = body.AuthorId
        }, ct);

        if (updated is null) return NotFound();
        return Ok(updated.ToResponse());
    }

    // DELETE /api/comments/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] int id,
        CancellationToken ct)
    {
        if (id <= 0) return BadRequest("id must be a positive integer.");

        var deleted = await _comments.DeleteComment(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
