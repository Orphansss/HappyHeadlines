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
    public async Task<ActionResult<Comment>> CreateComment([FromBody] Comment comment)
    {
        var created = await _commentService.CreateComment(comment);
        return CreatedAtAction(nameof(GetCommentById), new { id = created.Id }, created);
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
    public async Task<ActionResult<Comment>> UpdateComment(int id, [FromBody] Comment comment)
    {
        var updated = await _commentService.UpdateComment(id, comment);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteComment(int id)
    {
        var deleted = await _commentService.DeleteComment(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}