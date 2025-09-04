
using AS_API.Data;
using Microsoft.EntityFrameworkCore;

namespace AS_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // read ConnectionStrings:Default from env/appsettings/User-Secrets
            builder.Services.AddDbContext<ArticleDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // auto-migrate on startup (dev)
            using (var scope = app.Services.CreateScope())
                scope.ServiceProvider.GetRequiredService<ArticleDbContext>().Database.Migrate();

            app.MapGet("/health", () => Results.Ok(new { ok = true, service = "article-service" }));
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapControllers();
            app.Run();
        }
    }
}
