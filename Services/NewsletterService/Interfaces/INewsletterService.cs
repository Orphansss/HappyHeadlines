using NewsletterService.Models;

namespace NewsletterService.Interfaces
{
    public interface INewsletterService
    {
        Task<Newsletter> SendDailyAsync(List<string> email, int count, CancellationToken ct = default);
        Task<Newsletter> SendWelcomeAsync(string email, CancellationToken ct = default);
    }
}
