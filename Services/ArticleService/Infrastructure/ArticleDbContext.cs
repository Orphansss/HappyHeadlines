using ArticleService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Infrastructure;

public class ArticleDbContext(DbContextOptions<ArticleDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Article>(e =>
        {
            e.ToTable("Articles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();   // We provide the Ids
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Summary).HasMaxLength(500);
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.PublishedAt).IsRequired();
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => x.AuthorId);
        });

    }
}