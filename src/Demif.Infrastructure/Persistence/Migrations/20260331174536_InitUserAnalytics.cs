using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demif.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitUserAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAnalytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalExercisesCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalLessonsCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalLearningMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AvgDictationScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    AvgShadowingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    HighestScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PerfectScoresCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FirstActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalActiveDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StreakFreezesUsed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AvgSessionsPerWeek = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    MostActiveDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    MostActiveHour = table.Column<int>(type: "integer", nullable: true),
                    LessonTypeStats = table.Column<string>(type: "jsonb", nullable: true),
                    LevelStats = table.Column<string>(type: "jsonb", nullable: true),
                    CategoryStats = table.Column<string>(type: "jsonb", nullable: true),
                    TopLessons = table.Column<string>(type: "jsonb", nullable: true),
                    RecentLessons = table.Column<string>(type: "jsonb", nullable: true),
                    WeeklyTrends = table.Column<string>(type: "jsonb", nullable: true),
                    MonthlyTrends = table.Column<string>(type: "jsonb", nullable: true),
                    SkillsBreakdown = table.Column<string>(type: "jsonb", nullable: true),
                    WeeklyImprovement = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    MonthlyImprovement = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    CurrentSubscriptionTier = table.Column<string>(type: "text", nullable: true),
                    SubscriptionEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalAmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    SuccessfulPaymentsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastPaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalLogins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastLoginDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlogViewsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EngagementScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAnalytics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAnalytics_UserId",
                table: "UserAnalytics",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAnalytics");
        }
    }
}
