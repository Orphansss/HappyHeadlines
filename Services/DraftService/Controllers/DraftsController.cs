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
        public async Task<IActionResult> CreateDraft([FromBody] Draft draft)
        {
            if (draft == null ||
                draft.ArticleId <= 0 ||
                string.IsNullOrWhiteSpace(draft.Title) ||
                string.IsNullOrWhiteSpace(draft.Body))
                return BadRequest("ArticleId, Title and Content are required.");

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
        public async Task<ActionResult<Draft>> UpdateDraft(int id, [FromBody] Draft draft)
        {
            if (draft == null ||
                draft.ArticleId <= 0 ||
                string.IsNullOrWhiteSpace(draft.Title) ||
                string.IsNullOrWhiteSpace(draft.Body))
                return BadRequest("ArticleId, Title and Content are required.");

            var updated = await _draftService.UpdateDraft(id, draft);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDraft(int id)
            => await _draftService.DeleteDraft(id) ? NoContent() : NotFound();
    }
}
