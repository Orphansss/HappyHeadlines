using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AS_API.Migrations
{
    public partial class AlignTableName_Article : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: DB already has the correct table name "Article".
            // This migration just advances the EF snapshot so startup Migrate() stops complaining.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op.
        }
    }
}