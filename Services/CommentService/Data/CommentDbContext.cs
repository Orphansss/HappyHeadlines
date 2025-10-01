using CommentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Data;

public class CommentDbContext : DbContext
{
    public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options)
    {
    }
    
    public DbSet<Comment> Comments => Set<Comment>();
    
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Comment>(e =>
        {
            e.ToTable("Comments");               
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).IsRequired().HasMaxLength(500);
            e.Property(x => x.PublishedAt).IsRequired();
            e.HasIndex(x => x.PublishedAt);
            e.HasIndex(x => x.AuthorId);
        });
    }
}