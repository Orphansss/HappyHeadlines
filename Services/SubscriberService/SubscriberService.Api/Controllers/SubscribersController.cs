using Microsoft.AspNetCore.Mvc;
using RestSharp;
using SubscriberService.Application.Abstractions;
using SubscriberService.Application.DTOs;
using SubscriberService.Application.Services;
using Serilog;

namespace SubscriberService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscribersController : ControllerBase
{
    private static readonly RestClient restClient = new RestClient("http://newsletter-service:8083");

    private readonly ISubscriberService _service;
    public SubscribersController(ISubscriberService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req, CancellationToken ct)
    {
        var result = await _service.SubscribeAsync(req.Email, ct);
        Console.WriteLine(result);
        if (result == SubscribeResult.Created)
        {
            Log.Information("New subscriber added: {Email}", req.Email);
            _ = restClient.ExecuteAsync(new RestRequest("/api/newsletter/welcome", Method.POST).AddJsonBody(req.Email), CancellationToken.None);
            return Ok(result);
        }

        Log.Information("Subscriber {Email} subscription status: {Status}", req.Email, result);
        return NoContent();
    }   
    
    [HttpDelete("{email}")]
    public async Task<IActionResult> Unsubcribe([FromRoute] string email, CancellationToken ct = default)
    {
        await _service.UnsubscribeAsync(email, ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriberDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _service.GetActiveSubscribersAsync(ct);
        return Ok(list);
    }
    
}