using NewsletterService.Contracts.Dtos;
using NewsletterService.Interfaces;
using NewsletterService.Models;
using RestSharp;
using Serilog;

namespace NewsletterService.Services;

public class NewsletterService : INewsletterService
{
    private readonly IHttpClientFactory _http;
    private readonly INewsletterPublisher _publisher;
    private static readonly RestClient restClient = new RestClient("http://article-service:8080");



  public NewsletterService(IHttpClientFactory http, INewsletterPublisher publisher)
    {
        _http = http;
        _publisher = publisher;
  }

  public async Task<Newsletter> SendDailyAsync(List<string> email, int count, CancellationToken ct = default)
  {
      try
      {
        var req = new RestRequest($"/api/articles").AddQueryParameter("count", count);
        var resp = await restClient.ExecuteAsync<List<ArticleSummary>>(req, ct);

        if (!resp.IsSuccessful || resp.Data == null)
        {
          Log.Warning("ArticleService call failed. Status={StatusCode}, Error={ErrorMessage}",
              resp.StatusCode, resp.ErrorMessage);
        }

        var articles = resp.Data ?? new List<ArticleSummary>();

        var newsletter = new Newsletter
        {
          Subject = $"Daily newsletter ({articles.Count})",
          Body = "Here are today’s top articles",
          Articles = articles
        };

        Log.Information("Prepared newsletter with {ArticleCount} articles", newsletter.Articles.Count);

        return newsletter;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to build newsletter");
        throw;
      }
  }

  // Send a welcome mail to new subscribers to the subscriber queue as a event
  public async Task<Newsletter> SendWelcomeAsync(string email, CancellationToken ct = default)
  {
      var newsletter = new Newsletter
      {
          Subject = "Welcome to our Newsletter!",
          Body = $"Hello {email}, welcome to our community! We're glad to have you.",
      };

      try
      {
          await _publisher.PublishWelcomeAsync(
              new WelcomeEmailRequested(email, newsletter.Subject, newsletter.Body),
              ct
          );
          Log.Information("Queued welcome email event for {Email}", email);
      }
      catch (Exception ex)
      {
          Log.Error(ex, "Failed to queue welcome email event for {Email}", email);
      }

      return newsletter; 
  }
}
