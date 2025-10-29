using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SubscriberService.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SubscriberDbContext>
    {
        public SubscriberDbContext CreateDbContext(string[] args)
        {
            // Use a safe fallback connection string for migrations only
            var cs = "Server=localhost,14342;Database=SubscriberDb;User Id=sa;Password={Your_strong_password123};TrustServerCertificate=True;";
            
            var optionsBuilder = new DbContextOptionsBuilder<SubscriberDbContext>();
            optionsBuilder.UseSqlServer(cs);

            return new SubscriberDbContext(optionsBuilder.Options);
        }
    }
}