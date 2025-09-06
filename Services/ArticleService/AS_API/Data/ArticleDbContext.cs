using AS_API.Models;
using Microsoft.EntityFrameworkCore;

namespace AS_API.Data
{
    public class ArticleDbContext : DbContext
    {
        public ArticleDbContext(DbContextOptions<ArticleDbContext> options) : base(options) { }

        public DbSet<Article> Articles => Set<Article>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Article>().ToTable("Article"); // Articles skal hedde Article ligesom tabellen i db
        }
    }
}
