namespace SubscriberService.Application.Abstractions;

public interface IFeatureToggle
{
    bool IsServiceEnabled();
    bool ShouldPublishNewSubscriber();
}