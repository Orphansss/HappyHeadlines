// Controllers/NewsletterController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using NewsletterService.Interfaces;
using Serilog;

namespace NewsletterService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service) => _service = service;

    // GET /api/newsletter/daily?count=5
    [HttpGet("daily")]
    public async Task<IActionResult> SendDaily([FromQuery] int count = 5, CancellationToken ct = default)
    {
        if (count <= 0) count = 5;

        Log.Information("Daily newsletter requested for {Count} articles", count);

        var newsletter = await _service.SendDailyAsync(count, ct);

        // Build a tiny preview from the returned articles (optional)
        var preview = string.Join(Environment.NewLine,
            newsletter.Articles.Select(a => $"- {a.Title} ({a.PublishedAt:yyyy-MM-dd})"));

        return Ok(new
        {
            subject = newsletter.Subject,
            body = newsletter.Body,
            count = newsletter.Articles.Count,
            articles = newsletter.Articles,
            preview
        });
    }
}

