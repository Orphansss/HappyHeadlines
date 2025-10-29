using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriberService.Application.Abstractions;
using SubscriberService.Infrastructure.Messaging.Topology;
using SubscriberService.Infrastructure.Persistence;
using SubscriberService.Infrastructure.Persistence.Repositories;

namespace SubscriberService.Infrastructure.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("SubscriberDb");

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing connection string: SubscriberDb");

        services.AddDbContext<SubscriberDbContext>(opt =>
            opt.UseSqlServer(cs));

        services.AddScoped<ISubscriberRepository, SubscriberRepository>();

        return services;
    }
    public static IServiceCollection AddRabbitMqTopology(this IServiceCollection services)
    {
        services.AddHostedService<TopologyBootstrapper>();
        return services;
    }
    
}