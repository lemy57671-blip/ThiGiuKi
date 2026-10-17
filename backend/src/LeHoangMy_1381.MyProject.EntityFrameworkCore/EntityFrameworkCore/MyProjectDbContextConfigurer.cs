using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace LeHoangMy_1381.MyProject.EntityFrameworkCore;

public static class MyProjectDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<MyProjectDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<MyProjectDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}
