
using DraftService.Data;
using DraftService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DraftService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ----- Serilog setup (central logging to Seq) -----
            var serviceName = "DraftService";

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


            // Add services to the container.
            builder.Services.AddScoped<IDraftService, Services.DraftService>();
            builder.Services.AddDbContext<DraftDbContext>(o =>
               o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DraftService.Data.DraftDbContext>();
                db.Database.Migrate();  // applies pending migrations automatically
            }

            app.MapGet("/health", () => Results.Ok(new { ok = true, service = "draft-service" }));
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
            app.UseAuthorization();
            // Auto-migrate at startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DraftDbContext>();
                db.Database.Migrate();
            }

            app.MapControllers();


            app.Run();
        }
    }
}
