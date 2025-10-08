using CommentService.Exceptions;
using Microsoft.AspNetCore.Mvc;
using CommentService.Domain.Entities;
using CommentService.Application.Interfaces;

namespace CommentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<ActionResult<Comment>> CreateComment([FromBody] Comment comment, CancellationToken ct)
    {
        try
        {
            var created = await _commentService.CreateComment(comment, ct);
            return CreatedAtAction(nameof(GetCommentById), new { id = created.Id }, created);
        }
        catch (ProfanityUnavailableException)
        {
            // Strict fail-fast for the assignment:
            // Tell callers our dependency is currently down.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "ProfanityService unavailable. Please try again shortly."
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
    {
        var comments = await _commentService.GetComments();
        return Ok(comments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Comment>> GetCommentById(int id)
    {
        var comment = await _commentService.GetCommentById(id);
        if (comment == null) return NotFound();
        return Ok(comment);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Comment>> UpdateComment(int id, [FromBody] Comment comment, CancellationToken ct)
    {
        var updated = await _commentService.UpdateComment(id, comment, ct);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteComment(int id, CancellationToken ct)
    {
        var deleted = await _commentService.DeleteComment(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}