using SubscriberService.Api.Extensions;
using SubscriberService.Infrastructure.DI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure (DbContext + Repository)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Automatically apply migrations at startup
await app.MigrateDatabaseAsync();

app.MapGet("/", () => "SubscriberService API");
app.Run();