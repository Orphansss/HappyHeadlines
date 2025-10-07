using ArticleService.Application.Interfaces;
using ArticleService.Infrastructure;
using ArticleService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Monitoring;
using Prometheus;

namespace ArticleService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddMonitoring("ArticleService"); // adding SeriLog now through or Monitoring class

            // Gør HttpContext tilgængelig (til at læse X-Region / ?region=)
            builder.Services.AddHttpContextAccessor();

            // Registrér vores RegionResolver (finder den rigtige connection pr. request)
            builder.Services.AddScoped<IRegionResolver, RegionResolver>();
            // Application services
            builder.Services.AddScoped<IArticleService, Application.Services.ArticleService>();


            // Konfigurér DbContext med connection string valgt ved runtime (per request)
            builder.Services.AddDbContext<ArticleDbContext>((sp, o) =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var def = cfg.GetConnectionString("Default");
                var cs = string.IsNullOrWhiteSpace(def)
                    ? sp.GetRequiredService<IRegionResolver>().ResolveConnection(cfg)
                    : def;

                o.UseSqlServer(cs, sql => sql.EnableRetryOnFailure());
            });
            
            // Register RabbitMQ consumer
            builder.Services.AddHostedService<ArticleQueueConsumerRabbit>();   // runs as background worker
            builder.Services.AddScoped<IArticleQueueConsumer, ArticleQueueConsumerRabbit>(); 

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            var app = builder.Build();

            // Kør migrationer for ALLE connection strings ved opstart (en gang)
            using (var scope = app.Services.CreateScope())
            {
                var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var allConns = cfg.GetSection("ConnectionStrings").GetChildren();

                foreach (var c in allConns)
                {
                    var name = c.Key;     // fx "Europe"
                    var cs = c.Value;   // selve connection string'en
                    if (string.IsNullOrWhiteSpace(cs)) continue;

                    var opts = new DbContextOptionsBuilder<ArticleDbContext>()
                        .UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
                        .Options;

                    Console.WriteLine($"[Migrate] {name}");
                    using var db = new ArticleDbContext(opts);
                    db.Database.Migrate();
                }
            }

            app.MapGet("/health", () => Results.Ok(new { ok = true, service = "article-service" }));
            app.MapGet("/", () => Results.Redirect("/swagger"));

            if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
            {
                // Swagger JSON + UI available at /swagger
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            // Prometheus HTTP metrics (place before handlers so it observes them)
            app.UseHttpMetrics();
            // Expose /metrics endpoint
            app.MapMetrics();
            
            app.MapControllers();

            // add traceId into all logs + request logging
            app.UseTraceIdEnricher();
            app.UseSerilogRequestLogging();
            
            app.Run();
        }
    }

    // ----------------- RegionResolver -----------------
    public interface IRegionResolver
    {
        string ResolveRegion();
        string ResolveConnection(IConfiguration cfg);
    }

    public class RegionResolver : IRegionResolver
    {
        private readonly IHttpContextAccessor _http;

        public RegionResolver(IHttpContextAccessor http) => _http = http;

        public string ResolveRegion()
        {
            var ctx = _http.HttpContext;
            var header = ctx?.Request.Headers["X-Region"].ToString();
            var query = ctx?.Request.Query["region"].ToString();

            var raw = !string.IsNullOrWhiteSpace(header) ? header : query;

            return Normalize(string.IsNullOrWhiteSpace(raw) ? "Global" : raw);
        }

        public string ResolveConnection(IConfiguration cfg)
        {
            var region = ResolveRegion();
            // ConnectionStrings:<Region> eller fallback til Default
            var cs = cfg.GetConnectionString(region);
            if (string.IsNullOrWhiteSpace(cs))
                cs = cfg.GetConnectionString("Default"); // fallback hvis region ikke fundet

            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("No connection string found (region or Default).");

            return cs!;
        }

        private static string Normalize(string r) => r.ToLowerInvariant() switch
        {
            "africa" => "Africa",
            "asia" => "Asia",
            "europe" => "Europe",
            "northamerica" => "NorthAmerica",
            "southamerica" => "SouthAmerica",
            "oceania" => "Oceania",
            "antarctica" => "Antarctica",
            "global" => "Global",
            _ => "Global"
        };
    }
}