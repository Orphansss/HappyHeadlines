using Microsoft.EntityFrameworkCore;
using SubscriberService.Infrastructure.Persistence;

namespace SubscriberService.Api.Extensions;

public static class MigrationExtensions
{
    /// <summary>Apply pending EF Core migrations at startup.</summary>
    public static async Task<WebApplication> 
        MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubscriberDbContext>();
        await db.Database.MigrateAsync();
        return app;
    }
}

