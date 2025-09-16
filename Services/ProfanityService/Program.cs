using Microsoft.EntityFrameworkCore;
using ProfanityService.Data;
using ProfanityService.Services;

var builder = WebApplication.CreateBuilder(args);

// Læs fra miljøvariablen "PROFANITY_DB" (sat via docker-compose /.env)
var connStr = Environment.GetEnvironmentVariable("PROFANITY_DB")
             ?? builder.Configuration["PROFANITY_DB"] // fallback hvis den sættes som env key=value
             ?? throw new InvalidOperationException("Missing PROFANITY_DB environment variable");

builder.Services.AddDbContext<ProfanityService.Data.ProfanityDbContext>(opt =>
    opt.UseSqlServer(connStr));
builder.Services.AddScoped<IProfanityFilter, ProfanityFilter>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        ok = true,
        service = "profanity-service",
        timestamp = DateTime.UtcNow
    })
);

app.MapGet("/", () => Results.Redirect("/swagger"));

// Auto-migrate ved opstart (du har det her i forvejen)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProfanityDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
