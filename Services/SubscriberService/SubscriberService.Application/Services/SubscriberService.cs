using Microsoft.EntityFrameworkCore;
using SubscriberService.Application.Abstractions;
using SubscriberService.Application.DTOs;
using SubscriberService.Domain.Entities;
using SubscriberService.Domain.Enums;

namespace SubscriberService.Application.Services;

public enum SubscribeResult { Created, Reactivated, AlreadyActive, AlreadyUnsubscribed, Unsubscribed }

public class SubscriberService : ISubscriberService
{
    private readonly ISubscriberRepository _repo;
    private readonly ISubscriberPublisher _publisher;
    private readonly IFeatureToggle _toggles;
    
    public SubscriberService(ISubscriberRepository repo, ISubscriberPublisher publisher, IFeatureToggle toggles)
    {
        _repo = repo;
        _publisher = publisher;
        _toggles = toggles;
    }
    
    public async Task<SubscribeResult> SubscribeAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("Invalid email address.", nameof(email));

        var normalized = Subscriber.Normalize(email);
        var existing = await _repo.GetByEmailAsync(normalized, ct);

        // First-time subscribe
        if (existing is null)
        {
            var now = DateTimeOffset.UtcNow;
            var subscriber = new Subscriber(normalized, now);

            await _repo.AddAsync(subscriber, ct);
            
            if (_toggles.ShouldPublishNewSubscriber())
            {
                await _publisher.PublishNewSubscriberAsync(new SubscriberDto
                {
                    Id = subscriber.Id,
                    Email = subscriber.Email,
                    SubscribedAtUtc = subscriber.CreatedAtUtc
                }, ct);
            }
            return SubscribeResult.Created;
        }

        // Already active → idempotent no-op (and no event)
        if (existing.Status == SubscriberStatus.Active)
            return SubscribeResult.AlreadyActive;

        // Reactivate after unsubscribe → publish again (per your rule)
        existing.Activate(DateTimeOffset.UtcNow);
        await _repo.UpdateAsync(existing, ct);

        if (_toggles.ShouldPublishNewSubscriber())
        {
            await _publisher.PublishNewSubscriberAsync(new SubscriberDto
            {
                Id = existing.Id,
                Email = existing.Email,
                SubscribedAtUtc = existing.CreatedAtUtc
            }, ct);
        }
        return SubscribeResult.Reactivated;
    }

    public async Task<SubscribeResult> UnsubscribeAsync(string email, CancellationToken ct = default)
    {
        var normalized = Subscriber.Normalize(email);
        var existing = await _repo.GetByEmailAsync(normalized, ct);
        
        if (existing == null)
            return SubscribeResult.Unsubscribed;
        
        if (existing.Status == SubscriberStatus.Unsubscribed)
            return SubscribeResult.AlreadyUnsubscribed;
        
        existing.Unsubscribe(DateTimeOffset.UtcNow);
        await _repo.UpdateAsync(existing, ct);
        
        return SubscribeResult.Unsubscribed;
    }

    public async Task<IReadOnlyList<SubscriberDto>> GetActiveSubscribersAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetActiveAsync(ct);
        return items.Select(s => new SubscriberDto
        {
            Id = s.Id,
            Email = s.Email,
            SubscribedAtUtc = s.CreatedAtUtc
        }).ToList();
    }
}