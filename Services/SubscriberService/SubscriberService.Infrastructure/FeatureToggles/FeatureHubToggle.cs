using FeatureHubSDK;
using SubscriberService.Application.Abstractions;

namespace SubscriberService.Infrastructure.FeatureToggles;

/// <summary>
/// Reads runtime feature flags from FeatureHub.
/// Required flags:
/// - subscriber-service.enabled (bool)
/// - subscriber-service.publish-new-subscriber (bool)
/// </summary>
public class FeatureHubToggle : IFeatureToggle
{
    private readonly IClientContext _context;
    
    public FeatureHubToggle(IClientContext context) => _context = context;
    
    public bool IsServiceEnabled()
        => _context["subscriber-service.enabled"].IsEnabled;

    public bool ShouldPublishNewSubscriber()
        => _context["subscriber-service.publish-new-subscriber"].IsEnabled;
}