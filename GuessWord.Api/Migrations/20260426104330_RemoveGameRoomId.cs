using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuessWord.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGameRoomId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Rooms_RoomId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_RoomId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_RoomId",
                table: "Games",
                column: "RoomId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Rooms_RoomId",
                table: "Games",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id");
        }
    }
}
