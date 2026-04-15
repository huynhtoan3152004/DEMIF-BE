using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demif.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAccessEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonAccessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccessType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "detail"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonAccessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonAccessEvents_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonAccessEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonAccessEvents_AccessType",
                table: "LessonAccessEvents",
                column: "AccessType");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAccessEvents_LessonId_AccessedAt",
                table: "LessonAccessEvents",
                columns: new[] { "LessonId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonAccessEvents_UserId_LessonId",
                table: "LessonAccessEvents",
                columns: new[] { "UserId", "LessonId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonAccessEvents");
        }
    }
}
