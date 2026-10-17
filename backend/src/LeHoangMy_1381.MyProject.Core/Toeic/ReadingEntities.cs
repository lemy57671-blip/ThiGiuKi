using Abp.Domain.Entities;
using System;

namespace LeHoangMy_1381.MyProject.Toeic;

// One row is one immutable published version; SourceId + Version identify its source.
public class Exam : Entity<int>, IMayHaveTenant
{
    public int? TenantId { get; set; }
    public string SourceId { get; set; }
    public string Version { get; set; }
    public string Title { get; set; }
    public string SourceDocument { get; set; }
    public string SourceSha256 { get; set; }
    public string AnswerProvenance { get; set; }
    public int DurationMinutes { get; set; } = 75;
    public int QuestionCount { get; set; } = 100;
    public string Status { get; set; } = "DRAFT";
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public byte[] RowVersion { get; set; }
}

public class ExamPart : Entity<int>
{
    public int ExamId { get; set; }
    public string SourceId { get; set; }
    public byte PartNumber { get; set; }
    public int DisplayOrder { get; set; }
    public int ExpectedQuestionCount { get; set; }
    public bool ShuffleQuestions { get; set; }
}

public class ReadingGroup : Entity<int>
{
    public int ExamPartId { get; set; }
    public string SourceId { get; set; }
    public int DisplayOrder { get; set; }
    public int FirstQuestionNumber { get; set; }
    public int LastQuestionNumber { get; set; }
    public bool ShuffleGroup { get; set; }
    public bool ShuffleQuestions { get; set; }
}

public class ReadingBlock : Entity<int>
{
    public int ReadingGroupId { get; set; }
    public string SourceId { get; set; }
    public int DisplayOrder { get; set; }
    public string BlockType { get; set; }
    public string TextContent { get; set; }
    public byte[] ImageData { get; set; }
    public string MimeType { get; set; }
    public string ImageSha256 { get; set; }
    public int? SourcePdfPage { get; set; }
}

public class Question : Entity<int>
{
    public int ExamId { get; set; }
    public int ExamPartId { get; set; }
    public int? ReadingGroupId { get; set; }
    public string SourceId { get; set; }
    public int OriginalNumber { get; set; }
    public string Stem { get; set; }
    public int OriginalOrder { get; set; }
    public bool ShuffleQuestion { get; set; }
    public bool ShuffleOptions { get; set; }
}

public class QuestionOption : Entity<int>
{
    public int QuestionId { get; set; }
    public string SourceId { get; set; }
    public string OriginalLabel { get; set; }
    public string Content { get; set; }
    public int OriginalOrder { get; set; }
}

// Keep this entity out of all student question DTOs, including its Hint.
public class AnswerKey
{
    public int QuestionId { get; set; }
    public int CorrectOptionId { get; set; }
    public string Hint { get; set; }
    public bool IsVerified { get; set; }
    public long? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public class GapReference
{
    public int QuestionId { get; set; }
    public int ReadingGroupId { get; set; }
    public int ReadingBlockId { get; set; }
    public int GapLabel { get; set; }
}

// Final counts are stored on the unique attempt row: no competing result table.
public class ExamAttempt : Entity<long>, IMayHaveTenant
{
    public int? TenantId { get; set; }
    public int ExamId { get; set; }
    public long StudentUserId { get; set; }
    public Guid ClientRequestId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public int TotalQuestions { get; set; } = 100;
    public int? CorrectCount { get; set; }
    public int? WrongCount { get; set; }
    public int? SkippedCount { get; set; }
    public decimal? CorrectPercentage { get; private set; }
    public byte[] RowVersion { get; set; }
}

public class AttemptQuestion
{
    public long ExamAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int ExamId { get; set; }
    public int DisplayOrder { get; set; }
}

public class AttemptOptionOrder
{
    public long ExamAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int QuestionOptionId { get; set; }
    public int DisplayOrder { get; set; }
}

public class AttemptAnswer
{
    public long ExamAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; }
}
