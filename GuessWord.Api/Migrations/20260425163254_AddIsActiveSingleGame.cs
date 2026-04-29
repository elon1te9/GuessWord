using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuessWord.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveSingleGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GamePlayers_UserId",
                table: "GamePlayers");

            migrationBuilder.AddColumn<bool>(
                name: "IsActiveSingleGame",
                table: "GamePlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_UserId_IsActiveSingleGame",
                table: "GamePlayers",
                columns: new[] { "UserId", "IsActiveSingleGame" },
                unique: true,
                filter: "\"IsActiveSingleGame\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GamePlayers_UserId_IsActiveSingleGame",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "IsActiveSingleGame",
                table: "GamePlayers");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_UserId",
                table: "GamePlayers",
                column: "UserId");
        }
    }
}
