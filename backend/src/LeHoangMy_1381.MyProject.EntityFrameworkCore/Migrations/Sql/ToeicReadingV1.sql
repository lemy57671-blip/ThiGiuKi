-- Versioned migration SQL. Source for the exported 02 script. Do not edit after deployment.
IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET ARITHABORT ON; SET NUMERIC_ROUNDABORT OFF;
GO
IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.ValidateExam
    @ExamId int, @RequireVerified bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM toeic.Exams WHERE Id=@ExamId AND QuestionCount=100 AND DurationMinutes=75)
        THROW 51001, ''Invalid TOEIC exam metadata.'', 1;
    IF (SELECT COUNT(*) FROM toeic.ExamParts WHERE ExamId=@ExamId)<>3
        THROW 51002, ''An exam requires exactly three parts.'', 1;
    IF (SELECT COUNT(*) FROM toeic.Questions WHERE ExamId=@ExamId)<>100
        OR EXISTS (SELECT 1 FROM toeic.ExamParts p WHERE p.ExamId=@ExamId
          AND (SELECT COUNT(*) FROM toeic.Questions q WHERE q.ExamPartId=p.Id)<>p.ExpectedQuestionCount)
        THROW 51003, ''Expected 100 questions split into 30, 16 and 54.'', 1;
    IF EXISTS (SELECT 1 FROM toeic.Questions q JOIN toeic.ExamParts p ON p.Id=q.ExamPartId WHERE q.ExamId=@ExamId AND
        ((p.PartNumber=5 AND (q.OriginalNumber NOT BETWEEN 101 AND 130 OR q.ReadingGroupId IS NOT NULL OR LEN(LTRIM(RTRIM(q.Stem)))=0))
         OR (p.PartNumber=6 AND (q.OriginalNumber NOT BETWEEN 131 AND 146 OR q.ReadingGroupId IS NULL OR q.ShuffleQuestion=1))
         OR (p.PartNumber=7 AND (q.OriginalNumber NOT BETWEEN 147 AND 200 OR q.ReadingGroupId IS NULL OR q.ShuffleQuestion=1 OR LEN(LTRIM(RTRIM(q.Stem)))=0))))
        THROW 51004, ''Question range, stem or grouping is invalid.'', 1;
    IF EXISTS (SELECT 1 FROM toeic.Questions q WHERE q.ExamId=@ExamId AND
        ((SELECT COUNT(*) FROM toeic.QuestionOptions o WHERE o.QuestionId=q.Id)<>4
        OR NOT EXISTS (SELECT 1 FROM toeic.AnswerKeys k WHERE k.QuestionId=q.Id)))
        THROW 51005, ''Every question requires four options, one key and a hint.'', 1;
    IF @RequireVerified=1 AND EXISTS (SELECT 1 FROM toeic.AnswerKeys k JOIN toeic.Questions q ON q.Id=k.QuestionId WHERE q.ExamId=@ExamId AND k.IsVerified=0)
        THROW 51006, ''Answer keys are not yet verified by a reviewer.'', 1;
    IF (SELECT COUNT(*) FROM toeic.ReadingGroups g JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@ExamId)<>19
        THROW 51007, ''Expected nineteen reading groups.'', 1;
    IF EXISTS (
        SELECT expected.PartNumber, expected.FirstNumber, expected.LastNumber FROM
        (VALUES (6,131,134),(6,135,138),(6,139,142),(6,143,146),
          (7,147,148),(7,149,150),(7,151,152),(7,153,154),(7,155,157),(7,158,160),(7,161,163),
          (7,164,167),(7,168,171),(7,172,175),(7,176,180),(7,181,185),(7,186,190),(7,191,195),(7,196,200)) expected(PartNumber,FirstNumber,LastNumber)
        EXCEPT SELECT p.PartNumber,g.FirstQuestionNumber,g.LastQuestionNumber
        FROM toeic.ReadingGroups g JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@ExamId)
        THROW 51008, ''Reading group boundaries do not match the Reading test.'', 1;
    IF EXISTS (SELECT 1 FROM toeic.ReadingGroups g JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@ExamId
       AND ((SELECT COUNT(*) FROM toeic.Questions q WHERE q.ReadingGroupId=g.Id)<>g.LastQuestionNumber-g.FirstQuestionNumber+1
         OR NOT EXISTS (SELECT 1 FROM toeic.ReadingBlocks b WHERE b.ReadingGroupId=g.Id)))
        THROW 51009, ''A reading group has missing questions or content blocks.'', 1;
    IF EXISTS (SELECT 1 FROM toeic.Questions q JOIN toeic.ExamParts p ON p.Id=q.ExamPartId
       WHERE q.ExamId=@ExamId AND p.PartNumber=6 AND NOT EXISTS (SELECT 1 FROM toeic.GapReferences r WHERE r.QuestionId=q.Id))
       OR (SELECT COUNT(*) FROM toeic.GapReferences r JOIN toeic.Questions q ON q.Id=r.QuestionId WHERE q.ExamId=@ExamId)<>16
        THROW 51010, ''Part 6 requires sixteen valid gap references.'', 1;
    IF EXISTS (SELECT 1 FROM toeic.Exams WHERE Id=@ExamId AND SourceId=N''RC06'')
       AND (SELECT COUNT(*) FROM toeic.ReadingBlocks b JOIN toeic.ReadingGroups g ON g.Id=b.ReadingGroupId
           JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@ExamId AND b.BlockType=N''IMAGE'')<>27
        THROW 51011, ''Reading v06 requires all twenty-seven embedded images.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_Exams_Lifecycle ON toeic.Exams
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id
       WHERE (d.Status<>N''DRAFT'' OR d.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=d.Id))
       AND (i.Id IS NULL OR EXISTS (
          SELECT d.TenantId,d.SourceId,d.Version,d.Title,d.SourceDocument,d.SourceSha256,d.AnswerProvenance,d.DurationMinutes,d.QuestionCount,d.CreatedByUserId,d.CreatedAtUtc,d.PublishedAtUtc
          EXCEPT SELECT i.TenantId,i.SourceId,i.Version,i.Title,i.SourceDocument,i.SourceSha256,i.AnswerProvenance,i.DurationMinutes,i.QuestionCount,i.CreatedByUserId,i.CreatedAtUtc,i.PublishedAtUtc)))
        THROW 51101, ''Published content is immutable; create a new exam version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.Status<>d.Status
       AND NOT ((d.Status=N''DRAFT'' AND i.Status=N''PUBLISHED'') OR (d.Status=N''PUBLISHED'' AND i.Status=N''ARCHIVED'')))
       OR EXISTS (SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id WHERE d.Id IS NULL AND i.Status<>N''DRAFT'')
        THROW 51102, ''Invalid exam lifecycle transition.'', 1;
    DECLARE @ExamId int;
    DECLARE to_validate CURSOR LOCAL FAST_FORWARD FOR
       SELECT i.Id FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.Status=N''PUBLISHED'' AND d.Status=N''DRAFT'';
    OPEN to_validate;
    FETCH NEXT FROM to_validate INTO @ExamId;
    WHILE @@FETCH_STATUS=0
    BEGIN
        EXEC toeic.ValidateExam @ExamId,1;
        FETCH NEXT FROM to_validate INTO @ExamId;
    END
    CLOSE to_validate;
    DEALLOCATE to_validate;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_ExamParts_Content ON toeic.ExamParts
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT x.ExamId FROM inserted x UNION SELECT x.ExamId FROM deleted x) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ExamId<>d.ExamId OR i.PartNumber<>d.PartNumber OR i.SourceId<>d.SourceId)
        THROW 51110, ''Part identity cannot be changed.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_ReadingGroups_Content ON toeic.ReadingGroups
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT p.ExamId FROM inserted x JOIN toeic.ExamParts p ON p.Id=x.ExamPartId UNION SELECT p.ExamId FROM deleted x JOIN toeic.ExamParts p ON p.Id=x.ExamPartId) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN toeic.ExamParts p ON p.Id=i.ExamPartId WHERE p.PartNumber=5 OR (p.PartNumber=6 AND (i.FirstQuestionNumber<131 OR i.LastQuestionNumber>146)) OR (p.PartNumber=7 AND i.FirstQuestionNumber<147))
        THROW 51111, ''Reading group belongs to the wrong part.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ExamPartId<>d.ExamPartId OR i.SourceId<>d.SourceId OR i.FirstQuestionNumber<>d.FirstQuestionNumber OR i.LastQuestionNumber<>d.LastQuestionNumber)
        THROW 51112, ''Reading group identity cannot be changed.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_ReadingBlocks_Content ON toeic.ReadingBlocks
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT p.ExamId FROM inserted x JOIN toeic.ReadingGroups g ON g.Id=x.ReadingGroupId JOIN toeic.ExamParts p ON p.Id=g.ExamPartId UNION SELECT p.ExamId FROM deleted x JOIN toeic.ReadingGroups g ON g.Id=x.ReadingGroupId JOIN toeic.ExamParts p ON p.Id=g.ExamPartId) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;

