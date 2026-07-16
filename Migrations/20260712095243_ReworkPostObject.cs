using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramScheduler.Migrations
{
    /// <inheritdoc />
    public partial class ReworkPostObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HangfireJobId",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSent",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangfireJobId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsSent",
                table: "Posts");
        }
    }
}
