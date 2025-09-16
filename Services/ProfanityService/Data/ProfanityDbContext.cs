using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data
{
    public class ProfanityDbContext : DbContext
    {
        public ProfanityDbContext(DbContextOptions<ProfanityDbContext> options) : base(options) { }

        public DbSet<Word> Words => Set<Word>();
    }
}
