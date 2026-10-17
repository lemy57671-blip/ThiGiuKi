using System;
using System.Linq;
using LeHoangMy_1381.MyProject.EntityFrameworkCore.Toeic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace LeHoangMy_1381.MyProject.Tests.Toeic;

public class ReadingMigrationSql_Tests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Ef_migrations_support_an_isolated_database_without_removing_business_guards(int version)
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        if (version == 1) ReadingMigrationSql.ApplyV1(builder);
        else ReadingMigrationSql.ApplyV2(builder);
        var sql = string.Join("\n", builder.Operations.Cast<SqlOperation>().Select(x => x.Sql));
        Assert.DoesNotContain("Wrong database: expected TOEICReadingMidterm.", sql);
        Assert.Contains("toeic.StartAttempt", sql);
        Assert.Contains("PUBLISHED", sql);
        Assert.Contains("StudentUserId", sql);
        Assert.Contains("THROW", sql);
    }
}
