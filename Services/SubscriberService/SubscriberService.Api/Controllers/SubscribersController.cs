using Microsoft.AspNetCore.Mvc;
using SubscriberService.Application.Abstractions;
using SubscriberService.Application.DTOs;

namespace SubscriberService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscribersController : ControllerBase
{
    private readonly ISubscriberService _service;
    public SubscribersController(ISubscriberService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req, CancellationToken ct)
    {
        await _service.SubscribeAsync(req.Email, ct);
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