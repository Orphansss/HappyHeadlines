// Services/NewsletterService.cs
using System.Net.Http.Json;
using NewsletterService.Interfaces;
using NewsletterService.Models;
using Serilog;
using NewsletterService.Interfaces;

namespace NewsletterService.Services;

public class NewsletterService : INewsletterService
{
    private readonly IHttpClientFactory _http;

    public NewsletterService(IHttpClientFactory http)
    {
        _http = http;
    }

    public async Task<Newsletter> SendDailyAsync(int count, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("articles");
            Log.Information("Requesting latest {Count} articles from ArticleService", count);

            var articles = await client.GetFromJsonAsync<List<ArticleSummary>>(
                $"/api/articles/latest?count={count}", ct) ?? new();

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
            // rethrow or convert to a nicer problem-details error
            throw;
        }
    }

}
