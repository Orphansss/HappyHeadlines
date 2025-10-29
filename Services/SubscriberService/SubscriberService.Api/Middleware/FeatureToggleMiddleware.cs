using SubscriberService.Application.Abstractions;

namespace SubscriberService.Api.Middleware;

public class FeatureToggleMiddleware
{
    private readonly RequestDelegate _next;

    public FeatureToggleMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IFeatureToggle toggles)
    {
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Allow health & swagger even when disabled
        var allowList = new[] { "/swagger", "/health", "/liveness", "/readiness" };
        if (allowList.Any(path.StartsWith))
        {
            await _next(ctx);
            return;
        }

        if (!toggles.IsServiceEnabled())
        {
            ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await ctx.Response.WriteAsJsonAsync(new { error = "SubscriberService disabled by feature toggle." });
            return;
        }

        await _next(ctx);
    }
}