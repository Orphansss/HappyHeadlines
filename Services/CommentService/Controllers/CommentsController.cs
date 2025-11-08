using CommentService.Exceptions;
using CommentService.Models;
using CommentService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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
        Log.Information("CreateComment received. TraceId={TraceId}", HttpContext.TraceIdentifier);

        try
        {
            var created = await _commentService.CreateComment(comment, ct);
            Log.Information("Comment created. Id={Id} TraceId={TraceId}", created.Id, HttpContext.TraceIdentifier);

            return CreatedAtAction(nameof(CreateComment), new { id = created.Id }, created);
        }
        catch (ProfanityUnavailableException ex)
        {
            Log.Warning(ex, "ProfanityService unavailable → 503. TraceId={TraceId}", HttpContext.TraceIdentifier);

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