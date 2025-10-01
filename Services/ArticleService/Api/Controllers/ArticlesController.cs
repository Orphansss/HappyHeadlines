using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ArticlesController(IArticleService _articleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Article>> Create([FromBody] Article input, CancellationToken ct)
    {
        try
        {
            var created = await _articleService.CreateAsync(input, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Article>>> GetAll(CancellationToken ct) =>
        Ok(await _articleService.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Article>> GetById(int id, CancellationToken ct)
    {
        var item = await _articleService.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Article>> Update(int id, [FromBody] Article input, CancellationToken ct)
    {
        var updated = await _articleService.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await _articleService.DeleteAsync(id, ct) ? NoContent() : NotFound();
}