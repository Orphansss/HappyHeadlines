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

            // Add services to the container.
            builder.Services.AddScoped<ICommentService, Services.CommentService>();
            builder.Services.AddDbContext<CommentDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
            
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            
            app.MapGet("/health", () => Results.Ok(new { ok = true, service = "comment-service" }));
            app.MapGet("/", () => Results.Redirect("/swagger"));
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            
            app.MapControllers();

            app.Run();
        }
    }
}
