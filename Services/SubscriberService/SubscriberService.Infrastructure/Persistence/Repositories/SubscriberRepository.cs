using Microsoft.EntityFrameworkCore;
using SubscriberService.Application.Abstractions;
using SubscriberService.Domain.Entities;
using SubscriberService.Domain.Enums;

namespace SubscriberService.Infrastructure.Persistence.Repositories;

public class SubscriberRepository : ISubscriberRepository
{
    private readonly SubscriberDbContext _db;
    public SubscriberRepository(SubscriberDbContext db) => _db = db;

    public async Task<Subscriber?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _db.Subscribers.FirstOrDefaultAsync(
            s => s.Email == Subscriber.Normalize(email), ct);
    }

    public async Task AddAsync(Subscriber subscriber, CancellationToken ct = default)
    {
        await _db.Subscribers.AddAsync(subscriber, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Subscriber subscriber, CancellationToken ct = default)
    {
        _db.Subscribers.Update(subscriber);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Subscriber>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Subscribers
            .Where(s => s.Status == SubscriberStatus.Active)
            .OrderBy(s => s.Email)
            .ToListAsync(ct);
    }
}