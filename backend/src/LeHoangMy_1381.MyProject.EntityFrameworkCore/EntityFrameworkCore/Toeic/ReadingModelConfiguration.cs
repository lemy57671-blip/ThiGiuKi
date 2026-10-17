using LeHoangMy_1381.MyProject.Authorization.Users;
using LeHoangMy_1381.MyProject.MultiTenancy;
using LeHoangMy_1381.MyProject.Toeic;
using Microsoft.EntityFrameworkCore;

namespace LeHoangMy_1381.MyProject.EntityFrameworkCore.Toeic;

public static class ReadingModelConfiguration
{
    public static void ConfigureReading(this ModelBuilder model)
    {
        model.Entity<Exam>(b =>
        {
            b.ToTable("Exams", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_Exams_Status", "[Status] IN (N'DRAFT',N'PUBLISHED',N'ARCHIVED')");
                t.HasCheckConstraint("CK_Exams_Format", "[DurationMinutes]=75 AND [QuestionCount]=100");
                t.HasCheckConstraint("CK_Exams_Publication", "([Status]=N'DRAFT' AND [PublishedAtUtc] IS NULL) OR ([Status]<>N'DRAFT' AND [PublishedAtUtc] IS NOT NULL)");
            });
            b.Property(x => x.SourceId).HasMaxLength(40).IsRequired();
            b.Property(x => x.Version).HasMaxLength(30).IsRequired();
            b.Property(x => x.Title).HasMaxLength(250).IsRequired();
            b.Property(x => x.SourceDocument).HasMaxLength(260).IsRequired();
            b.Property(x => x.SourceSha256).HasMaxLength(64).IsRequired();
            b.Property(x => x.AnswerProvenance).HasMaxLength(500).IsRequired();
            b.Property(x => x.Status).HasMaxLength(12).HasDefaultValue("DRAFT").IsRequired();
            b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.TenantId, x.SourceId, x.Version }).IsUnique().HasFilter(null);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<ExamPart>(b =>
        {
            b.ToTable("ExamParts", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_ExamParts_Format", "([PartNumber]=5 AND [DisplayOrder]=1 AND [ExpectedQuestionCount]=30) OR ([PartNumber]=6 AND [DisplayOrder]=2 AND [ExpectedQuestionCount]=16 AND [ShuffleQuestions]=0) OR ([PartNumber]=7 AND [DisplayOrder]=3 AND [ExpectedQuestionCount]=54 AND [ShuffleQuestions]=0)");
            });
            b.Property(x => x.SourceId).HasMaxLength(40).IsRequired();
            b.HasAlternateKey(x => new { x.Id, x.ExamId });
            b.HasIndex(x => new { x.ExamId, x.PartNumber }).IsUnique();
            b.HasIndex(x => new { x.ExamId, x.SourceId }).IsUnique();
            b.HasIndex(x => new { x.ExamId, x.DisplayOrder }).IsUnique();
            b.HasOne<Exam>().WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<ReadingGroup>(b =>
        {
            b.ToTable("ReadingGroups", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_ReadingGroups_Range", "[FirstQuestionNumber]>=131 AND [LastQuestionNumber]<=200 AND [FirstQuestionNumber]<=[LastQuestionNumber] AND [DisplayOrder]>0 AND [ShuffleQuestions]=0");
            });
            b.Property(x => x.SourceId).HasMaxLength(40).IsRequired();
            b.HasAlternateKey(x => new { x.Id, x.ExamPartId });
            b.HasIndex(x => new { x.ExamPartId, x.SourceId }).IsUnique();
            b.HasIndex(x => new { x.ExamPartId, x.DisplayOrder }).IsUnique();
            b.HasOne<ExamPart>().WithMany().HasForeignKey(x => x.ExamPartId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<ReadingBlock>(b =>
        {
            b.ToTable("ReadingBlocks", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_ReadingBlocks_Content", "([BlockType]=N'TEXT' AND [TextContent] IS NOT NULL AND LEN(LTRIM(RTRIM([TextContent])))>0 AND [ImageData] IS NULL AND [MimeType] IS NULL AND [ImageSha256] IS NULL) OR ([BlockType]=N'IMAGE' AND [TextContent] IS NULL AND DATALENGTH([ImageData])>0 AND [ImageData] IS NOT NULL AND [MimeType] IN (N'image/png',N'image/jpeg') AND [MimeType] IS NOT NULL AND LEN([ImageSha256])=64 AND [ImageSha256] IS NOT NULL)");
                t.HasCheckConstraint("CK_ReadingBlocks_OrderPage", "[DisplayOrder]>0 AND ([SourcePdfPage] IS NULL OR [SourcePdfPage]>0)");
            });
            b.Property(x => x.SourceId).HasMaxLength(40).IsRequired();
            b.Property(x => x.BlockType).HasMaxLength(5).IsRequired();
            b.Property(x => x.MimeType).HasMaxLength(40);
            b.Property(x => x.ImageSha256).HasMaxLength(64);
            b.HasAlternateKey(x => new { x.Id, x.ReadingGroupId });
            b.HasIndex(x => new { x.ReadingGroupId, x.SourceId }).IsUnique();
            b.HasIndex(x => new { x.ReadingGroupId, x.DisplayOrder }).IsUnique();
            b.HasOne<ReadingGroup>().WithMany().HasForeignKey(x => x.ReadingGroupId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<Question>(b =>
        {
            b.ToTable("Questions", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_Questions_NumberOrder", "[OriginalNumber] BETWEEN 101 AND 200 AND [OriginalOrder] BETWEEN 1 AND 100");
            });
            b.Property(x => x.SourceId).HasMaxLength(40).IsRequired();
            b.Property(x => x.Stem).IsRequired();
            b.HasAlternateKey(x => new { x.Id, x.ExamId });
            b.HasIndex(x => new { x.ExamId, x.OriginalNumber }).IsUnique();
            b.HasIndex(x => new { x.ExamId, x.SourceId }).IsUnique();
            b.HasIndex(x => new { x.ExamId, x.OriginalOrder }).IsUnique();
            b.HasOne<ExamPart>().WithMany().HasForeignKey(x => new { x.ExamPartId, x.ExamId }).HasPrincipalKey(x => new { x.Id, x.ExamId }).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<ReadingGroup>().WithMany().HasForeignKey(x => new { x.ReadingGroupId, x.ExamPartId }).HasPrincipalKey(x => new { x.Id, x.ExamPartId }).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<QuestionOption>(b =>
        {
            b.ToTable("QuestionOptions", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_QuestionOptions_Label", "([OriginalLabel]=N'A' AND [OriginalOrder]=1) OR ([OriginalLabel]=N'B' AND [OriginalOrder]=2) OR ([OriginalLabel]=N'C' AND [OriginalOrder]=3) OR ([OriginalLabel]=N'D' AND [OriginalOrder]=4)");
                t.HasCheckConstraint("CK_QuestionOptions_Content", "LEN(LTRIM(RTRIM([Content])))>0");
            });
            b.Property(x => x.SourceId).HasMaxLength(60).IsRequired();
            b.Property(x => x.OriginalLabel).HasMaxLength(1).IsRequired();
            b.Property(x => x.Content).IsRequired();
            b.HasAlternateKey(x => new { x.QuestionId, x.Id });
            b.HasIndex(x => new { x.QuestionId, x.OriginalLabel }).IsUnique();
            b.HasIndex(x => new { x.QuestionId, x.SourceId }).IsUnique();
            b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<AnswerKey>(b =>
        {
            b.ToTable("AnswerKeys", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_AnswerKeys_Hint", "LEN(LTRIM(RTRIM([Hint])))>0");
                t.HasCheckConstraint("CK_AnswerKeys_Verification", "([IsVerified]=0 AND [VerifiedByUserId] IS NULL AND [VerifiedAtUtc] IS NULL) OR ([IsVerified]=1 AND [VerifiedByUserId] IS NOT NULL AND [VerifiedAtUtc] IS NOT NULL)");
            });
            b.HasKey(x => x.QuestionId);
            b.Property(x => x.Hint).IsRequired();
            b.HasOne<Question>().WithOne().HasForeignKey<AnswerKey>(x => x.QuestionId).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<QuestionOption>().WithMany().HasForeignKey(x => new { x.QuestionId, x.CorrectOptionId }).HasPrincipalKey(x => new { x.QuestionId, x.Id }).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<GapReference>(b =>
        {
            b.ToTable("GapReferences", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_GapReferences_Label", "[GapLabel] BETWEEN 131 AND 146");
            });
            b.HasKey(x => x.QuestionId);
            b.HasIndex(x => new { x.ReadingGroupId, x.GapLabel }).IsUnique();
            // Question.GroupId is nullable for Part 5: use a trigger for group equality
            // instead of making that nullable property part of an EF alternate key.
            b.HasOne<Question>().WithOne().HasForeignKey<GapReference>(x => x.QuestionId).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<ReadingBlock>().WithMany().HasForeignKey(x => new { x.ReadingBlockId, x.ReadingGroupId }).HasPrincipalKey(x => new { x.Id, x.ReadingGroupId }).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<ExamAttempt>(b =>
        {
            b.ToTable("ExamAttempts", "toeic", t => {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_ExamAttempts_Time", "[ExpiresAtUtc]=DATEADD(MINUTE,75,[StartedAtUtc]) AND [TotalQuestions]=100");
                t.HasCheckConstraint("CK_ExamAttempts_Result", "([Status]=N'IN_PROGRESS' AND [SubmittedAtUtc] IS NULL AND [CorrectCount] IS NULL AND [WrongCount] IS NULL AND [SkippedCount] IS NULL) OR ([Status] IN (N'SUBMITTED',N'EXPIRED') AND [SubmittedAtUtc] IS NOT NULL AND [SubmittedAtUtc]>=[StartedAtUtc] AND [CorrectCount] IS NOT NULL AND [WrongCount] IS NOT NULL AND [SkippedCount] IS NOT NULL AND [CorrectCount]>=0 AND [WrongCount]>=0 AND [SkippedCount]>=0 AND [CorrectCount]+[WrongCount]+[SkippedCount]=[TotalQuestions])");
            });
            b.Property(x => x.Status).HasMaxLength(12).HasDefaultValue("IN_PROGRESS").IsRequired();
            b.Property(x => x.ClientRequestId).HasDefaultValueSql("NEWID()");
            b.Property(x => x.CorrectPercentage).HasComputedColumnSql("CONVERT(decimal(5,2),100.0*[CorrectCount]/NULLIF([TotalQuestions],0))", stored: true);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasAlternateKey(x => new { x.Id, x.ExamId });
            b.HasIndex(x => new { x.StudentUserId, x.ClientRequestId }).IsUnique();
            b.HasIndex(x => new { x.ExamId, x.Status });
            b.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
            b.HasOne<Exam>().WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<User>().WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<AttemptQuestion>(b =>
        {
            b.ToTable("AttemptQuestions", "toeic", t => { t.UseSqlOutputClause(false); t.HasCheckConstraint("CK_AttemptQuestions_Order", "[DisplayOrder] BETWEEN 1 AND 100"); });
            b.HasKey(x => new { x.ExamAttemptId, x.QuestionId });
            b.HasIndex(x => new { x.ExamAttemptId, x.DisplayOrder }).IsUnique();
            b.HasOne<ExamAttempt>().WithMany().HasForeignKey(x => new { x.ExamAttemptId, x.ExamId }).HasPrincipalKey(x => new { x.Id, x.ExamId }).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<Question>().WithMany().HasForeignKey(x => new { x.QuestionId, x.ExamId }).HasPrincipalKey(x => new { x.Id, x.ExamId }).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<AttemptOptionOrder>(b =>
        {
            b.ToTable("AttemptOptionOrders", "toeic", t => { t.UseSqlOutputClause(false); t.HasCheckConstraint("CK_AttemptOptionOrders_Order", "[DisplayOrder] BETWEEN 1 AND 4"); });
            b.HasKey(x => new { x.ExamAttemptId, x.QuestionId, x.QuestionOptionId });
            b.HasIndex(x => new { x.ExamAttemptId, x.QuestionId, x.DisplayOrder }).IsUnique();
            b.HasOne<AttemptQuestion>().WithMany().HasForeignKey(x => new { x.ExamAttemptId, x.QuestionId }).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<QuestionOption>().WithMany().HasForeignKey(x => new { x.QuestionId, x.QuestionOptionId }).HasPrincipalKey(x => new { x.QuestionId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        });
        model.Entity<AttemptAnswer>(b =>
        {
            b.ToTable("AttemptAnswers", "toeic", t => t.UseSqlOutputClause(false));
            b.HasKey(x => new { x.ExamAttemptId, x.QuestionId });
            b.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasOne<AttemptQuestion>().WithOne().HasForeignKey<AttemptAnswer>(x => new { x.ExamAttemptId, x.QuestionId }).OnDelete(DeleteBehavior.NoAction);
            b.HasOne<AttemptOptionOrder>().WithMany().HasForeignKey(x => new { x.ExamAttemptId, x.QuestionId, x.SelectedOptionId }).HasPrincipalKey(x => new { x.ExamAttemptId, x.QuestionId, x.QuestionOptionId }).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
