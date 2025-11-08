using ArticleService.Api.Contracts.Dtos;
using ArticleService.Api.Contracts.Mappings;
using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ArticlesController(IArticleService articleService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleResponse>> Create([FromBody] CreateArticleRequest request, CancellationToken ct)
    {
        try
        {
            var article = request.ToEntity();
            var created = await articleService.CreateAsync(article, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());

        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArticleListDto>>> GetAll(CancellationToken ct) =>
        Ok(await articleService.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> GetById(int id, CancellationToken ct)
    {
        var item = await articleService.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UpdateArticleDto>> Update(int id, [FromBody] Article input, CancellationToken ct)
    {
        var updated = await articleService.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await articleService.DeleteAsync(id, ct) ? NoContent() : NotFound();
}