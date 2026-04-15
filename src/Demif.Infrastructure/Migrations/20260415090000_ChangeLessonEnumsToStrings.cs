using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demif.Infrastructure.Migrations
{
    public partial class ChangeLessonEnumsToStrings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Lessons""
    ALTER COLUMN ""LessonType"" TYPE text
    USING CASE ""LessonType""
        WHEN 1 THEN 'Dictation'
        WHEN 2 THEN 'Shadowing'
        ELSE 'Dictation'
    END,
    ALTER COLUMN ""Level"" TYPE text
    USING CASE ""Level""
        WHEN 1 THEN 'Beginner'
        WHEN 2 THEN 'Intermediate'
        WHEN 3 THEN 'Advanced'
        WHEN 4 THEN 'Expert'
        ELSE 'Beginner'
    END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Lessons""
    ALTER COLUMN ""LessonType"" TYPE integer
    USING CASE ""LessonType""
        WHEN 'Dictation' THEN 1
        WHEN 'Shadowing' THEN 2
        ELSE 1
    END,
    ALTER COLUMN ""Level"" TYPE integer
    USING CASE ""Level""
        WHEN 'Beginner' THEN 1
        WHEN 'Intermediate' THEN 2
        WHEN 'Advanced' THEN 3
        WHEN 'Expert' THEN 4
        ELSE 1
    END;
");
        }
    }
}