END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_Questions_Content ON toeic.Questions
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT x.ExamId FROM inserted x UNION SELECT x.ExamId FROM deleted x) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN toeic.ExamParts p ON p.Id=i.ExamPartId LEFT JOIN toeic.ReadingGroups g ON g.Id=i.ReadingGroupId
      WHERE (p.PartNumber=5 AND (i.ReadingGroupId IS NOT NULL OR i.OriginalNumber NOT BETWEEN 101 AND 130 OR LEN(LTRIM(RTRIM(i.Stem)))=0))
         OR (p.PartNumber=6 AND (i.ReadingGroupId IS NULL OR i.OriginalNumber NOT BETWEEN 131 AND 146 OR i.ShuffleQuestion=1))
         OR (p.PartNumber=7 AND (i.ReadingGroupId IS NULL OR i.OriginalNumber NOT BETWEEN 147 AND 200 OR i.ShuffleQuestion=1 OR LEN(LTRIM(RTRIM(i.Stem)))=0))
         OR (i.ReadingGroupId IS NOT NULL AND i.OriginalNumber NOT BETWEEN g.FirstQuestionNumber AND g.LastQuestionNumber))
        THROW 51113, ''Question range, group, stem or shuffle flag is invalid.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.ExamId<>d.ExamId OR i.ExamPartId<>d.ExamPartId OR ISNULL(i.ReadingGroupId,0)<>ISNULL(d.ReadingGroupId,0) OR i.SourceId<>d.SourceId OR i.OriginalNumber<>d.OriginalNumber)
        THROW 51114, ''Question identity cannot be changed.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_QuestionOptions_Content ON toeic.QuestionOptions
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT q.ExamId FROM inserted x JOIN toeic.Questions q ON q.Id=x.QuestionId UNION SELECT q.ExamId FROM deleted x JOIN toeic.Questions q ON q.Id=x.QuestionId) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN toeic.Questions q ON q.Id=i.QuestionId WHERE i.SourceId<>CONCAT(q.SourceId,N''-'',i.OriginalLabel))
        THROW 51115, ''Option source ID must match its question and original label.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.QuestionId<>d.QuestionId OR i.SourceId<>d.SourceId OR i.OriginalLabel<>d.OriginalLabel)
        THROW 51116, ''Option identity cannot be changed.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_AnswerKeys_Content ON toeic.AnswerKeys
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT q.ExamId FROM inserted x JOIN toeic.Questions q ON q.Id=x.QuestionId UNION SELECT q.ExamId FROM deleted x JOIN toeic.Questions q ON q.Id=x.QuestionId) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;

