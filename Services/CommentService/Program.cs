using CommentService.Data;
using CommentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CommentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<ICommentService, Services.CommentService>();
            builder.Services.AddDbContext<CommentDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

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
            app.Run();
        }
    }
}
