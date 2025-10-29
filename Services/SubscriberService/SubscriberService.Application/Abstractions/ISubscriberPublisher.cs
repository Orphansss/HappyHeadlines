using SubscriberService.Application.DTOs;

namespace SubscriberService.Application.Abstractions;

public interface ISubscriberPublisher
{
    Task PublishNewSubscriberAsync(SubscriberDto dto, CancellationToken ct = default);
}