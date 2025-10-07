using ArticleService.Api.Contracts.Dtos;
using ArticleService.Api.Contracts.Mappings;
using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Api.Controllers;

// /Api/Controllers/ArticlesController.cs
[ApiController]
[Route("api/[controller]")]
public sealed class ArticlesController(IArticleService articleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ArticleResponse>> Create(
        [FromBody] CreateArticleRequest request, CancellationToken ct)
    {
        var created = await articleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> GetAll(CancellationToken ct) =>
        Ok(await articleService.GetAllAsync(ct));

    [HttpGet("latest/{count:int}")]
    public async Task<ActionResult<IReadOnlyList<ArticleResponse>>> GetLatest(int count, CancellationToken ct) =>
        Ok(await articleService.GetLatestAsync(count, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await articleService.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArticleResponse>> Update(
        int id, [FromBody] UpdateArticleRequest input, CancellationToken ct)
    {
        var updated = await articleService.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await articleService.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
