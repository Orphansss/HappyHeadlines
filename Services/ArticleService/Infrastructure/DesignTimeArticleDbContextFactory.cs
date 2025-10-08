using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArticleService.Infrastructure;

public sealed class DesignTimeArticleDbContextFactory : IDesignTimeDbContextFactory<ArticleDbContext>
{
    public ArticleDbContext CreateDbContext(string[] args)
    {
        // Load configuration from project root so we can read ConnectionStrings
        var basePath = Directory.GetCurrentDirectory();
        var cfg = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Prefer Default, then Global (matches your compose)
        var cs = cfg.GetConnectionString("Default")
                 ?? cfg.GetConnectionString("Global")
                 ?? "Server=localhost,11433;Database=ArticlesDb_Global;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=True;TrustServerCertificate=True;";

        var opts = new DbContextOptionsBuilder<ArticleDbContext>()
            .UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
            .Options;

        return new ArticleDbContext(opts);
    }
}