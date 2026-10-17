using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeHoangMy_1381.MyProject.Migrations
{
    public partial class HardenAttemptStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => EntityFrameworkCore.Toeic.ReadingMigrationSql.ApplyV2(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => EntityFrameworkCore.Toeic.ReadingMigrationSql.ApplyV1(migrationBuilder);
    }
}
