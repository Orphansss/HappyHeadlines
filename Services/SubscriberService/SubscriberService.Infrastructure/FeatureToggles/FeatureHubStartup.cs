using FeatureHubSDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SubscriberService.Infrastructure.FeatureToggles;

public static class FeatureHubStartup
{
    public static IServiceCollection AddFeatureHub(this IServiceCollection services, IConfiguration config)
    {
        var edgeUrl = config["FeatureHub:EdgeUrl"] ?? "http://featurehub:8085";
        var apiKey  = config["FeatureHub:ApiKey"]  ?? throw new InvalidOperationException("FeatureHub:ApiKey missing.");

        var fhConfig = new EdgeFeatureHubConfig(edgeUrl, apiKey);

        // Build a default context for the service (can add targeting attributes here)
        var ctx = fhConfig.NewContext() 
            .Build()
            .GetAwaiter().GetResult();         

        services.AddSingleton<IClientContext>(ctx);
        return services;
    }
}