using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeHoangMy_1381.MyProject.Migrations
{
    /// <inheritdoc />
    public partial class AddToeicReadingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "toeic");

            migrationBuilder.CreateTable(
                name: "Exams",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    SourceId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SourceDocument = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AnswerProvenance = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValue: "DRAFT"),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.CheckConstraint("CK_Exams_Format", "[DurationMinutes]=75 AND [QuestionCount]=100");
                    table.CheckConstraint("CK_Exams_Publication", "([Status]=N'DRAFT' AND [PublishedAtUtc] IS NULL) OR ([Status]<>N'DRAFT' AND [PublishedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Exams_Status", "[Status] IN (N'DRAFT',N'PUBLISHED',N'ARCHIVED')");
                    table.ForeignKey(
                        name: "FK_Exams_AbpTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "AbpTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Exams_AbpUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamAttempts",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentUserId = table.Column<long>(type: "bigint", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, defaultValue: "IN_PROGRESS"),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: true),
                    WrongCount = table.Column<int>(type: "int", nullable: true),
                    SkippedCount = table.Column<int>(type: "int", nullable: true),
                    CorrectPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true, computedColumnSql: "CONVERT(decimal(5,2),100.0*[CorrectCount]/NULLIF([TotalQuestions],0))", stored: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttempts", x => x.Id);
                    table.UniqueConstraint("AK_ExamAttempts_Id_ExamId", x => new { x.Id, x.ExamId });
                    table.CheckConstraint("CK_ExamAttempts_Result", "([Status]=N'IN_PROGRESS' AND [SubmittedAtUtc] IS NULL AND [CorrectCount] IS NULL AND [WrongCount] IS NULL AND [SkippedCount] IS NULL) OR ([Status] IN (N'SUBMITTED',N'EXPIRED') AND [SubmittedAtUtc] IS NOT NULL AND [SubmittedAtUtc]>=[StartedAtUtc] AND [CorrectCount] IS NOT NULL AND [WrongCount] IS NOT NULL AND [SkippedCount] IS NOT NULL AND [CorrectCount]>=0 AND [WrongCount]>=0 AND [SkippedCount]>=0 AND [CorrectCount]+[WrongCount]+[SkippedCount]=[TotalQuestions])");
                    table.CheckConstraint("CK_ExamAttempts_Time", "[ExpiresAtUtc]=DATEADD(MINUTE,75,[StartedAtUtc]) AND [TotalQuestions]=100");
                    table.ForeignKey(
                        name: "FK_ExamAttempts_AbpTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "AbpTenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamAttempts_AbpUsers_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Exams_ExamId",
                        column: x => x.ExamId,
                        principalSchema: "toeic",
                        principalTable: "Exams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamParts",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PartNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ExpectedQuestionCount = table.Column<int>(type: "int", nullable: false),
                    ShuffleQuestions = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamParts", x => x.Id);
                    table.UniqueConstraint("AK_ExamParts_Id_ExamId", x => new { x.Id, x.ExamId });
                    table.CheckConstraint("CK_ExamParts_Format", "([PartNumber]=5 AND [DisplayOrder]=1 AND [ExpectedQuestionCount]=30) OR ([PartNumber]=6 AND [DisplayOrder]=2 AND [ExpectedQuestionCount]=16 AND [ShuffleQuestions]=0) OR ([PartNumber]=7 AND [DisplayOrder]=3 AND [ExpectedQuestionCount]=54 AND [ShuffleQuestions]=0)");
                    table.ForeignKey(
                        name: "FK_ExamParts_Exams_ExamId",
                        column: x => x.ExamId,
                        principalSchema: "toeic",
                        principalTable: "Exams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReadingGroups",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamPartId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    FirstQuestionNumber = table.Column<int>(type: "int", nullable: false),
                    LastQuestionNumber = table.Column<int>(type: "int", nullable: false),
                    ShuffleGroup = table.Column<bool>(type: "bit", nullable: false),
                    ShuffleQuestions = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingGroups", x => x.Id);
                    table.UniqueConstraint("AK_ReadingGroups_Id_ExamPartId", x => new { x.Id, x.ExamPartId });
                    table.CheckConstraint("CK_ReadingGroups_Range", "[FirstQuestionNumber]>=131 AND [LastQuestionNumber]<=200 AND [FirstQuestionNumber]<=[LastQuestionNumber] AND [DisplayOrder]>0 AND [ShuffleQuestions]=0");
                    table.ForeignKey(
                        name: "FK_ReadingGroups_ExamParts_ExamPartId",
                        column: x => x.ExamPartId,
                        principalSchema: "toeic",
                        principalTable: "ExamParts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ExamPartId = table.Column<int>(type: "int", nullable: false),
                    ReadingGroupId = table.Column<int>(type: "int", nullable: true),
                    SourceId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OriginalNumber = table.Column<int>(type: "int", nullable: false),
                    Stem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalOrder = table.Column<int>(type: "int", nullable: false),
                    ShuffleQuestion = table.Column<bool>(type: "bit", nullable: false),
                    ShuffleOptions = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.UniqueConstraint("AK_Questions_Id_ExamId", x => new { x.Id, x.ExamId });
                    table.CheckConstraint("CK_Questions_NumberOrder", "[OriginalNumber] BETWEEN 101 AND 200 AND [OriginalOrder] BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_Questions_ExamParts_ExamPartId_ExamId",
                        columns: x => new { x.ExamPartId, x.ExamId },
                        principalSchema: "toeic",
                        principalTable: "ExamParts",
                        principalColumns: new[] { "Id", "ExamId" });
                    table.ForeignKey(
                        name: "FK_Questions_ReadingGroups_ReadingGroupId_ExamPartId",
                        columns: x => new { x.ReadingGroupId, x.ExamPartId },
                        principalSchema: "toeic",
                        principalTable: "ReadingGroups",
                        principalColumns: new[] { "Id", "ExamPartId" });
                });

            migrationBuilder.CreateTable(
                name: "ReadingBlocks",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReadingGroupId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    BlockType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ImageSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SourcePdfPage = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingBlocks", x => x.Id);
                    table.UniqueConstraint("AK_ReadingBlocks_Id_ReadingGroupId", x => new { x.Id, x.ReadingGroupId });
                    table.CheckConstraint("CK_ReadingBlocks_Content", "([BlockType]=N'TEXT' AND [TextContent] IS NOT NULL AND LEN(LTRIM(RTRIM([TextContent])))>0 AND [ImageData] IS NULL AND [MimeType] IS NULL AND [ImageSha256] IS NULL) OR ([BlockType]=N'IMAGE' AND [TextContent] IS NULL AND DATALENGTH([ImageData])>0 AND [ImageData] IS NOT NULL AND [MimeType] IN (N'image/png',N'image/jpeg') AND [MimeType] IS NOT NULL AND LEN([ImageSha256])=64 AND [ImageSha256] IS NOT NULL)");
                    table.CheckConstraint("CK_ReadingBlocks_OrderPage", "[DisplayOrder]>0 AND ([SourcePdfPage] IS NULL OR [SourcePdfPage]>0)");
                    table.ForeignKey(
                        name: "FK_ReadingBlocks_ReadingGroups_ReadingGroupId",
                        column: x => x.ReadingGroupId,
                        principalSchema: "toeic",
                        principalTable: "ReadingGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AttemptQuestions",
                schema: "toeic",
                columns: table => new
                {
                    ExamAttemptId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptQuestions", x => new { x.ExamAttemptId, x.QuestionId });
                    table.CheckConstraint("CK_AttemptQuestions_Order", "[DisplayOrder] BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_AttemptQuestions_ExamAttempts_ExamAttemptId_ExamId",
                        columns: x => new { x.ExamAttemptId, x.ExamId },
                        principalSchema: "toeic",
                        principalTable: "ExamAttempts",
                        principalColumns: new[] { "Id", "ExamId" });
                    table.ForeignKey(
                        name: "FK_AttemptQuestions_Questions_QuestionId_ExamId",
                        columns: x => new { x.QuestionId, x.ExamId },
                        principalSchema: "toeic",
                        principalTable: "Questions",
                        principalColumns: new[] { "Id", "ExamId" });
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                schema: "toeic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    OriginalLabel = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.UniqueConstraint("AK_QuestionOptions_QuestionId_Id", x => new { x.QuestionId, x.Id });
                    table.CheckConstraint("CK_QuestionOptions_Content", "LEN(LTRIM(RTRIM([Content])))>0");
                    table.CheckConstraint("CK_QuestionOptions_Label", "([OriginalLabel]=N'A' AND [OriginalOrder]=1) OR ([OriginalLabel]=N'B' AND [OriginalOrder]=2) OR ([OriginalLabel]=N'C' AND [OriginalOrder]=3) OR ([OriginalLabel]=N'D' AND [OriginalOrder]=4)");
                    table.ForeignKey(
                        name: "FK_QuestionOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "toeic",
                        principalTable: "Questions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GapReferences",
                schema: "toeic",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    ReadingGroupId = table.Column<int>(type: "int", nullable: false),
                    ReadingBlockId = table.Column<int>(type: "int", nullable: false),
                    GapLabel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapReferences", x => x.QuestionId);
                    table.CheckConstraint("CK_GapReferences_Label", "[GapLabel] BETWEEN 131 AND 146");
                    table.ForeignKey(
                        name: "FK_GapReferences_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "toeic",
                        principalTable: "Questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GapReferences_ReadingBlocks_ReadingBlockId_ReadingGroupId",
                        columns: x => new { x.ReadingBlockId, x.ReadingGroupId },
                        principalSchema: "toeic",
                        principalTable: "ReadingBlocks",
                        principalColumns: new[] { "Id", "ReadingGroupId" });
                });

            migrationBuilder.CreateTable(
                name: "AnswerKeys",
                schema: "toeic",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    CorrectOptionId = table.Column<int>(type: "int", nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerKeys", x => x.QuestionId);
                    table.CheckConstraint("CK_AnswerKeys_Hint", "LEN(LTRIM(RTRIM([Hint])))>0");
                    table.CheckConstraint("CK_AnswerKeys_Verification", "([IsVerified]=0 AND [VerifiedByUserId] IS NULL AND [VerifiedAtUtc] IS NULL) OR ([IsVerified]=1 AND [VerifiedByUserId] IS NOT NULL AND [VerifiedAtUtc] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AnswerKeys_AbpUsers_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnswerKeys_QuestionOptions_QuestionId_CorrectOptionId",
                        columns: x => new { x.QuestionId, x.CorrectOptionId },
                        principalSchema: "toeic",
                        principalTable: "QuestionOptions",
                        principalColumns: new[] { "QuestionId", "Id" });
                    table.ForeignKey(
                        name: "FK_AnswerKeys_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "toeic",
                        principalTable: "Questions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AttemptOptionOrders",
                schema: "toeic",
                columns: table => new
                {
                    ExamAttemptId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionOptionId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptOptionOrders", x => new { x.ExamAttemptId, x.QuestionId, x.QuestionOptionId });
                    table.CheckConstraint("CK_AttemptOptionOrders_Order", "[DisplayOrder] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_AttemptOptionOrders_AttemptQuestions_ExamAttemptId_QuestionId",
                        columns: x => new { x.ExamAttemptId, x.QuestionId },
                        principalSchema: "toeic",
                        principalTable: "AttemptQuestions",
                        principalColumns: new[] { "ExamAttemptId", "QuestionId" });
                    table.ForeignKey(
                        name: "FK_AttemptOptionOrders_QuestionOptions_QuestionId_QuestionOptionId",
                        columns: x => new { x.QuestionId, x.QuestionOptionId },
                        principalSchema: "toeic",
                        principalTable: "QuestionOptions",
                        principalColumns: new[] { "QuestionId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "AttemptAnswers",
                schema: "toeic",
                columns: table => new
                {
                    ExamAttemptId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptAnswers", x => new { x.ExamAttemptId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_AttemptOptionOrders_ExamAttemptId_QuestionId_SelectedOptionId",
                        columns: x => new { x.ExamAttemptId, x.QuestionId, x.SelectedOptionId },
                        principalSchema: "toeic",
                        principalTable: "AttemptOptionOrders",
                        principalColumns: new[] { "ExamAttemptId", "QuestionId", "QuestionOptionId" });
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_AttemptQuestions_ExamAttemptId_QuestionId",
                        columns: x => new { x.ExamAttemptId, x.QuestionId },
                        principalSchema: "toeic",
                        principalTable: "AttemptQuestions",
                        principalColumns: new[] { "ExamAttemptId", "QuestionId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerKeys_QuestionId_CorrectOptionId",
                schema: "toeic",
                table: "AnswerKeys",
                columns: new[] { "QuestionId", "CorrectOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerKeys_VerifiedByUserId",
                schema: "toeic",
                table: "AnswerKeys",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_ExamAttemptId_QuestionId_SelectedOptionId",
                schema: "toeic",
                table: "AttemptAnswers",
                columns: new[] { "ExamAttemptId", "QuestionId", "SelectedOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptOptionOrders_ExamAttemptId_QuestionId_DisplayOrder",
                schema: "toeic",
                table: "AttemptOptionOrders",
                columns: new[] { "ExamAttemptId", "QuestionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptOptionOrders_QuestionId_QuestionOptionId",
                schema: "toeic",
                table: "AttemptOptionOrders",
                columns: new[] { "QuestionId", "QuestionOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestions_ExamAttemptId_DisplayOrder",
                schema: "toeic",
                table: "AttemptQuestions",
                columns: new[] { "ExamAttemptId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestions_ExamAttemptId_ExamId",
                schema: "toeic",
                table: "AttemptQuestions",
                columns: new[] { "ExamAttemptId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestions_QuestionId_ExamId",
                schema: "toeic",
                table: "AttemptQuestions",
                columns: new[] { "QuestionId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamId_Status",
                schema: "toeic",
                table: "ExamAttempts",
                columns: new[] { "ExamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_Status_ExpiresAtUtc",
                schema: "toeic",
                table: "ExamAttempts",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_StudentUserId_ClientRequestId",
                schema: "toeic",
                table: "ExamAttempts",
                columns: new[] { "StudentUserId", "ClientRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_TenantId",
                schema: "toeic",
                table: "ExamAttempts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamParts_ExamId_DisplayOrder",
                schema: "toeic",
                table: "ExamParts",
                columns: new[] { "ExamId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamParts_ExamId_PartNumber",
                schema: "toeic",
                table: "ExamParts",
                columns: new[] { "ExamId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamParts_ExamId_SourceId",
                schema: "toeic",
                table: "ExamParts",
                columns: new[] { "ExamId", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedByUserId",
                schema: "toeic",
                table: "Exams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TenantId_SourceId_Version",
                schema: "toeic",
                table: "Exams",
                columns: new[] { "TenantId", "SourceId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_TenantId_Status",
                schema: "toeic",
                table: "Exams",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GapReferences_ReadingBlockId_ReadingGroupId",
                schema: "toeic",
                table: "GapReferences",
                columns: new[] { "ReadingBlockId", "ReadingGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_GapReferences_ReadingGroupId_GapLabel",
                schema: "toeic",
                table: "GapReferences",
                columns: new[] { "ReadingGroupId", "GapLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId_OriginalLabel",
                schema: "toeic",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "OriginalLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId_SourceId",
                schema: "toeic",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId_OriginalNumber",
                schema: "toeic",
                table: "Questions",
                columns: new[] { "ExamId", "OriginalNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId_OriginalOrder",
                schema: "toeic",
                table: "Questions",
                columns: new[] { "ExamId", "OriginalOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId_SourceId",
                schema: "toeic",
                table: "Questions",
                columns: new[] { "ExamId", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamPartId_ExamId",
                schema: "toeic",
                table: "Questions",
                columns: new[] { "ExamPartId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ReadingGroupId_ExamPartId",
                schema: "toeic",
                table: "Questions",
                columns: new[] { "ReadingGroupId", "ExamPartId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingBlocks_ReadingGroupId_DisplayOrder",
                schema: "toeic",
                table: "ReadingBlocks",
                columns: new[] { "ReadingGroupId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingBlocks_ReadingGroupId_SourceId",
                schema: "toeic",
                table: "ReadingBlocks",
                columns: new[] { "ReadingGroupId", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingGroups_ExamPartId_DisplayOrder",
                schema: "toeic",
                table: "ReadingGroups",
                columns: new[] { "ExamPartId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingGroups_ExamPartId_SourceId",
                schema: "toeic",
                table: "ReadingGroups",
                columns: new[] { "ExamPartId", "SourceId" },
                unique: true);
            EntityFrameworkCore.Toeic.ReadingMigrationSql.ApplyV1(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerKeys",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "AttemptAnswers",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "GapReferences",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "AttemptOptionOrders",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "ReadingBlocks",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "AttemptQuestions",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "QuestionOptions",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "ExamAttempts",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "Questions",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "ReadingGroups",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "ExamParts",
                schema: "toeic");

            migrationBuilder.DropTable(
                name: "Exams",
                schema: "toeic");
        }
    }
}
