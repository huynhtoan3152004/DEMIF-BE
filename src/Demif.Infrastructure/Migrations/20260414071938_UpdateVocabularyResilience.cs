using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demif.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVocabularyResilience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCorrect",
                table: "UserVocabularies",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveCorrect",
                table: "UserVocabularies");
        }
    }
}
