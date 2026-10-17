using Abp.Runtime.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace LeHoangMy_1381.MyProject.Web.Tests.Controllers;

public class ReadingAccessController_Tests : MyProjectWebTestBase
{
    private void ConfigureAccess(bool enabled, string environment = "Development")
    {
        Resolve<IWebHostEnvironment>().EnvironmentName = environment;
        Resolve<IConfiguration>()["Reading:OpenAccess"] = enabled.ToString();
    }

    private async Task<JsonElement> OpenStudentAsync(string visitorKey = null)
    {
        var response = await Client.PostAsync("/api/ReadingAccess/Student",
            new StringContent(JsonSerializer.Serialize(new { visitorKey }), Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        return (root.TryGetProperty("result", out var result) ? result : root).Clone();
    }

    [Fact]
    public async Task Student_ReusesBrowserIdentity_AndSeparatesOtherVisitors()
    {
        ConfigureAccess(true);
        var first = await OpenStudentAsync();
        var key = first.GetProperty("visitorKey").GetString();
        key.Length.ShouldBe(43);
        var same = await OpenStudentAsync(key);
        var other = await OpenStudentAsync();
        same.GetProperty("userId").GetInt64().ShouldBe(first.GetProperty("userId").GetInt64());
        other.GetProperty("userId").GetInt64().ShouldNotBe(first.GetProperty("userId").GetInt64());

        var tenantId = first.GetProperty("tenantId").GetInt32();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(first.GetProperty("accessToken").GetString());
        token.Claims.Single(c => c.Type == AbpClaimTypes.TenantId).Value.ShouldBe(tenantId.ToString());
        var userId = first.GetProperty("userId").GetInt64();
        var user = UsingDbContext(db => db.Users.Single(u => u.Id == userId));
        user.TenantId.ShouldBe(tenantId);
        user.UserName.ShouldStartWith("reading.visitor.");
        UsingDbContext(db => db.UserRoles.Count(r => r.UserId == userId)).ShouldBe(0);
    }

    [Theory]
    [InlineData(false, "Development")]
    [InlineData(true, "Production")]
    public async Task ClosedMode_DoesNotIssueTeacherCookieOrStudentToken(bool enabled, string environment)
    {
        ConfigureAccess(enabled, environment);
        var before = UsingDbContext(db => db.Users.Count());
        var student = await Client.PostAsync("/api/ReadingAccess/Student",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        student.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var teacher = await Client.GetAsync("/ReadingAccess/Teacher");
        teacher.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        teacher.Headers.Contains("Set-Cookie").ShouldBeFalse();
        UsingDbContext(db => db.Users.Count()).ShouldBe(before);
    }

    [Fact]
    public async Task Student_RejectsMalformedIdentityBeforeCreatingAnyAccount()
    {
        ConfigureAccess(true);
        var before = UsingDbContext(db => db.Users.Count());
        var response = await Client.PostAsync("/api/ReadingAccess/Student",
            new StringContent("{\"visitorKey\":\"admin\"}", Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        UsingDbContext(db => db.Users.Count()).ShouldBe(before);
    }

    [Fact]
    public async Task Teacher_EntersDefaultTenant_AndRejectsExternalReturnUrl()
    {
        ConfigureAccess(true);
        var response = await Client.GetAsync("/ReadingAccess/Teacher?returnUrl=https%3A%2F%2Fexample.com");
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ToString().ShouldBe("/Reading");
        response.Headers.Contains("Set-Cookie").ShouldBeTrue();
    }
}
