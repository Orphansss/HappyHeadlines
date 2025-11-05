using Microsoft.AspNetCore.Mvc;
using NewsletterService.Interfaces;
using RestSharp;
using Serilog;

namespace NewsletterService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private static readonly RestClient restClient = new RestClient("http://subscriber-service:8091");
    private readonly INewsletterService _service;
    public sealed record SubscriberDto(string Email, bool IsActive);


  public NewsletterController(INewsletterService service) => _service = service;

    // GET /api/newsletter/daily?count=5
    [HttpGet("daily")]
    public async Task<IActionResult> SendDailyAsync([FromQuery] int count = 5, CancellationToken ct = default)
    {

        Log.Information("Daily newsletter requested for {Count} articles", count);
      
        var task = restClient.GetAsync<List<SubscriberDto>>(
        new RestRequest("/api/subscribers", Method.Get), ct
        );

        List<string> emails = [];

        var winner = await Task.WhenAny(task, Task.Delay(500));

        if (winner == task &&
        task.Status == TaskStatus.RanToCompletion &&
        task.Result is List<SubscriberDto> list)
        {
          emails = list.Select(s => s.Email).ToList();
          Log.Debug("Fire-and-hope succeeded: got {Count} emails", emails.Count);
        }
        else
        {
          Log.Warning("Fire-and-hope: subscriber fetch didn't finish in time");
        }

        Log.Information("Subscribers to send newsletter to: {Emails}", string.Join(", ", emails));

        var newsletter = await _service.SendDailyAsync(emails, count, ct);

        // Build a tiny preview from the returned articles
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

    // Post welcome newsletter for new subscribers
    [HttpPost("welcome")]
    public async Task<IActionResult> SendWelcomeAsync([FromBody] string email, CancellationToken ct = default)
    {
        Log.Information("Welcome newsletter requested for {Email}", email);
        await _service.SendWelcomeAsync(email, ct);

        return Ok(new { message = $"Welcome newsletter sent to {email}" });
    }
}

