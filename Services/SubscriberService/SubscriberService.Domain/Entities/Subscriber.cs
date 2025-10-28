using SubscriberService.Domain.Enums;

namespace SubscriberService.Domain.Entities;

public class Subscriber
{
    private Subscriber() { }

    public Subscriber(string email, DateTimeOffset nowUtc)
    {
        Email = Normalize(email);
        Status = SubscriberStatus.Active;
        CreatedAtUtc = nowUtc;
    }

    public int Id { get; private set; }
    public string Email { get; private set; } = default!;
    public SubscriberStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UnsubscribedAtUtc { get; private set; }

    public void Activate(DateTimeOffset nowUtc)
    {
        Status = SubscriberStatus.Active;
        UnsubscribedAtUtc = null;
    }

    public void Unsubscribe(DateTimeOffset nowUtc)
    {
        if(Status == SubscriberStatus.Unsubscribed) return;
        Status = SubscriberStatus.Unsubscribed;
        UnsubscribedAtUtc = nowUtc;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}