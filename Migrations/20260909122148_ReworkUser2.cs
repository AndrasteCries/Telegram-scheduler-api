using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramScheduler.Migrations
{
    /// <inheritdoc />
    public partial class ReworkUser2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "TelegramAccounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "TelegramAccounts");
        }
    }
}
