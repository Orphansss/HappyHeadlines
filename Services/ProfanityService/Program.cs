using ProfanityService.Infrastructure.Data;
using ProfanityService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ProfanityService;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddScoped<IProfanityService, Applications.ProfanitySerivce>();
        builder.Services.AddDbContext<ProfanityDbContext>(o =>
            o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok(new { ok = true, service = "profanity-service" }));
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