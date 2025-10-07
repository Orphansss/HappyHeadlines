using Microsoft.EntityFrameworkCore;

namespace ArticleService.Infrastructure;

public static class RegionDbContextFactory
{
    /// <summary>
    /// Creates a DbContext for the specific database based on region
    /// </summary>
    public static ArticleDbContext CreateDbContext(string? region, IConfiguration cfg)
    {
        var cs = ResolveConnectionString(region, cfg);
        var opts = new DbContextOptionsBuilder<ArticleDbContext>()
            .UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
            .Options;

        return new ArticleDbContext(opts);
    }

    /// <summary>
    /// Turn a region string into the correct connection string with fallbacks
    /// </summary>
    private static string ResolveConnectionString(string? region, IConfiguration cfg)
    {
        var key = region;
        var cs = cfg.GetConnectionString(key);

        if (!string.IsNullOrWhiteSpace(cs)) return cs!;

        var global = cfg.GetConnectionString("Global");
        if (!string.IsNullOrWhiteSpace(global)) return global!;

        var @default = cfg.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(@default)) return @default!;

        throw new InvalidOperationException(
            $"Missing ConnectionStrings for '{key}', and fallbacks 'Global'/'Default' were not provided.");
    }
}
