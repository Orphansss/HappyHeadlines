using NewsletterService.Models;

namespace NewsletterService.Interfaces
{
    public interface INewsletterService
    {
        Task<Newsletter> SendDailyAsync(int count, CancellationToken ct = default);
    }
}
