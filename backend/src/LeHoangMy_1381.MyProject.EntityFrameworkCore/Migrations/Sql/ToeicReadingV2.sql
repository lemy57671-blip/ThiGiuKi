IF DB_NAME()<>N'TOEICReadingMidterm' THROW 51000, 'Wrong database: expected TOEICReadingMidterm.', 1;
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
EXEC(N'CREATE OR ALTER PROCEDURE toeic.StartAttempt
    @ExamId int, @StudentUserId bigint, @ClientRequestId uniqueidentifier, @ExamAttemptId bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @TenantId int, @Status nvarchar(12), @Now datetime2;
        IF NOT EXISTS (SELECT 1 FROM dbo.AbpUsers WITH(UPDLOCK,HOLDLOCK) WHERE Id=@StudentUserId AND IsActive=1 AND IsDeleted=0)
            THROW 51310, ''Active ABP student user not found.'', 1;
        SET @ExamAttemptId=NULL; -- OUTPUT arguments must never be trusted as input.
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
        SET @Now=SYSUTCDATETIME(); -- Start the clock after locks and validation.
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
