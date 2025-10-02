using System.Text.Json.Serialization;
using PublisherService.Application.Abstractions;
using PublisherService.Infrastructure.Messaging;
using PublisherService.Infrastructure.Profanity;
using PublisherService.Application.UseCases.PublishArticle;

var builder = WebApplication.CreateBuilder(args);

// ---- Configure options ----
builder.Services.Configure<MessagingOptions>(
    builder.Configuration.GetSection(MessagingOptions.SectionName));

// ---- Profanity HTTP client ----
builder.Services
    .AddHttpClient<IProfanityClient, ProfanityClientHttp>(client =>
    {
        var baseUrl = builder.Configuration["PROFANITY_BASEURL"]
                      ?? throw new InvalidOperationException("PROFANITY_BASEURL not set");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(2);
    });

// ---- RabbitMQ publisher ----
builder.Services.AddSingleton<IArticleQueuePublisher, ArticleQueuePublisherRabbit>();

builder.Services.AddScoped<PublishArticleHandler>();
builder.Services.AddSingleton<IIdGenerator, MonotonicIdGenerator>();

// ---- Controllers + Swagger ----
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {   // Serialize/deserialize enums as strings. Case-insensitive by default
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---- Middleware pipeline ----
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    // Swagger JSON + UI available at /swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();