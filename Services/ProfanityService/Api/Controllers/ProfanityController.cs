using Microsoft.AspNetCore.Mvc;
using ProfanityService.Application.Abstractions;
using ProfanityService.Domain.Entities;

namespace ProfanityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfanityController : ControllerBase
{
    private readonly IProfanityService _profanityService;

    public ProfanityController(IProfanityService profanityService)
    {
        _profanityService = profanityService;
    }

    // POST /api/profanity
    [HttpPost]
    public async Task<ActionResult<Profanity>> CreateProfanityTerm([FromBody] Profanity profanity)
    {
        var created = await _profanityService.CreateProfanityTerm(profanity);
        return CreatedAtAction(nameof(GetProfanityTermById), new { id = created.Id }, created);
    }

    // GET /api/profanity
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Profanity>>> GetProfanityTerms()
    {
        var profanities = await _profanityService.GetProfanityTerms();
        return Ok(profanities);
    }

    // GET /api/profanity/active
    [HttpGet("/active")]
    public async Task<ActionResult<IReadOnlyList<Profanity>>> GetActiveProfanityTerms()
    {
        var activeProfanities = await _profanityService.GetActiveProfanityTerms();
        return Ok(activeProfanities);
    }

    // GET /api/profanity/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Profanity>> GetProfanityTermById(int id)
    {
        var profanity = await _profanityService.GetProfanityTermById(id);
        if (profanity == null) return NotFound();
        return Ok(profanity);
    }

    // PUT /api/profanity/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Profanity>> UpdateProfanityTerm(int id, [FromBody] Profanity profanity)
    {
        var updated = await _profanityService.UpdateProfanityTerm(id, profanity);
        return updated is null ? NotFound() : Ok(updated);
    }

    // PATCH /api/profanity/{id}/deactivate
    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult> DeactivateProfanityTerm(int id)
    {
        var deactivated = await _profanityService.DeactivateProfanityTerm(id);
        return deactivated ? NoContent() : NotFound();
    }

    // PATCH /api/profanity/{id}/reactivate
    [HttpPatch("{id:int}/reactivate")]
    public async Task<ActionResult> ReactivateProfanityTerm(int id)
    {
        var reactivate = await _profanityService.ReactivateProfanityTerm(id);
        return reactivate ? NoContent() : NotFound();
    }
}