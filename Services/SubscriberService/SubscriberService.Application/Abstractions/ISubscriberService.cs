using SubscriberService.Application.DTOs;

namespace SubscriberService.Application.Abstractions;

public interface ISubscriberService
{
    Task SubscribeAsync(string email, CancellationToken ct = default);
    Task UnsubscribeAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriberDto>> GetActiveSubscribersAsync(CancellationToken ct = default);
}