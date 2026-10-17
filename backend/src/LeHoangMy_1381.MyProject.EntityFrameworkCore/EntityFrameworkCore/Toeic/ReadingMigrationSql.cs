using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LeHoangMy_1381.MyProject.EntityFrameworkCore.Toeic;

public static class ReadingMigrationSql
{
    public static void ApplyV1(MigrationBuilder migrationBuilder) => Apply(migrationBuilder, "ToeicReadingV1.sql");
    public static void ApplyV2(MigrationBuilder migrationBuilder) => Apply(migrationBuilder, "ToeicReadingV2.sql");

    private static void Apply(MigrationBuilder migrationBuilder, string resourceName)
    {
        using var stream = typeof(ReadingMigrationSql).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded TOEIC migration SQL is missing.");
        using var reader = new StreamReader(stream);
        // EF chooses the target through its configured connection. The exported
        // SSMS scripts retain their fixed-name guard, but that guard must not
        // prevent a fresh EF deployment to an isolated integration-test database.
        const string exportGuard = "IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;";
        var sql = reader.ReadToEnd().Replace(exportGuard, string.Empty, StringComparison.Ordinal);
        foreach (var batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(batch)) migrationBuilder.Sql(batch);
        }
    }
}
