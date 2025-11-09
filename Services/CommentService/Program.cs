using CommentService.Data;
using CommentService.Interfaces;
using CommentService.Profanity;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Monitoring;

namespace CommentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.AddMonitoring("CommentService"); // adding SeriLog now through or Monitoring class

            var config = builder.Configuration;
            
            builder.Services.AddScoped<ICommentService, Services.CommentService>();
            builder.Services.AddDbContext<CommentDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
            
            // 1) Create a single, shared breaker instance
            var sharedBreaker = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(10),
                    onBreak: (outcome, span) =>
                        Log.Warning("Polly Circuit OPEN for {BreakSeconds}s. Reason={Reason}",
                            span.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()),
                    onReset: () => Log.Information("Polly Circuit CLOSED"),
                    onHalfOpen: () => Log.Information("Polly Circuit HALF-OPEN")
                );
            
            // HttpClient to ProfanityService 
            builder.Services
                .AddHttpClient<IProfanityService, ProfanityServiceHttp>(client =>
                {
                    var baseUrl = config["PROFANITY_BASEURL"]
                                  ?? throw new InvalidOperationException("PROFANITY_BASEURL not set");
                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = TimeSpan.FromSeconds(2);
                })
                // 1) Retry (exponential backoff)
                .AddPolicyHandler(_ =>
                    HttpPolicyExtensions.HandleTransientHttpError()
                        .WaitAndRetryAsync(
                            new[]
                            {
                                TimeSpan.FromMilliseconds(200),
                                TimeSpan.FromMilliseconds(400),
                                TimeSpan.FromMilliseconds(800)
                            },
                            onRetry: (outcome, delay, attempt, _) =>
                                Log.Warning("Polly Retry {Attempt} after {Delay}ms. Reason={Reason}",
                                    attempt,
                                    delay.TotalMilliseconds,
                                    outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString())
                        ))
                // 2) Circuit Breaker (fail fast when dependency is unhealthy)
                // Use the shared breaker instance
                .AddPolicyHandler(sharedBreaker);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.MapGet("/health", () => Results.Ok(new { ok = true, service = "comment-service" }));

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapGet("/", () => Results.Redirect("/swagger"));
            
            app.UseAuthorization();

            // Auto-migrate at startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CommentDbContext>();
                db.Database.Migrate();
            }

            app.MapControllers();

            // add traceId into all logs + nice request logging
            app.UseTraceIdEnricher();
            app.UseSerilogRequestLogging();

            app.Run();
        }
    }
}
