using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProfanityService.Data
{
    public class ProfanityDbContextFactory : IDesignTimeDbContextFactory<ProfanityDbContext>
    {
        public ProfanityDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("PROFANITY_DB")
                     ?? "Server=localhost,14340;Database=ProfanityDb;User Id=sa;Password=P@ssword1;Encrypt=True;TrustServerCertificate=True;";
            var options = new DbContextOptionsBuilder<ProfanityDbContext>()
                .UseSqlServer(cs)
                .Options;
            return new ProfanityDbContext(options);
        }
    }
}
