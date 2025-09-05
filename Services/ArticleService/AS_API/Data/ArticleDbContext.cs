using AS_API.Models;
using Microsoft.EntityFrameworkCore;
using AS_API.Models;

namespace AS_API.Data
{
    public class ArticleDbContext(DbContextOptions<ArticleDbContext> options) : DbContext(options)
    {
        public DbSet<Article> Articles => Set<Article>();
        
    }
}
