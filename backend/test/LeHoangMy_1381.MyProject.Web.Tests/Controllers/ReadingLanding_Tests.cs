using LeHoangMy_1381.MyProject.Models.TokenAuth;
using Shouldly;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace LeHoangMy_1381.MyProject.Web.Tests.Controllers;

public class ReadingLanding_Tests : MyProjectWebTestBase
{
    [Fact]
    public async Task Root_OpensReading_WithoutRemovedPanels()
    {
        await AuthenticateAsync(null, new AuthenticateModel
        {
            UserNameOrEmailAddress = "admin",
            Password = "123qwe"
        });
        var html = await GetResponseAsStringAsync("/");
        html.ShouldContain("id=\"exams-table\"");
        html.ShouldNotContain("sidebar-note");
        html.ShouldNotContain("href=\"/Home\"");
        html.ShouldNotContain("Bản nháp chưa có lượt làm bài có thể xóa.");
    }

    [Theory]
    [InlineData("/Home")]
    [InlineData("/Home/Index")]
    public async Task RemovedHomeEndpoint_ReturnsNotFound(string path)
    {
        await GetResponseAsync(path, HttpStatusCode.NotFound);
    }
}
