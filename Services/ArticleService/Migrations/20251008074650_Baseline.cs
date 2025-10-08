using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleService.Migrations
{
    public partial class Baseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: Database already contains the current schema (Articles + indexes).
            // This migration only baselines the EF history.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op.
        }
    }
}