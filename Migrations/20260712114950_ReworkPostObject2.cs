using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramScheduler.Migrations
{
    /// <inheritdoc />
    public partial class ReworkPostObject2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                     ALTER TABLE "Posts"
                                     ALTER COLUMN "ChatId"
                                     TYPE bigint
                                     USING "ChatId"::bigint;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                     ALTER TABLE "Posts"
                                     ALTER COLUMN "ChatId"
                                     TYPE text
                                     USING "ChatId"::text;
                                 """);
        }
    }
}
