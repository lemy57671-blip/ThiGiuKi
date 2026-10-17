using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Toeic;

public record ReadingActor(long UserId, int? TenantId, bool HostManagement = false);
public class ExamSummary
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string SourceId { get; set; }
    public string Version { get; set; }
    public string Status { get; set; }
    public int DurationMinutes { get; set; }
    public int QuestionCount { get; set; }
    public int VerifiedKeys { get; set; }
    public string ExamRowVersion { get; set; }
    public bool CanDelete { get; set; }
}
public class PaperOption
{
    public int Id { get; set; }
    public string Label { get; set; }
    public string Content { get; set; }
}
public class PaperQuestion
{
    public int Id { get; set; }
    public int OriginalNumber { get; set; }
    public int DisplayOrder { get; set; }
    public int PartNumber { get; set; }
    public int? GroupId { get; set; }
    public int? GapLabel { get; set; }
    public string Stem { get; set; }
    public List<PaperOption> Options { get; set; } = new();
}
public class PaperBlock
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string BlockType { get; set; }
    public string TextContent { get; set; }
    public string ImageBase64 { get; set; }
    public string MimeType { get; set; }
}
public class PaperGroup
{
    public int Id { get; set; }
    public int PartNumber { get; set; }
    public int FirstQuestionNumber { get; set; }
    public int LastQuestionNumber { get; set; }
    public List<PaperBlock> Blocks { get; set; } = new();
}
public class ReadingPaper
{
    public ExamSummary Exam { get; set; }
    public List<PaperQuestion> Questions { get; set; } = new();
    public List<PaperGroup> Groups { get; set; } = new();
}
public class SavedAnswer
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string RowVersion { get; set; }
}
public class AttemptState
{
    // BIGINT is a decimal string on the wire, including on Flutter Web.
    public string Id { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime ServerNowUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public int TotalQuestions { get; set; }
    public int? CorrectCount { get; set; }
    public int? WrongCount { get; set; }
    public int? SkippedCount { get; set; }
    public decimal? AccuracyPercent { get; set; }
    public List<SavedAnswer> Answers { get; set; } = new();
    public List<PartResult> PartResults { get; set; } = new();
}
public class PartResult
{
    public int PartNumber { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int Skipped { get; set; }
}
public class QuestionReview
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public int CorrectOptionId { get; set; }
    public string Hint { get; set; }
    public string Outcome { get; set; }
}
public class AttemptReview
{
    public ReadingPaper Paper { get; set; }
    public List<QuestionReview> Questions { get; set; } = new();
}
public class StartReadingInput
{
    [Range(1, int.MaxValue)] public int ExamId { get; set; }
    public Guid ClientRequestId { get; set; }
}
public class SaveReadingInput
{
    [Required] public string AttemptId { get; set; }
    [Range(1, int.MaxValue)] public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    [Required] public string ExpectedRowVersion { get; set; }
}
public class EditQuestionInput
{
    [Range(1, int.MaxValue)] public int ExamId { get; set; }
    [Range(1, int.MaxValue)] public int QuestionId { get; set; }
    [Required] public string ExamRowVersion { get; set; }
    public string Stem { get; set; }
    [Required] public List<PaperOption> Options { get; set; }
    public int CorrectOptionId { get; set; }
    [Required, StringLength(8000)] public string Hint { get; set; }
    public bool ShuffleQuestion { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool ConfirmAnswer { get; set; }
}
public class AdminQuestion
{
    public PaperQuestion Question { get; set; }
    public int CorrectOptionId { get; set; }
    public string Hint { get; set; }
    public bool IsVerified { get; set; }
    public bool ShuffleQuestion { get; set; }
    public bool ShuffleOptions { get; set; }
}
public class AdminPreview
{
    public ReadingPaper Paper { get; set; }
    public string RowVersion { get; set; }
    public List<AdminQuestion> Questions { get; set; } = new();
}
public class ReviewInput
{
    public int ExamId { get; set; }
    [Required] public string ExamRowVersion { get; set; }
    public bool ConfirmReviewed { get; set; }
}
public class ManageReadingExamInput
{
    [Range(1, int.MaxValue)] public int ExamId { get; set; }
    [Required] public string ExamRowVersion { get; set; }
}
public class EditReadingBlockInput
{
    public int ExamId { get; set; }
    public int BlockId { get; set; }
    [Required] public string ExamRowVersion { get; set; }
    public string TextContent { get; set; }
    public byte[] ImageData { get; set; }
}
public class WrongQuestionStat
{
    public int OriginalNumber { get; set; }
    public int WrongCount { get; set; }
}
public class ExamStatistics
{
    public ExamSummary Exam { get; set; }
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public decimal? AverageAccuracyPercent { get; set; }
    public List<WrongQuestionStat> TopWrong { get; set; } = new();
    public List<ScoreBucket> Distribution { get; set; } = new();
    public List<PartResult> Parts { get; set; } = new();
}
public class ScoreBucket
{
    public string Label { get; set; }
    public int Count { get; set; }
}
public class ImportReadingResult
{
    public int ExamId { get; set; }
    public string Version { get; set; }
    public string Status { get; set; }
    public int Questions { get; set; }
    public int Images { get; set; }
}
public interface IReadingStore
{
    Task<List<ExamSummary>> ListAsync(ReadingActor actor, bool management);
    Task<AttemptState> StartAsync(ReadingActor actor, StartReadingInput input);
    Task<AttemptState> StateAsync(ReadingActor actor, long id);
    Task<List<AttemptState>> AttemptsAsync(ReadingActor actor);
    Task<ReadingPaper> PaperAsync(ReadingActor actor, long attemptId);
    Task<AttemptReview> ReviewAttemptAsync(ReadingActor actor, long attemptId);
    Task<SavedAnswer> SaveAsync(ReadingActor actor, SaveReadingInput input);
    Task<AttemptState> SubmitAsync(ReadingActor actor, long id);
    Task<AdminPreview> PreviewAsync(ReadingActor actor, int examId);
    Task EditAsync(ReadingActor actor, EditQuestionInput input);
    Task EditBlockAsync(ReadingActor actor, EditReadingBlockInput input);
    Task ReviewAsync(ReadingActor actor, ReviewInput input);
    Task PublishAsync(ReadingActor actor, ReviewInput input);
    Task DeleteDraftAsync(ReadingActor actor, ManageReadingExamInput input);
    Task ArchiveAsync(ReadingActor actor, ManageReadingExamInput input);
    Task ValidateAsync(ReadingActor actor, int examId);
    Task<ExamStatistics> StatisticsAsync(ReadingActor actor, int examId, int? part = null);
    Task<ImportReadingResult> ImportAsync(ReadingActor actor, byte[] data, string fileName, bool createNewVersion = false);
    Task ExpireAsync();
}
