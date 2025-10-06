using CommentService.Data;
using CommentService.Interfaces;
using CommentService.Profanity;
using CommentService.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Monitoring;
using StackExchange.Redis;

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
            // Apply the decorator pattern with Scrutor
            builder.Services.Decorate<ICommentService, CachedCommentService>();
            
            builder.Services.AddDbContext<CommentDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
            
            // Read and validate (fail fast if missing)
            var redisConnString = builder.Configuration.GetConnectionString("Redis")
                                  ?? throw new InvalidOperationException("Missing connection string: 'Redis'");

            // Distributed cache (IDistributedCache)
            builder.Services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = redisConnString;
            });

            // Redis multiplexer for LRU ZSET ops
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var cfg = ConfigurationOptions.Parse(redisConnString);
                cfg.AbortOnConnectFail = false; // don’t crash on cold start
                cfg.ConnectRetry       = 3;
                cfg.ConnectTimeout     = 3000;
                return ConnectionMultiplexer.Connect(cfg);
            });
            
            // Create a single, shared breaker instance
            var sharedBreaker = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 2,           
                    durationOfBreak: TimeSpan.FromSeconds(10),
                    onBreak: (outcome, span) =>
                        Console.WriteLine($"[Polly] Circuit OPEN for {span}. Reason: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}"),
                    onReset: () => Console.WriteLine("[Polly] Circuit CLOSED."),
                    onHalfOpen: () => Console.WriteLine("[Polly] Circuit HALF-OPEN.")
                );

            builder.Services
                .AddHttpClient<IProfanityService, ProfanityServiceHttp>(client =>
                {
                    var baseUrl = config["PROFANITY_BASEURL"]
                                  ?? throw new InvalidOperationException("PROFANITY_BASEURL not set");
                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = TimeSpan.FromSeconds(2);
                })
                // Retry (exponential backoff)
                .AddPolicyHandler(_ =>
                    HttpPolicyExtensions.HandleTransientHttpError()
                        .WaitAndRetryAsync(new[]
                            {
                                TimeSpan.FromMilliseconds(200),
                                TimeSpan.FromMilliseconds(400),
                                TimeSpan.FromMilliseconds(800)
                            },
                            onRetry: (outcome, delay, attempt, _) =>
                                Console.WriteLine($"[Polly] Retry {attempt} after {delay}. Reason: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}")))
                // Circuit Breaker (fail fast when dependency is unhealthy)
                // Use the shared breaker instance
                .AddPolicyHandler(sharedBreaker);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.MapGet("/health", () => Results.Ok(new { ok = true, service = "comment-service" }));

            if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
            {
                // Swagger JSON + UI available at /swagger
                app.UseSwagger();
                app.UseSwaggerUI();
            }

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
