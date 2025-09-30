
using DraftService.Data;
using DraftService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Monitoring;

namespace DraftService
{
    public class Program
    {
        public static void Main(string[] args)
        {
          

            var builder = WebApplication.CreateBuilder(args);

            builder.AddMonitoring("DraftService"); // adding SeriLog now through or Monitoring class

      

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
            // add traceId into all logs + nice request logging
            app.UseTraceIdEnricher();
            app.UseSerilogRequestLogging();


            app.Run();
        }
    }
}
