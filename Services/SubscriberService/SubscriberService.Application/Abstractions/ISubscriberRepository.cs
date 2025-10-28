using SubscriberService.Domain.Entities;

namespace SubscriberService.Application.Abstractions;

public interface ISubscriberRepository
{
    Task<Subscriber?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Subscriber subscriber, CancellationToken ct = default);
    Task UpdateAsync(Subscriber subscriber, CancellationToken ct = default);
    Task<IReadOnlyList<Subscriber>> GetActiveAsync(CancellationToken ct = default);
}