namespace SubscriberService.Application.DTOs;

public class SubscriberDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset SubscribedAtUtc { get; set; }
}