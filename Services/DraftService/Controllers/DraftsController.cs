using Microsoft.AspNetCore.Mvc;
using DraftService.Models;
using DraftService.Interfaces;


namespace DraftService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DraftsController : ControllerBase
    {
        private readonly DraftService.Interfaces.IDraftService _draftService;

        public DraftsController(DraftService.Interfaces.IDraftService draftService)
        {
            _draftService = draftService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDraft([FromBody] DraftService.Models.Draft draft)
        {
            if (draft is null || string.IsNullOrWhiteSpace(draft.Author) || string.IsNullOrWhiteSpace(draft.Content))
                return BadRequest("Invalid draft data.");

            var created = await _draftService.CreateDraft(draft);
            return CreatedAtAction(nameof(GetDraftById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DraftService.Models.Draft>> GetDraftById(int id)
        {
            var draft = await _draftService.GetDraftById(id);
            if (draft is null) return NotFound();
            return Ok(draft);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DraftService.Models.Draft>>> GetDrafts()
            => Ok(await _draftService.GetDrafts());

        [HttpPut("{id}")]
        public async Task<ActionResult<DraftService.Models.Draft>> UpdateDraft(int id, [FromBody] DraftService.Models.Draft draft)
        {
            var updated = await _draftService.UpdateDraft(id, draft);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDraft(int id)
            => await _draftService.DeleteDraft(id) ? NoContent() : NotFound();
    }
}
