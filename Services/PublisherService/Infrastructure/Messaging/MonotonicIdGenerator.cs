using PublisherService.Application.Abstractions;

namespace PublisherService.Infrastructure.Messaging;

/// <summary>
/// Simple monotonic in-memory ID generator (resets on process restart).
/// For demo/testing only. In production, a DB/sequence or ULID would be better.
/// </summary>
public sealed class MonotonicIdGenerator : IIdGenerator
{
    private int _current = 0;
    public int NextId() => Interlocked.Increment(ref _current);
}