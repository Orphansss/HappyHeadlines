using NewsletterService.Interfaces;
using Monitoring;

namespace NewsletterService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // central logging + tracing (Seq + Jaeger) via your helper
            builder.AddMonitoring("NewsletterService");

            // HttpClient to ArticleService (URL comes from env)
            var articleBaseUrl =
                builder.Configuration["ArticleService:BaseUrl"] ??
                Environment.GetEnvironmentVariable("ARTICLE_BASEURL") ??
                "http://article-service:8080/";

            builder.Services.AddHttpClient("articles", c =>
            {
                // internal Docker DNS name + port of your ArticleService
                // (matches docker-compose service name and ASPNETCORE_URLS)
                c.BaseAddress = new Uri(
                    builder.Configuration["Services:ArticleService"]
                    ?? "http://article-service:8080/");
            });


            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // our service
            builder.Services.AddScoped<INewsletterService, NewsletterService.Services.NewsletterService>();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
            // add traceId to every log
            app.UseTraceIdEnricher();

            app.MapControllers();

            app.Run();
        }
    }
}
