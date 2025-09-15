using ProfanityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProfanityService.Infrastructure.Data;
public class ProfanityDbContext : DbContext
{
    public ProfanityDbContext(DbContextOptions<ProfanityDbContext> options) : base(options) { }
    public DbSet<Profanity> Profanities => Set<Profanity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profanity>().ToTable("Profanity");
    }
}
