using Abp.Dependency;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LeHoangMy_1381.MyProject.Web.Reading;

/// <summary>Controls the local demonstration entry points without changing Reading authorization.</summary>
public class ReadingOpenAccess : ITransientDependency
{
    public const string TeacherClaim = "reading_open_access_teacher";
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ReadingOpenAccess(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public bool Enabled => _environment.IsDevelopment() && _configuration.GetValue<bool>("Reading:OpenAccess");
}
