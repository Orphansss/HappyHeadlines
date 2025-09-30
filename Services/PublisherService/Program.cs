using PublisherService.Application.Abstractions;
using PublisherService.Infrastructure.Messaging;
using PublisherService.Infrastructure.Profanity;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<MessagingOptions>(builder.Configuration.GetSection(MessagingOptions.SectionName));

// Profanity HTTP client
builder.Services
    .AddHttpClient<IProfanityClient, ProfanityClientHttp>(client =>
    {
        var baseUrl = builder.Configuration["PROFANITY_BASEURL"]
                      ?? throw new InvalidOperationException("PROFANITY_BASEURL not set");
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(2);
    });

// Rabbit publisher
builder.Services.AddSingleton<IArticleQueuePublisher, ArticleQueuePublisherRabbit>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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