using SubscriberService.Api.Extensions;
using SubscriberService.Infrastructure.DI;                
using SubscriberService.Infrastructure.FeatureToggles;    
using SubscriberService.Infrastructure.Messaging;        
using SubscriberService.Application.Abstractions;
using SubscriberService.Api.Middleware;
using Monitoring;

var builder = WebApplication.CreateBuilder(args);

// central logging + tracing (Seq + Jaeger) via your helper
builder.AddMonitoring("NewsletterService");

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB + Repo
builder.Services.AddInfrastructure(builder.Configuration);

// FeatureHub and toggle adapter
builder.Services.AddFeatureHub(builder.Configuration);
builder.Services.AddSingleton<IFeatureToggle, FeatureHubToggle>();

// RabbitMQ (publisher)
builder.Services.AddSingleton<ISubscriberPublisher, RabbitMqSubscriberPublisher>();
builder.Services.AddRabbitMqTopology();

// Application service
builder.Services.AddScoped<ISubscriberService, SubscriberService.Application.Services.SubscriberService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Gate all requests when 'subscriber-service.enabled' is false (allows swagger/health)
app.UseFeatureToggleGate();

// Automatically apply migrations at startup
await app.MigrateDatabaseAsync();

app.MapControllers();
app.Run();
