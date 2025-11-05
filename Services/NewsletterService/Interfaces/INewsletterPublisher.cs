using NewsletterService.Contracts.Dtos;

namespace NewsletterService.Interfaces;

public interface INewsletterPublisher
{
    Task PublishWelcomeAsync(WelcomeEmailRequested evt, CancellationToken ct = default);
}
