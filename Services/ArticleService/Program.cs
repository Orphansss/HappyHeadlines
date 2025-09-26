using ArticleService.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ArticleService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ----- Serilog setup (central logging to Seq) -----
            var serviceName = "ArticleService";

            var builder = WebApplication.CreateBuilder(args);

            // create Serilog logger early
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", serviceName)
                .Enrich.WithEnvironmentName()                               // adds Environment property
                .WriteTo.Console()
                .WriteTo.Seq(builder.Configuration["Seq:Url"]                // from Seq__Url env
                             ?? "http://localhost:5341")                     // fallback when running outside compose
                .CreateLogger();

            builder.Host.UseSerilog(); // plug Serilog into ASP.NET

            

            // Gør HttpContext tilgængelig (til at læse X-Region / ?region=)
            builder.Services.AddHttpContextAccessor();

            // Registrér vores RegionResolver (finder den rigtige connection pr. request)
            builder.Services.AddScoped<IRegionResolver, RegionResolver>();

            // Konfigurér DbContext med connection string valgt ved runtime (per request)
            builder.Services.AddDbContext<ArticleDbContext>((sp, o) =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var resolver = sp.GetRequiredService<IRegionResolver>();
                var conn = resolver.ResolveConnection(cfg); // <- vælg pr. request
                o.UseSqlServer(conn, sql => sql.EnableRetryOnFailure());
            });

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

            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapControllers();

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