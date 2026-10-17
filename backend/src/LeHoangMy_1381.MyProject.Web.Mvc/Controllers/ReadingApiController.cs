using LeHoangMy_1381.MyProject.Controllers;
using LeHoangMy_1381.MyProject.Reading;
using LeHoangMy_1381.MyProject.Toeic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Web.Controllers;

// These routes accept a bearer token only. Cookie-based MVC retains CSRF checks.
[Route("api/Reading"), Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme), IgnoreAntiforgeryToken]
public class ReadingApiController : MyProjectControllerBase
{
    private readonly IReadingAppService _reading;
    public ReadingApiController(IReadingAppService reading) { _reading=reading; }
    [HttpGet("Exams")] public Task<List<ExamSummary>> Exams() => _reading.GetExamsAsync();
    [HttpGet("Attempts")] public Task<List<AttemptState>> Attempts() => _reading.GetAttemptsAsync();
    [HttpGet("Attempts/{id}")] public Task<AttemptState> State(string id) => _reading.GetStateAsync(id);
    [HttpGet("Attempts/{id}/Paper")] public Task<ReadingPaper> Paper(string id) => _reading.GetPaperAsync(id);
    [HttpGet("Attempts/{id}/Review")] public Task<AttemptReview> Review(string id) => _reading.GetReviewAsync(id);
    [HttpPost("Attempts")] public Task<AttemptState> Start([FromBody] StartReadingInput input) => _reading.StartAsync(input);
    [HttpPost("Answer")] public Task<SavedAnswer> Save([FromBody] SaveReadingInput input) => _reading.SaveAsync(input);
    [HttpPost("Attempts/{id}/Submit")] public Task<AttemptState> Submit(string id) => _reading.SubmitAsync(id);
}
