namespace SubscriberService.Api.Middleware;


public static class FeatureToggleMiddlewareExtensions
{
    public static IApplicationBuilder UseFeatureToggleGate(this IApplicationBuilder app)
        => app.UseMiddleware<FeatureToggleMiddleware>();
}