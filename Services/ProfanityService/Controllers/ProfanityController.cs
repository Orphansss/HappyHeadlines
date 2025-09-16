using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;
using ProfanityService.DTOs;
using ProfanityService.Models;
using ProfanityService.Services;

namespace ProfanityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfanityController : ControllerBase
{
    private readonly ProfanityDbContext _db;
    private readonly IProfanityFilter _filter;

    public ProfanityController(ProfanityDbContext db, IProfanityFilter filter)
    {
        _db = db;
        _filter = filter;
    }

    // POST api/profanity/filter
    [HttpPost("filter")]
    public async Task<ActionResult<FilterResponse>> Filter([FromBody] FilterRequest req)
    {
        var (ok, cleaned, hits) = await _filter.FilterAsync(req.Text);
        return Ok(new FilterResponse(ok, cleaned, hits));
    }

    // GET api/profanity/words
    [HttpGet("words")]
    public async Task<ActionResult<IEnumerable<Word>>> GetWords()
    {
        return Ok(await _db.Words.AsNoTracking().ToListAsync());
    }

    // GET api/profanity/words/{id}
    [HttpGet("words/{id:int}")]
    public async Task<ActionResult<Word>> GetWord(int id)
    {
        var w = await _db.Words.FindAsync(id);
        if (w == null) return NotFound();
        return Ok(w);
    }

    // POST api/profanity/words
    [HttpPost("words")]
    public async Task<ActionResult<Word>> PostWord(CreateWordDto dto)
    {
        var word = new Word { Value = dto.Value };
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetWord), new { id = word.Id }, word);
    }

    // DELETE api/profanity/words/{id}
    [HttpDelete("words/{id:int}")]
    public async Task<ActionResult> DeleteWord(int id)
    {
        var w = await _db.Words.FindAsync(id);
        if (w == null) return NotFound();
        _db.Words.Remove(w);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
