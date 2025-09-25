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

            modelBuilder.Entity<Draft>().ToTable("Draft");

            modelBuilder.Entity<Draft>().HasKey(d => d.Id);

            modelBuilder.Entity<Draft>().Property(d => d.ArticleId).IsRequired();
            modelBuilder.Entity<Draft>().Property(d => d.Title).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<Draft>().Property(d => d.Body).IsRequired();
            modelBuilder.Entity<Draft>().Property(d => d.Author).IsRequired().HasMaxLength(100);

            modelBuilder.Entity<Draft>().HasIndex(d => d.ArticleId);       // fast lookup by article
            modelBuilder.Entity<Draft>().Property(d => d.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            modelBuilder.Entity<Draft>().Property(d => d.LastModified).HasDefaultValueSql("GETUTCDATE()");
        }
    }

    
}
