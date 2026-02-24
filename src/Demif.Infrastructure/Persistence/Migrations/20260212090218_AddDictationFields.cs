using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demif.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDictationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DictationTemplate",
                table: "Lessons",
                newName: "TimedTranscript");

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "Lessons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MediaType",
                table: "Lessons",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DictationTemplates",
                table: "Lessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DictationTemplates",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "TimedTranscript",
                table: "Lessons",
                newName: "DictationTemplate");

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "Lessons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MediaType",
                table: "Lessons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
