using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace GuessWord.Api.Migrations
{
    /// <inheritdoc />
    public partial class SwitchWordEmbeddingsToSociation800 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Words\" SET \"Embedding\" = NULL;");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "Words",
                type: "vector(800)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(1024)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Words\" SET \"Embedding\" = NULL;");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "Words",
                type: "vector(1024)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(800)",
                oldNullable: true);
        }
    }
}