END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_GapReferences_Content ON toeic.GapReferences
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.Exams e WITH (UPDLOCK,HOLDLOCK)
       JOIN (SELECT q.ExamId FROM inserted x JOIN toeic.Questions q ON q.Id=x.QuestionId UNION SELECT q.ExamId FROM deleted x JOIN toeic.Questions q ON q.Id=x.QuestionId) affected ON affected.ExamId=e.Id
       WHERE e.Status<>N''DRAFT'' OR e.PublishedAtUtc IS NOT NULL OR EXISTS (SELECT 1 FROM toeic.ExamAttempts a WHERE a.ExamId=e.Id))
        THROW 51100, ''Published content and answer keys are immutable; create a new version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN toeic.Questions q ON q.Id=i.QuestionId JOIN toeic.ExamParts p ON p.Id=q.ExamPartId
       WHERE p.PartNumber<>6 OR q.ReadingGroupId IS NULL OR q.ReadingGroupId<>i.ReadingGroupId OR q.OriginalNumber<>i.GapLabel)
        THROW 51117, ''Gap reference must point to its Part 6 question and group.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_ExamAttempts_State ON toeic.ExamAttempts
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
        THROW 51201, ''Attempt history cannot be deleted.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE d.Status<>N''IN_PROGRESS''
       OR EXISTS (SELECT d.TenantId,d.ExamId,d.StudentUserId,d.ClientRequestId,d.StartedAtUtc,d.ExpiresAtUtc,d.TotalQuestions
                  EXCEPT SELECT i.TenantId,i.ExamId,i.StudentUserId,i.ClientRequestId,i.StartedAtUtc,i.ExpiresAtUtc,i.TotalQuestions))
        THROW 51202, ''A final result and attempt identity/deadline cannot be overwritten.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id JOIN toeic.Exams e ON e.Id=i.ExamId JOIN dbo.AbpUsers u ON u.Id=i.StudentUserId
       WHERE d.Id IS NULL AND (i.Status<>N''IN_PROGRESS'' OR e.Status<>N''PUBLISHED'' OR u.IsDeleted=1 OR u.IsActive=0
            OR ISNULL(i.TenantId,0)<>ISNULL(e.TenantId,0) OR ISNULL(i.TenantId,0)<>ISNULL(u.TenantId,0)))
        THROW 51203, ''Only active users in the exam tenant may start a published exam.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.Status<>N''IN_PROGRESS'' AND
       ((SELECT COUNT(*) FROM toeic.AttemptQuestions aq WHERE aq.ExamAttemptId=i.Id)<>i.TotalQuestions
        OR (SELECT COUNT(*) FROM toeic.AttemptOptionOrders ao WHERE ao.ExamAttemptId=i.Id)<>i.TotalQuestions*4
        OR i.CorrectCount<>(SELECT COUNT(*) FROM toeic.AttemptQuestions aq JOIN toeic.AttemptAnswers aa ON aa.ExamAttemptId=aq.ExamAttemptId AND aa.QuestionId=aq.QuestionId
             JOIN toeic.AnswerKeys k ON k.QuestionId=aq.QuestionId WHERE aq.ExamAttemptId=i.Id AND aa.SelectedOptionId=k.CorrectOptionId)
        OR i.WrongCount<>(SELECT COUNT(*) FROM toeic.AttemptQuestions aq JOIN toeic.AttemptAnswers aa ON aa.ExamAttemptId=aq.ExamAttemptId AND aa.QuestionId=aq.QuestionId
             JOIN toeic.AnswerKeys k ON k.QuestionId=aq.QuestionId WHERE aq.ExamAttemptId=i.Id AND aa.SelectedOptionId IS NOT NULL AND aa.SelectedOptionId<>k.CorrectOptionId)
        OR (i.Status=N''EXPIRED'' AND i.SubmittedAtUtc<i.ExpiresAtUtc)
        OR (i.Status=N''SUBMITTED'' AND i.SubmittedAtUtc>=i.ExpiresAtUtc)))
        THROW 51204, ''Final result does not match the stored answers, snapshot or deadline.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_AttemptQuestions_Immutable ON toeic.AttemptQuestions
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51210, ''Attempt question/option order is immutable.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN toeic.ExamAttempts a WITH(UPDLOCK,HOLDLOCK) ON a.Id=i.ExamAttemptId WHERE a.Status<>N''IN_PROGRESS'')
        THROW 51211, ''Cannot add content to a finalized attempt.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_AttemptOptionOrders_Immutable ON toeic.AttemptOptionOrders
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51210, ''Attempt question/option order is immutable.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN toeic.ExamAttempts a WITH(UPDLOCK,HOLDLOCK) ON a.Id=i.ExamAttemptId WHERE a.Status<>N''IN_PROGRESS'')
        THROW 51211, ''Cannot add content to a finalized attempt.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER TRIGGER toeic.TR_AttemptAnswers_Open ON toeic.AttemptAnswers
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM toeic.ExamAttempts a WITH(UPDLOCK,HOLDLOCK)
       JOIN (SELECT ExamAttemptId FROM inserted UNION SELECT ExamAttemptId FROM deleted) affected ON affected.ExamAttemptId=a.Id
       WHERE a.Status<>N''IN_PROGRESS'' OR a.ExpiresAtUtc<=SYSUTCDATETIME())
        THROW 51220, ''Answers are locked after submission or expiry.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.ExamAttemptId=i.ExamAttemptId AND d.QuestionId=i.QuestionId WHERE i.UpdatedAtUtc<d.UpdatedAtUtc)
        THROW 51221, ''Answer update time cannot move backwards.'', 1;
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.PublishExam @ExamId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM toeic.Exams WITH(UPDLOCK,HOLDLOCK) WHERE Id=@ExamId AND Status=N''DRAFT'')
            THROW 51301, ''Only a draft exam can be published.'', 1;
        EXEC toeic.ValidateExam @ExamId,1;
        UPDATE toeic.Exams SET Status=N''PUBLISHED'',PublishedAtUtc=SYSUTCDATETIME() WHERE Id=@ExamId;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.StartAttempt
    @ExamId int, @StudentUserId bigint, @ClientRequestId uniqueidentifier, @ExamAttemptId bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @TenantId int, @Status nvarchar(12), @Now datetime2=SYSUTCDATETIME();
        IF NOT EXISTS (SELECT 1 FROM dbo.AbpUsers WITH(UPDLOCK,HOLDLOCK) WHERE Id=@StudentUserId AND IsActive=1 AND IsDeleted=0)
            THROW 51310, ''Active ABP student user not found.'', 1;
        SELECT @ExamAttemptId=Id FROM toeic.ExamAttempts WHERE StudentUserId=@StudentUserId AND ClientRequestId=@ClientRequestId;
        IF @ExamAttemptId IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM toeic.ExamAttempts WHERE Id=@ExamAttemptId AND ExamId=@ExamId)
                THROW 51311, ''Start request ID was already used for another exam.'', 1;
            COMMIT;
            SELECT Id,StartedAtUtc,ExpiresAtUtc,Status FROM toeic.ExamAttempts WHERE Id=@ExamAttemptId;
            RETURN;
        END
        SELECT @TenantId=TenantId,@Status=Status FROM toeic.Exams WITH(UPDLOCK,HOLDLOCK) WHERE Id=@ExamId;
        IF @Status IS NULL OR @Status<>N''PUBLISHED'' THROW 51312, ''Exam is not published.'', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.AbpUsers WHERE Id=@StudentUserId AND ISNULL(TenantId,0)=ISNULL(@TenantId,0))
            THROW 51313, ''User and exam must belong to the same tenant.'', 1;
        EXEC toeic.ValidateExam @ExamId,1;
        INSERT toeic.ExamAttempts(TenantId,ExamId,StudentUserId,ClientRequestId,StartedAtUtc,ExpiresAtUtc,Status,TotalQuestions)
            VALUES(@TenantId,@ExamId,@StudentUserId,@ClientRequestId,@Now,DATEADD(MINUTE,75,@Now),N''IN_PROGRESS'',100);
        SET @ExamAttemptId=CONVERT(bigint,SCOPE_IDENTITY());
        -- Shuffle only eligible slots; fixed questions/groups keep their original slots.
        ;WITH eligible AS (
            SELECT q.Id,q.OriginalOrder,p.Id PartId,ROW_NUMBER() OVER(PARTITION BY p.Id ORDER BY q.OriginalOrder) SlotNo
            FROM toeic.Questions q JOIN toeic.ExamParts p ON p.Id=q.ExamPartId
            WHERE q.ExamId=@ExamId AND p.PartNumber=5 AND p.ShuffleQuestions=1 AND q.ShuffleQuestion=1),
        random_questions AS (SELECT Id,PartId,ROW_NUMBER() OVER(PARTITION BY PartId ORDER BY NEWID()) SlotNo FROM eligible),
        question_slots AS (SELECT r.Id,s.OriginalOrder FROM random_questions r JOIN eligible s ON s.PartId=r.PartId AND s.SlotNo=r.SlotNo),
        eligible_groups AS (
            SELECT g.Id,g.ExamPartId,g.DisplayOrder,ROW_NUMBER() OVER(PARTITION BY g.ExamPartId ORDER BY g.DisplayOrder) SlotNo
            FROM toeic.ReadingGroups g JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@ExamId AND g.ShuffleGroup=1),
        random_groups AS (SELECT Id,ExamPartId,ROW_NUMBER() OVER(PARTITION BY ExamPartId ORDER BY NEWID()) SlotNo FROM eligible_groups),
        group_slots AS (SELECT r.Id,s.DisplayOrder FROM random_groups r JOIN eligible_groups s ON s.ExamPartId=r.ExamPartId AND s.SlotNo=r.SlotNo)
        INSERT toeic.AttemptQuestions(ExamAttemptId,QuestionId,ExamId,DisplayOrder)
            SELECT @ExamAttemptId,q.Id,@ExamId,ROW_NUMBER() OVER(ORDER BY p.DisplayOrder,
                CASE WHEN p.PartNumber=5 THEN COALESCE(qs.OriginalOrder,q.OriginalOrder) ELSE COALESCE(gs.DisplayOrder,g.DisplayOrder) END,q.OriginalOrder)
            FROM toeic.Questions q JOIN toeic.ExamParts p ON p.Id=q.ExamPartId LEFT JOIN toeic.ReadingGroups g ON g.Id=q.ReadingGroupId
            LEFT JOIN question_slots qs ON qs.Id=q.Id LEFT JOIN group_slots gs ON gs.Id=g.Id WHERE q.ExamId=@ExamId;
        INSERT toeic.AttemptOptionOrders(ExamAttemptId,QuestionId,QuestionOptionId,DisplayOrder)
            SELECT @ExamAttemptId,q.Id,o.Id,ROW_NUMBER() OVER(PARTITION BY q.Id ORDER BY CASE WHEN q.ShuffleOptions=1 THEN NEWID() END,o.OriginalOrder)
            FROM toeic.Questions q JOIN toeic.QuestionOptions o ON o.QuestionId=q.Id WHERE q.ExamId=@ExamId;
        INSERT toeic.AttemptAnswers(ExamAttemptId,QuestionId,SelectedOptionId,UpdatedAtUtc)
            SELECT @ExamAttemptId,QuestionId,NULL,@Now FROM toeic.AttemptQuestions WHERE ExamAttemptId=@ExamAttemptId;
        COMMIT;
        SELECT Id,StartedAtUtc,ExpiresAtUtc,Status FROM toeic.ExamAttempts WHERE Id=@ExamAttemptId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.SaveAnswer
    @ExamAttemptId bigint, @StudentUserId bigint, @QuestionId int, @SelectedOptionId int=NULL, @ExpectedRowVersion binary(8)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Status nvarchar(12),@Expires datetime2,@Version binary(8);
        SELECT @Status=Status,@Expires=ExpiresAtUtc FROM toeic.ExamAttempts WITH(UPDLOCK,HOLDLOCK) WHERE Id=@ExamAttemptId AND StudentUserId=@StudentUserId;
        IF @Status IS NULL THROW 51320, ''Attempt not found for this student.'', 1;
        IF @Status<>N''IN_PROGRESS'' OR @Expires<=SYSUTCDATETIME() THROW 51321, ''Attempt is submitted or expired.'', 1;
        IF NOT EXISTS(SELECT 1 FROM toeic.AttemptQuestions WHERE ExamAttemptId=@ExamAttemptId AND QuestionId=@QuestionId)
            THROW 51322, ''Question does not belong to this attempt.'', 1;
        IF @SelectedOptionId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM toeic.AttemptOptionOrders WHERE ExamAttemptId=@ExamAttemptId AND QuestionId=@QuestionId AND QuestionOptionId=@SelectedOptionId)
            THROW 51323, ''Option does not belong to this question/attempt.'', 1;
        SELECT @Version=RowVersion FROM toeic.AttemptAnswers WHERE ExamAttemptId=@ExamAttemptId AND QuestionId=@QuestionId;
        IF @ExpectedRowVersion IS NOT NULL AND (@Version IS NULL OR @Version<>@ExpectedRowVersion)
            THROW 51324, ''Stale answer version; reload before retrying.'', 1;
        UPDATE toeic.AttemptAnswers SET SelectedOptionId=@SelectedOptionId,UpdatedAtUtc=SYSUTCDATETIME() WHERE ExamAttemptId=@ExamAttemptId AND QuestionId=@QuestionId;
        IF @@ROWCOUNT=0 INSERT toeic.AttemptAnswers(ExamAttemptId,QuestionId,SelectedOptionId,UpdatedAtUtc) VALUES(@ExamAttemptId,@QuestionId,@SelectedOptionId,SYSUTCDATETIME());
        COMMIT;
        SELECT QuestionId,SelectedOptionId,UpdatedAtUtc,RowVersion FROM toeic.AttemptAnswers WHERE ExamAttemptId=@ExamAttemptId AND QuestionId=@QuestionId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.SubmitAttempt @ExamAttemptId bigint, @StudentUserId bigint=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @Status nvarchar(12),@Expires datetime2,@Now datetime2,@Correct int,@Wrong int,@Total int;
        SELECT @Status=Status,@Expires=ExpiresAtUtc,@Total=TotalQuestions FROM toeic.ExamAttempts WITH(UPDLOCK,HOLDLOCK)
            WHERE Id=@ExamAttemptId AND (@StudentUserId IS NULL OR StudentUserId=@StudentUserId);
        IF @Status IS NULL THROW 51330, ''Attempt not found for this student.'', 1;
        SET @Now=SYSUTCDATETIME();
        IF @Status=N''IN_PROGRESS''
        BEGIN
            SELECT @Correct=COALESCE(SUM(CASE WHEN aa.SelectedOptionId=k.CorrectOptionId THEN 1 ELSE 0 END),0),
                @Wrong=COALESCE(SUM(CASE WHEN aa.SelectedOptionId IS NOT NULL AND aa.SelectedOptionId<>k.CorrectOptionId THEN 1 ELSE 0 END),0)
            FROM toeic.AttemptQuestions aq JOIN toeic.AnswerKeys k ON k.QuestionId=aq.QuestionId
            LEFT JOIN toeic.AttemptAnswers aa ON aa.ExamAttemptId=aq.ExamAttemptId AND aa.QuestionId=aq.QuestionId WHERE aq.ExamAttemptId=@ExamAttemptId;
            UPDATE toeic.ExamAttempts SET Status=CASE WHEN @Now>=@Expires THEN N''EXPIRED'' ELSE N''SUBMITTED'' END,
                SubmittedAtUtc=@Now,CorrectCount=@Correct,WrongCount=@Wrong,SkippedCount=@Total-@Correct-@Wrong WHERE Id=@ExamAttemptId;
        END
        COMMIT;
        SELECT Id,Status,SubmittedAtUtc,CorrectCount,WrongCount,SkippedCount,CorrectPercentage FROM toeic.ExamAttempts WHERE Id=@ExamAttemptId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END');
GO

IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.ExpireDueAttempts @BatchSize int=100
AS
BEGIN
    SET NOCOUNT ON;
    IF @BatchSize NOT BETWEEN 1 AND 1000 THROW 51340, ''Batch size must be 1..1000.'', 1;
    DECLARE @AttemptId bigint;
    DECLARE due CURSOR LOCAL FAST_FORWARD FOR SELECT TOP(@BatchSize) Id FROM toeic.ExamAttempts
       WHERE Status=N''IN_PROGRESS'' AND ExpiresAtUtc<=SYSUTCDATETIME() ORDER BY ExpiresAtUtc,Id;
    OPEN due;
    FETCH NEXT FROM due INTO @AttemptId;
    WHILE @@FETCH_STATUS=0
    BEGIN
        EXEC toeic.SubmitAttempt @AttemptId;
        FETCH NEXT FROM due INTO @AttemptId;
    END
    CLOSE due;
    DEALLOCATE due;
END');
GO
