using SubscriberService.Application.DTOs;
using SubscriberService.Application.Services;

namespace SubscriberService.Application.Abstractions;

public interface ISubscriberService
{
    Task<SubscribeResult> SubscribeAsync(string email, CancellationToken ct = default);
    Task<SubscribeResult> UnsubscribeAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriberDto>> GetActiveSubscribersAsync(CancellationToken ct = default);
}