using Microsoft.EntityFrameworkCore;
using SubscriberService.Domain.Entities;

namespace SubscriberService.Infrastructure.Persistence;

public class SubscriberDbContext : DbContext
{
    public SubscriberDbContext(DbContextOptions<SubscriberDbContext> options) : base(options) { }
    
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriberDbContext).Assembly);
    }
}