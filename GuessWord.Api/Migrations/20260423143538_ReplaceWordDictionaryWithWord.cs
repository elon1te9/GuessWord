using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuessWord.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceWordDictionaryWithWord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_WordDictionaries_SecretWordId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "WordDictionaries");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Words_SecretWordId",
                table: "Games",
                column: "SecretWordId",
                principalTable: "Words",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Words_SecretWordId",
                table: "Games");

            migrationBuilder.CreateTable(
                name: "WordDictionaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Word = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordDictionaries", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Games_WordDictionaries_SecretWordId",
                table: "Games",
                column: "SecretWordId",
                principalTable: "WordDictionaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
