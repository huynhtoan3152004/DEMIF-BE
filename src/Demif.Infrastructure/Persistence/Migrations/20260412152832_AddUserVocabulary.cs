using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demif.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserVocabularies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Word = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedWord = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Meaning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContextSentence = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CorrectReviews = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsMastered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextReviewAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MasteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserVocabularies_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserVocabularies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_LessonId",
                table: "UserVocabularies",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_NextReviewAt",
                table: "UserVocabularies",
                column: "NextReviewAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_Topic",
                table: "UserVocabularies",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_UserId",
                table: "UserVocabularies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVocabularies_UserId_LessonId_Topic_NormalizedWord",
                table: "UserVocabularies",
                columns: new[] { "UserId", "LessonId", "Topic", "NormalizedWord" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVocabularies");
        }
    }
}
