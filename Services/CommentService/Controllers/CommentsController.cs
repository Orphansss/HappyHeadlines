using CommentService.Exceptions;
using CommentService.Models;
using CommentService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Controllers;

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
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments([FromQuery] int? articleId, CancellationToken ct)
    {
        // If articleId is provided, use the LRU-cached article-scoped query
        if (articleId.HasValue)
        {
            var comments = await _commentService.GetCommentsByArticleId(articleId.Value, ct);
            return Ok(comments);
        }

        // Otherwise return all comments (not cached)
        var allComments = await _commentService.GetComments();
        return Ok(allComments);
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