using PublisherService.Domain.Exceptions;

namespace PublisherService.Domain;

internal static class Guard
{
    public static void NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty) throw new DomainValidationException($"{name} cannot be empty.");
    }

    public static void NotNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException($"{name} is required.");
    }

    public static void MaxLength(string? value, int max, string name)
    {
        if (value is not null && value.Length > max)
            throw new DomainValidationException($"{name} exceeds max length of {max}.");
    }
}