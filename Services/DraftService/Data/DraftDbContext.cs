using DraftService.Models;
using Microsoft.EntityFrameworkCore;    

namespace DraftService.Data
{
    public class DraftDbContext : DbContext
    {
        public DraftDbContext(DbContextOptions<DraftDbContext> options) : base(options)
        {
        }

        public DbSet<Draft> Drafts => Set<Draft>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            

            base.OnModelCreating(modelBuilder);

            // Set up the Draft entity with appropriate contraints, keys and default values.
            modelBuilder.Entity<Draft>().ToTable("Draft");
            modelBuilder.Entity<Draft>().HasKey(d => d.Id); 
            modelBuilder.Entity<Draft>().Property(d => d.Author).IsRequired().HasMaxLength(300);
            modelBuilder.Entity<Draft>().Property(d => d.Content).IsRequired();
            modelBuilder.Entity<Draft>().Property(d => d.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<Draft>().Property(d => d.LastModified).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }   
    }

    
}
