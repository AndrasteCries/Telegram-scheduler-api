using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TelegramScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_Users_UserId",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_Channels_TelegramChatId",
                table: "Channels");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Channels",
                newName: "TelegramAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Channels_UserId",
                table: "Channels",
                newName: "IX_Channels_TelegramAccountId");

            migrationBuilder.CreateTable(
                name: "TelegramAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    TdlibSessionPath = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramAccounts_UserId",
                table: "TelegramAccounts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_TelegramAccounts_TelegramAccountId",
                table: "Channels",
                column: "TelegramAccountId",
                principalTable: "TelegramAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_TelegramAccounts_TelegramAccountId",
                table: "Channels");

            migrationBuilder.DropTable(
                name: "TelegramAccounts");

            migrationBuilder.RenameColumn(
                name: "TelegramAccountId",
                table: "Channels",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Channels_TelegramAccountId",
                table: "Channels",
                newName: "IX_Channels_UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_TelegramChatId",
                table: "Channels",
                column: "TelegramChatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_Users_UserId",
                table: "Channels",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
