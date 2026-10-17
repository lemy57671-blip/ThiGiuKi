using Abp.Dependency;
using Abp.EntityFrameworkCore;
using Abp.UI;
using LeHoangMy_1381.MyProject.Toeic;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.EntityFrameworkCore.Toeic;

public class ReadingStore : IReadingStore, ITransientDependency
{
    private readonly IDbContextProvider<MyProjectDbContext> _provider;
    public ReadingStore(IDbContextProvider<MyProjectDbContext> provider) { _provider = provider; }
    private Task<MyProjectDbContext> Db() => _provider.GetDbContextAsync();
    private static string Id(long n) => n.ToString(CultureInfo.InvariantCulture);
    private static DateTime Utc(DateTime n) => DateTime.SpecifyKind(n, DateTimeKind.Utc);
    private static string Version(byte[] bytes) => Convert.ToBase64String(bytes);
    private static byte[] ParseVersion(string value)
    {
        try { var bytes = Convert.FromBase64String(value ?? ""); if (bytes.Length == 8) return bytes; }
        catch (FormatException) { }
        throw new UserFriendlyException("RowVersion không hợp lệ. Tải lại dữ liệu.");
    }
    private async Task<SqlCommand> Command(string text, params (string Name, object Value)[] values)
    {
        var db = await Db();
        var connection = (SqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await db.Database.OpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = text;
        command.CommandTimeout = 60;
        command.Transaction = (SqlTransaction)db.Database.CurrentTransaction?.GetDbTransaction();
        foreach (var value in values) command.Parameters.AddWithValue(value.Name, value.Value ?? DBNull.Value);
        return command;
    }
    private async Task Execute(string text, params (string Name, object Value)[] values)
    {
        using var command = await Command(text, values);
        try { await command.ExecuteNonQueryAsync(); }
        catch (SqlException ex) when (ex.Number >= 50000) { throw new UserFriendlyException(ex.Message); }
    }
    private async Task<Exam> ExamAsync(ReadingActor actor, int id, bool management, bool locked = false)
    {
        var db = await Db();
        Exam exam;
        if (locked)
        {
            // Serialize edit, answer review, publish and content-trigger locks on one exam.
            using var command = await Command("SELECT Id FROM toeic.Exams WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id", ("@id",id));
            await command.ExecuteScalarAsync();
        }
        exam = await db.Exams.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id);
        if (exam == null || (exam.TenantId != actor.TenantId && !(management && actor.HostManagement))) throw new UserFriendlyException("Không tìm thấy đề trong đơn vị của bạn.");
        return exam;
    }
    private async Task<Exam> DraftAsync(ReadingActor actor, int id, string version)
    {
        var exam = await ExamAsync(actor,id,true,true);
        if (exam.Status != "DRAFT" || exam.PublishedAtUtc != null) throw new UserFriendlyException("Đề đã xuất bản bị khóa. Hãy import phiên bản mới.");
        if (!exam.RowVersion.SequenceEqual(ParseVersion(version))) throw new UserFriendlyException("Đề đã thay đổi ở phiên khác. Tải lại trước khi lưu.");
        return exam;
    }
    private async Task<ExamAttempt> OwnedAsync(ReadingActor actor, long id)
    {
        var db = await Db();
        var a = await db.ExamAttempts.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.StudentUserId == actor.UserId && x.TenantId == actor.TenantId);
        return a ?? throw new UserFriendlyException("Không tìm thấy lượt thi của bạn.");
    }
    private static ExamSummary Summary(Exam e) => new() { Id=e.Id, Title=e.Title, SourceId=e.SourceId,Version=e.Version,Status=e.Status,DurationMinutes=e.DurationMinutes,QuestionCount=e.QuestionCount,ExamRowVersion=Version(e.RowVersion) };
    public async Task<List<ExamSummary>> ListAsync(ReadingActor actor, bool management)
    {
        var db = await Db();
        var exams = await db.Exams.IgnoreQueryFilters().AsNoTracking().Where(e => (e.TenantId == actor.TenantId || (management && actor.HostManagement)) && (management || e.Status == "PUBLISHED")).OrderByDescending(e => e.Id).ToListAsync();
        var result = exams.Select(Summary).ToList();
        if (management)
            foreach (var e in result)
            {
                e.VerifiedKeys = await (from k in db.AnswerKeys.AsNoTracking() join q in db.Questions.AsNoTracking() on k.QuestionId equals q.Id where q.ExamId == e.Id && k.IsVerified select k).CountAsync();
                var exam = exams.Single(x => x.Id == e.Id);
                e.CanDelete = exam.Status == "DRAFT" && exam.PublishedAtUtc == null && !await db.ExamAttempts.IgnoreQueryFilters().AnyAsync(a => a.ExamId == e.Id);
            }
        return result;
    }
    public async Task<AttemptState> StartAsync(ReadingActor actor, StartReadingInput input)
    {
        if (input.ClientRequestId == Guid.Empty) throw new UserFriendlyException("ClientRequestId phải là UUID khác rỗng.");
        var exam = await ExamAsync(actor,input.ExamId,false);
        if (exam.Status != "PUBLISHED") throw new UserFriendlyException("Đề chưa được giảng viên kích hoạt.");
        using var command = await Command("toeic.StartAttempt", ("@ExamId",exam.Id), ("@StudentUserId",actor.UserId), ("@ClientRequestId",input.ClientRequestId));
        command.CommandType = CommandType.StoredProcedure;
        var output = command.Parameters.Add("@ExamAttemptId",SqlDbType.BigInt); output.Direction = ParameterDirection.Output;
        try { await command.ExecuteNonQueryAsync(); }
        catch (SqlException ex) when (ex.Number >= 50000) { throw new UserFriendlyException(ex.Message); }
        return await StateAsync(actor,Convert.ToInt64(output.Value,CultureInfo.InvariantCulture));
    }
    public async Task<AttemptState> StateAsync(ReadingActor actor, long id)
    {
        var a = await OwnedAsync(actor,id);
        if (a.Status == "IN_PROGRESS" && a.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await Execute("EXEC toeic.SubmitAttempt @id,@student",("@id",id),("@student",actor.UserId));
            a = await OwnedAsync(actor,id);
        }
        var db = await Db();
        var title = await db.Exams.IgnoreQueryFilters().Where(e => e.Id == a.ExamId).Select(e => e.Title).SingleAsync();
        var answers = await db.AttemptAnswers.AsNoTracking().Where(x => x.ExamAttemptId == id).ToListAsync();
        var result = new AttemptState
        {
            Id=Id(a.Id),ExamId=a.ExamId,ExamTitle=title,Status=a.Status,StartedAtUtc=Utc(a.StartedAtUtc),ExpiresAtUtc=Utc(a.ExpiresAtUtc),ServerNowUtc=DateTime.UtcNow,
            SubmittedAtUtc=a.SubmittedAtUtc.HasValue ? Utc(a.SubmittedAtUtc.Value) : null,TotalQuestions=a.TotalQuestions,
            CorrectCount=a.CorrectCount,WrongCount=a.WrongCount,SkippedCount=a.SkippedCount,AccuracyPercent=a.CorrectPercentage,
            Answers=answers.Select(x => new SavedAnswer {QuestionId=x.QuestionId,SelectedOptionId=x.SelectedOptionId,RowVersion=Version(x.RowVersion)}).ToList()
        };
        if (a.Status != "IN_PROGRESS")
        {
            using var command = await Command(@"SELECT p.PartNumber,COUNT(*) AS Total,
                SUM(CASE WHEN aa.SelectedOptionId=k.CorrectOptionId THEN 1 ELSE 0 END),
                SUM(CASE WHEN aa.SelectedOptionId IS NOT NULL AND aa.SelectedOptionId<>k.CorrectOptionId THEN 1 ELSE 0 END),
                SUM(CASE WHEN aa.SelectedOptionId IS NULL THEN 1 ELSE 0 END)
                FROM toeic.AttemptQuestions aq JOIN toeic.Questions q ON q.Id=aq.QuestionId
                JOIN toeic.ExamParts p ON p.Id=q.ExamPartId JOIN toeic.AnswerKeys k ON k.QuestionId=q.Id
                LEFT JOIN toeic.AttemptAnswers aa ON aa.ExamAttemptId=aq.ExamAttemptId AND aa.QuestionId=q.Id
                WHERE aq.ExamAttemptId=@id GROUP BY p.PartNumber ORDER BY p.PartNumber",("@id",id));
            using var reader = await command.ExecuteReaderAsync();
            while(await reader.ReadAsync()) result.PartResults.Add(new PartResult { PartNumber=Convert.ToInt32(reader[0]),Total=reader.GetInt32(1),Correct=reader.GetInt32(2),Wrong=reader.GetInt32(3),Skipped=reader.GetInt32(4) });
        }
        return result;
    }
    public async Task<List<AttemptState>> AttemptsAsync(ReadingActor actor)
    {
        var db = await Db();
        var ids = await db.ExamAttempts.IgnoreQueryFilters().AsNoTracking().Where(a => a.StudentUserId == actor.UserId && a.TenantId == actor.TenantId).OrderByDescending(a => a.Id).Take(50).Select(a => a.Id).ToListAsync();
        var result = new List<AttemptState>();
        foreach (var id in ids) result.Add(await StateAsync(actor,id));
        return result;
    }
    public async Task<ReadingPaper> PaperAsync(ReadingActor actor, long attemptId)
    {
        var attempt = await OwnedAsync(actor,attemptId);
        return await BuildPaper(await ExamAsync(actor,attempt.ExamId,false),attemptId);
    }
    public async Task<AttemptReview> ReviewAttemptAsync(ReadingActor actor, long attemptId)
    {
        var state=await StateAsync(actor,attemptId);
        if(state.Status == "IN_PROGRESS") throw new UserFriendlyException("Chỉ xem đáp án và gợi ý sau khi bài thi đã kết thúc.");
        var paper=await PaperAsync(actor,attemptId); var db=await Db();
        var ids=paper.Questions.Select(q=>q.Id).ToList();
        var keys=await db.AnswerKeys.AsNoTracking().Where(k=>ids.Contains(k.QuestionId)).ToListAsync();
        return new AttemptReview { Paper=paper,Questions=keys.Select(k=>
        {
            var selected=state.Answers.SingleOrDefault(a=>a.QuestionId==k.QuestionId)?.SelectedOptionId;
            return new QuestionReview { QuestionId=k.QuestionId,SelectedOptionId=selected,CorrectOptionId=k.CorrectOptionId,Hint=k.Hint,Outcome=selected==null?"SKIPPED":selected==k.CorrectOptionId?"CORRECT":"WRONG" };
        }).ToList() };
    }
    private async Task<ReadingPaper> BuildPaper(Exam exam, long? attemptId)
    {
        var db = await Db();
        var parts = await db.ExamParts.AsNoTracking().Where(p => p.ExamId == exam.Id).ToDictionaryAsync(p => p.Id);
        var questions = await db.Questions.AsNoTracking().Where(q => q.ExamId == exam.Id).OrderBy(q => q.OriginalOrder).ToListAsync();
        var qids = questions.Select(q => q.Id).ToList();
        var options = await db.QuestionOptions.AsNoTracking().Where(o => qids.Contains(o.QuestionId)).ToListAsync();
        var groups = await db.ReadingGroups.AsNoTracking().Where(g => parts.Keys.Contains(g.ExamPartId)).OrderBy(g => g.DisplayOrder).ToListAsync();
        var gids = groups.Select(g => g.Id).ToList();
        var blocks = await db.ReadingBlocks.AsNoTracking().Where(b => gids.Contains(b.ReadingGroupId)).OrderBy(b => b.DisplayOrder).ToListAsync();
        var gaps = await db.GapReferences.AsNoTracking().Where(g => qids.Contains(g.QuestionId)).ToDictionaryAsync(g => g.QuestionId);
        var order = attemptId.HasValue ? await db.AttemptQuestions.AsNoTracking().Where(a => a.ExamAttemptId == attemptId.Value).ToDictionaryAsync(a => a.QuestionId,a => a.DisplayOrder) : questions.ToDictionary(q => q.Id,q => q.OriginalOrder);
        var optionOrder = attemptId.HasValue ? await db.AttemptOptionOrders.AsNoTracking().Where(a => a.ExamAttemptId == attemptId.Value).ToDictionaryAsync(a => a.QuestionOptionId,a => a.DisplayOrder) : options.ToDictionary(o => o.Id,o => o.OriginalOrder);
        return new ReadingPaper
        {
            Exam=Summary(exam),
            Questions=questions.Select(q => new PaperQuestion
            {
                Id=q.Id,OriginalNumber=q.OriginalNumber,DisplayOrder=order[q.Id],PartNumber=parts[q.ExamPartId].PartNumber,GroupId=q.ReadingGroupId,
                GapLabel=gaps.TryGetValue(q.Id,out var gap) ? gap.GapLabel : null,Stem=q.Stem,
                Options=options.Where(o => o.QuestionId == q.Id).OrderBy(o => optionOrder[o.Id]).Select(o => new PaperOption { Id=o.Id,Label=((char)('A'+optionOrder[o.Id]-1)).ToString(),Content=o.Content }).ToList()
            }).OrderBy(q => q.DisplayOrder).ToList(),
            Groups=groups.Select(g => new PaperGroup
            {
                Id=g.Id,PartNumber=parts[g.ExamPartId].PartNumber,FirstQuestionNumber=g.FirstQuestionNumber,LastQuestionNumber=g.LastQuestionNumber,
                Blocks=blocks.Where(b => b.ReadingGroupId == g.Id).Select(b => new PaperBlock { Id=b.Id,DisplayOrder=b.DisplayOrder,BlockType=b.BlockType,TextContent=b.TextContent,ImageBase64=b.ImageData == null ? null : Convert.ToBase64String(b.ImageData),MimeType=b.MimeType }).ToList()
            }).ToList()
        };
    }
    public async Task<SavedAnswer> SaveAsync(ReadingActor actor, SaveReadingInput input)
    {
        if (!long.TryParse(input.AttemptId,out var id) || id <= 0) throw new UserFriendlyException("AttemptId không hợp lệ.");
        await OwnedAsync(actor,id);
        var version = ParseVersion(input.ExpectedRowVersion);
        await Execute("EXEC toeic.SaveAnswer @id,@student,@question,@option,@version",("@id",id),("@student",actor.UserId),("@question",input.QuestionId),("@option",input.SelectedOptionId),("@version",version));
        var db = await Db();
        var answer = await db.AttemptAnswers.AsNoTracking().SingleAsync(a => a.ExamAttemptId == id && a.QuestionId == input.QuestionId);
        return new SavedAnswer { QuestionId=answer.QuestionId,SelectedOptionId=answer.SelectedOptionId,RowVersion=Version(answer.RowVersion) };
    }
    public async Task<AttemptState> SubmitAsync(ReadingActor actor, long id)
    {
        await OwnedAsync(actor,id);
        await Execute("EXEC toeic.SubmitAttempt @id,@student",("@id",id),("@student",actor.UserId));
        return await StateAsync(actor,id);
    }
    public async Task<AdminPreview> PreviewAsync(ReadingActor actor, int examId)
    {
        var exam = await ExamAsync(actor,examId,true);
        var paper = await BuildPaper(exam,null);
        var db = await Db();
        paper.Exam.CanDelete = exam.Status == "DRAFT" && exam.PublishedAtUtc == null && !await db.ExamAttempts.IgnoreQueryFilters().AnyAsync(a => a.ExamId == examId);
        var source = await db.Questions.AsNoTracking().Where(q => q.ExamId == examId).ToDictionaryAsync(q => q.Id);
        var keys = await db.AnswerKeys.AsNoTracking().Where(k => source.Keys.Contains(k.QuestionId)).ToDictionaryAsync(k => k.QuestionId);
        return new AdminPreview
        {
            Paper=paper,RowVersion=Version(exam.RowVersion),
            Questions=paper.Questions.Select(q => new AdminQuestion { Question=q,CorrectOptionId=keys[q.Id].CorrectOptionId,Hint=keys[q.Id].Hint,IsVerified=keys[q.Id].IsVerified,ShuffleQuestion=source[q.Id].ShuffleQuestion,ShuffleOptions=source[q.Id].ShuffleOptions }).ToList()
        };
    }
    public async Task EditAsync(ReadingActor actor, EditQuestionInput input)
    {
        await DraftAsync(actor,input.ExamId,input.ExamRowVersion);
        var db = await Db();
        var q = await db.Questions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.QuestionId && x.ExamId == input.ExamId) ?? throw new UserFriendlyException("Câu không thuộc đề.");
        var part = await db.ExamParts.AsNoTracking().SingleAsync(p => p.Id == q.ExamPartId);
        var options = await db.QuestionOptions.AsNoTracking().Where(o => o.QuestionId == q.Id).ToListAsync();
        if (input.Options == null || input.Options.Count != 4 || !input.Options.Select(o => o.Id).OrderBy(i => i).SequenceEqual(options.Select(o => o.Id).OrderBy(i => i)) || input.Options.Any(o => string.IsNullOrWhiteSpace(o.Content)) || !options.Any(o => o.Id == input.CorrectOptionId) || string.IsNullOrWhiteSpace(input.Hint) || (part.PartNumber != 6 && string.IsNullOrWhiteSpace(input.Stem)) || (part.PartNumber != 5 && input.ShuffleQuestion)) throw new UserFriendlyException("Nội dung, bốn option, key/HINT hoặc cờ hoán vị không hợp lệ.");
        await Execute("UPDATE toeic.Questions SET Stem=@stem,ShuffleQuestion=@sq,ShuffleOptions=@so WHERE Id=@id",("@stem",input.Stem?.Trim() ?? ""),("@sq",input.ShuffleQuestion),("@so",input.ShuffleOptions),("@id",q.Id));
        foreach (var option in input.Options) await Execute("UPDATE toeic.QuestionOptions SET Content=@content WHERE Id=@id AND QuestionId=@qid",("@content",option.Content.Trim()),("@id",option.Id),("@qid",q.Id));
        await Execute("UPDATE toeic.AnswerKeys SET CorrectOptionId=@option,Hint=@hint,IsVerified=@verified,VerifiedByUserId=@reviewer,VerifiedAtUtc=CASE WHEN @verified=1 THEN SYSUTCDATETIME() ELSE NULL END WHERE QuestionId=@id",("@option",input.CorrectOptionId),("@hint",input.Hint.Trim()),("@verified",input.ConfirmAnswer),("@reviewer",input.ConfirmAnswer ? actor.UserId : null),("@id",q.Id));
        // Changing a shared reading item would invalidate all its keys; this editor changes one question only.
        await Execute("UPDATE toeic.Exams SET Title=Title WHERE Id=@id",("@id",input.ExamId));
        await ValidateAsync(actor,input.ExamId);
    }
    public async Task ReviewAsync(ReadingActor actor, ReviewInput input)
    {
        await DraftAsync(actor,input.ExamId,input.ExamRowVersion);
        if (!input.ConfirmReviewed) throw new UserFriendlyException("Cần xác nhận đã rà soát đầy đủ 100 đáp án và gợi ý.");
        await ValidateAsync(actor,input.ExamId);
        await Execute("UPDATE k SET IsVerified=1,VerifiedByUserId=@reviewer,VerifiedAtUtc=SYSUTCDATETIME() FROM toeic.AnswerKeys k JOIN toeic.Questions q ON q.Id=k.QuestionId WHERE q.ExamId=@id; UPDATE toeic.Exams SET Title=Title WHERE Id=@id;",("@reviewer",actor.UserId),("@id",input.ExamId));
    }
    public async Task EditBlockAsync(ReadingActor actor, EditReadingBlockInput input)
    {
        await DraftAsync(actor,input.ExamId,input.ExamRowVersion);var db=await Db();
        var block=await (from b in db.ReadingBlocks.AsNoTracking() join g in db.ReadingGroups.AsNoTracking() on b.ReadingGroupId equals g.Id join p in db.ExamParts.AsNoTracking() on g.ExamPartId equals p.Id where b.Id==input.BlockId && p.ExamId==input.ExamId select b).SingleOrDefaultAsync() ?? throw new UserFriendlyException("Bài đọc không thuộc đề.");
        if(block.BlockType=="IMAGE")
        {
            var data=input.ImageData;
            if(data==null || data.Length<8 || data.Length>10*1024*1024) throw new UserFriendlyException("Chọn ảnh PNG/JPEG không quá 10 MB.");
            var mime=ReadingImageValidator.Mime(data);
            if(mime==null) throw new UserFriendlyException("Ảnh PNG/JPEG bị hỏng hoặc không đúng định dạng.");
            await Execute("UPDATE toeic.ReadingBlocks SET ImageData=@data,MimeType=@mime,ImageSha256=@hash WHERE Id=@id",("@data",data),("@mime",mime),("@hash",Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()),("@id",block.Id));
        }
        else
        {
            if(string.IsNullOrWhiteSpace(input.TextContent)) throw new UserFriendlyException("Nội dung bài đọc không được rỗng.");
            await Execute("UPDATE toeic.ReadingBlocks SET TextContent=@text WHERE Id=@id",("@text",input.TextContent.Trim()),("@id",block.Id));
        }
        await Execute("UPDATE k SET IsVerified=0,VerifiedByUserId=NULL,VerifiedAtUtc=NULL FROM toeic.AnswerKeys k JOIN toeic.Questions q ON q.Id=k.QuestionId WHERE q.ReadingGroupId=@group; UPDATE toeic.Exams SET Title=Title WHERE Id=@id;",("@group",block.ReadingGroupId),("@id",input.ExamId));
        await ValidateAsync(actor,input.ExamId);
    }
    public async Task PublishAsync(ReadingActor actor, ReviewInput input)
    {
        await DraftAsync(actor,input.ExamId,input.ExamRowVersion);
        await Execute("EXEC toeic.PublishExam @id",("@id",input.ExamId));
    }
    public async Task DeleteDraftAsync(ReadingActor actor, ManageReadingExamInput input)
    {
        await DraftAsync(actor,input.ExamId,input.ExamRowVersion);
        var db = await Db();
        if (await db.ExamAttempts.IgnoreQueryFilters().AnyAsync(a => a.ExamId == input.ExamId))
            throw new UserFriendlyException("Đề đã có lượt làm bài nên không thể xóa. Lịch sử thi được giữ nguyên.");
        // All foreign keys remain enabled. The application service owns the transaction.
        await Execute(@"DELETE k FROM toeic.AnswerKeys k JOIN toeic.Questions q ON q.Id=k.QuestionId WHERE q.ExamId=@id;
            DELETE g FROM toeic.GapReferences g JOIN toeic.Questions q ON q.Id=g.QuestionId WHERE q.ExamId=@id;
            DELETE o FROM toeic.QuestionOptions o JOIN toeic.Questions q ON q.Id=o.QuestionId WHERE q.ExamId=@id;
            DELETE FROM toeic.Questions WHERE ExamId=@id;
            DELETE b FROM toeic.ReadingBlocks b JOIN toeic.ReadingGroups g ON g.Id=b.ReadingGroupId JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@id;
            DELETE g FROM toeic.ReadingGroups g JOIN toeic.ExamParts p ON p.Id=g.ExamPartId WHERE p.ExamId=@id;
            DELETE FROM toeic.ExamParts WHERE ExamId=@id;
            DELETE FROM toeic.Exams WHERE Id=@id;",("@id",input.ExamId));
    }
    public async Task ArchiveAsync(ReadingActor actor, ManageReadingExamInput input)
    {
        var exam = await ExamAsync(actor,input.ExamId,true,true);
        if (exam.Status != "PUBLISHED" || exam.PublishedAtUtc == null)
            throw new UserFriendlyException("Chỉ đề đã xuất bản mới có thể lưu trữ.");
        if (!exam.RowVersion.SequenceEqual(ParseVersion(input.ExamRowVersion)))
            throw new UserFriendlyException("Đề đã thay đổi ở phiên khác. Tải lại trước khi lưu trữ.");
        await Execute("UPDATE toeic.Exams SET Status=N'ARCHIVED' WHERE Id=@id",("@id",input.ExamId));
    }
    public async Task ValidateAsync(ReadingActor actor, int examId)
    {
        await ExamAsync(actor,examId,true);
        await Execute("EXEC toeic.ValidateExam @id,0",("@id",examId));
    }
    public async Task<ExamStatistics> StatisticsAsync(ReadingActor actor, int examId, int? part = null)
    {
        if(part.HasValue && part is not (5 or 6 or 7)) throw new UserFriendlyException("Part phải là 5, 6 hoặc 7.");
        var exam = await ExamAsync(actor,examId,true);
        var db = await Db();
        var attempts = db.ExamAttempts.IgnoreQueryFilters().AsNoTracking().Where(a => a.ExamId == examId);
        var completed = attempts.Where(a => a.Status != "IN_PROGRESS");
        var wrong = from answer in db.AttemptAnswers.AsNoTracking()
                    join attempt in completed on answer.ExamAttemptId equals attempt.Id
                    join key in db.AnswerKeys.AsNoTracking() on answer.QuestionId equals key.QuestionId
                    join question in db.Questions.AsNoTracking() on answer.QuestionId equals question.Id
                    join examPart in db.ExamParts.AsNoTracking() on question.ExamPartId equals examPart.Id
                    where answer.SelectedOptionId != null && answer.SelectedOptionId != key.CorrectOptionId && (!part.HasValue || examPart.PartNumber == part.Value)
                    group answer by question.OriginalNumber into g
                    select new WrongQuestionStat { OriginalNumber=g.Key,WrongCount=g.Count() };
        var result = new ExamStatistics { Exam=Summary(exam),TotalAttempts=await attempts.CountAsync(),CompletedAttempts=await completed.CountAsync(),AverageAccuracyPercent=await completed.AverageAsync(a => a.CorrectPercentage),TopWrong=await wrong.OrderByDescending(w => w.WrongCount).ThenBy(w => w.OriginalNumber).Take(10).ToListAsync() };
        var scores=await completed.Select(a=>a.CorrectPercentage.Value).ToListAsync();
        for(var lower=0;lower<100;lower+=20)
            result.Distribution.Add(new ScoreBucket { Label=lower==80?"80–100%":$"{lower}–<{lower+20}%",Count=scores.Count(s=>s>=lower && (s<lower+20 || (lower==80 && s==100))) });
        using var command=await Command(@"SELECT p.PartNumber,COUNT(*),
            SUM(CASE WHEN aa.SelectedOptionId=k.CorrectOptionId THEN 1 ELSE 0 END),
            SUM(CASE WHEN aa.SelectedOptionId IS NOT NULL AND aa.SelectedOptionId<>k.CorrectOptionId THEN 1 ELSE 0 END),
            SUM(CASE WHEN aa.SelectedOptionId IS NULL THEN 1 ELSE 0 END)
            FROM toeic.ExamAttempts a JOIN toeic.AttemptQuestions aq ON aq.ExamAttemptId=a.Id
            JOIN toeic.Questions q ON q.Id=aq.QuestionId JOIN toeic.ExamParts p ON p.Id=q.ExamPartId
            JOIN toeic.AnswerKeys k ON k.QuestionId=q.Id
            LEFT JOIN toeic.AttemptAnswers aa ON aa.ExamAttemptId=a.Id AND aa.QuestionId=q.Id
            WHERE a.ExamId=@id AND a.Status<>N'IN_PROGRESS' GROUP BY p.PartNumber ORDER BY p.PartNumber",("@id",examId));
        using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync())result.Parts.Add(new PartResult {PartNumber=Convert.ToInt32(reader[0]),Total=reader.GetInt32(1),Correct=reader.GetInt32(2),Wrong=reader.GetInt32(3),Skipped=reader.GetInt32(4)});
        return result;
    }
    public async Task<ImportReadingResult> ImportAsync(ReadingActor actor, byte[] data, string fileName, bool createNewVersion = false)
    {
        var root = TaggedReadingParser.Parse(data); // Complete validation precedes writes.
        var hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        var db = await Db();
        await Execute("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'toeic-word-import',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW 51200,'Import is busy; retry.',1;");
        var sourceId=root.Attr("id"); var sourceVersion=root.Attr("version");
        if (string.IsNullOrWhiteSpace(sourceVersion) || sourceVersion.Length > 30) throw new UserFriendlyException("Phiên bản trong Word phải có từ 1 đến 30 ký tự.");
        if (createNewVersion && actor.HostManagement) throw new UserFriendlyException("Hãy chuyển sang đơn vị Default trước khi tạo bản nháp mới.");
        var versions = await db.Exams.IgnoreQueryFilters().AsNoTracking().Where(e => (e.TenantId == actor.TenantId || actor.HostManagement) && e.SourceId == sourceId).OrderByDescending(e => e.Id).ToListAsync();
        var existing = versions.Where(e => string.Equals(e.Version,sourceVersion,StringComparison.OrdinalIgnoreCase)).ToList();
        // A repeated upload reopens its draft, even if its version was allocated by the website.
        // Published versions are never overwritten or changed back into drafts.
        var same = createNewVersion
            ? versions.FirstOrDefault(e => e.Status == "DRAFT" && e.PublishedAtUtc == null && e.SourceSha256 == hash)
            : existing.FirstOrDefault(e => e.SourceSha256 == hash);
        if (same != null) { await ValidateAsync(actor,same.Id); return new ImportReadingResult { ExamId=same.Id,Version=same.Version,Status="ALREADY_IMPORTED",Questions=100,Images=27 }; }
        if (existing.Count > 0 && !createNewVersion) throw new UserFriendlyException("Phiên bản trong Word đã tồn tại với file khác. Chọn ‘Tạo bản nháp mới từ file Word’ để hệ thống cấp phiên bản mới; bạn không cần sửa file Word.");
        if (actor.HostManagement) throw new UserFriendlyException("Hãy chuyển sang đơn vị Default trước khi import phiên bản mới.");
        var version = sourceVersion;
        if (existing.Count > 0)
        {
            var revision = 0;
            do
            {
                var suffix = "-r" + (++revision).ToString(CultureInfo.InvariantCulture);
                version = sourceVersion[..Math.Min(sourceVersion.Length,30-suffix.Length)] + suffix;
            } while (versions.Any(e => string.Equals(e.Version,version,StringComparison.OrdinalIgnoreCase)));
        }
        var exam = new Exam { TenantId=actor.TenantId,SourceId=sourceId,Version=version,Title=root.Attr("title"),SourceDocument=System.IO.Path.GetFileName(fileName),SourceSha256=hash,AnswerProvenance="Imported tagged Word; instructor review required.",Status="DRAFT",CreatedByUserId=actor.UserId,CreatedAtUtc=DateTime.UtcNow };
        db.Exams.Add(exam); await db.SaveChangesAsync();
        var groupIds = new Dictionary<string,int>(); var blockIds = new Dictionary<string,int>(); var questionIds = new Dictionary<string,int>();
        var partOrder = 0;
        foreach (var p in root.All("PART"))
        {
            var part = new ExamPart { ExamId=exam.Id,SourceId=p.Attr("id"),PartNumber=byte.Parse(p.Attr("id")[1..],CultureInfo.InvariantCulture),DisplayOrder=++partOrder,ExpectedQuestionCount=p.Number("question_count"),ShuffleQuestions=p.Flag("shuffle_questions") };
            db.ExamParts.Add(part); await db.SaveChangesAsync();
            var groupOrder = 0;
            foreach (var g in p.All("GROUP"))
            {
                var group = new ReadingGroup { ExamPartId=part.Id,SourceId=g.Attr("id"),DisplayOrder=++groupOrder,FirstQuestionNumber=g.Number("from"),LastQuestionNumber=g.Number("to"),ShuffleGroup=g.Flag("shuffle_group"),ShuffleQuestions=g.Flag("shuffle_questions") };
                db.ReadingGroups.Add(group); await db.SaveChangesAsync(); groupIds.Add(group.SourceId,group.Id);
                var blockOrder = 0;
                foreach (var b in g.All("BLOCK"))
                {
                    var image = b.Images.FirstOrDefault();
                    var block = new ReadingBlock { ReadingGroupId=group.Id,SourceId=b.Attr("id"),DisplayOrder=++blockOrder,BlockType=b.Attr("type").ToUpperInvariant(),TextContent=image == null ? b.Text : null,ImageData=image,MimeType=image == null ? null : b.Attr("mime"),ImageSha256=image == null ? null : Convert.ToHexString(SHA256.HashData(image)).ToLowerInvariant(),SourcePdfPage=b.Number("source_pdf_page") };
                    db.ReadingBlocks.Add(block); await db.SaveChangesAsync(); blockIds.Add(block.SourceId,block.Id);
                }
            }
            foreach (var q in p.All("QUESTION"))
            {
                var question = new Question { ExamId=exam.Id,ExamPartId=part.Id,ReadingGroupId=groupIds.TryGetValue(q.Attr("group"),out var gid) ? gid : null,SourceId=q.Attr("id"),OriginalNumber=q.Number("number"),OriginalOrder=q.Number("number")-100,Stem=q.One("STEM").Text,ShuffleQuestion=q.Flag("shuffle_question"),ShuffleOptions=q.Flag("shuffle_options") };
                db.Questions.Add(question); await db.SaveChangesAsync(); questionIds.Add(question.SourceId,question.Id);
                var options = q.All("OPTION").Select(o => new QuestionOption { QuestionId=question.Id,SourceId=o.Attr("id"),OriginalLabel=o.Attr("label"),OriginalOrder="ABCD".IndexOf(o.Attr("label"),StringComparison.Ordinal)+1,Content=o.Text }).ToList();
                db.QuestionOptions.AddRange(options); await db.SaveChangesAsync();
                db.AnswerKeys.Add(new AnswerKey { QuestionId=question.Id,CorrectOptionId=options.Single(o => o.SourceId == q.One("ANSWER").Attr("option_id")).Id,Hint=q.One("HINT").Text,IsVerified=false });
                await db.SaveChangesAsync();
            }
            foreach (var g in p.All("GROUP")) foreach (var gap in g.All("GAP_REF"))
                db.GapReferences.Add(new GapReference { QuestionId=questionIds[gap.Attr("question_id")],ReadingGroupId=groupIds[g.Attr("id")],ReadingBlockId=blockIds[gap.Attr("source_block")],GapLabel=gap.Number("gap_label") });
            await db.SaveChangesAsync();
        }
        await ValidateAsync(actor,exam.Id);
        return new ImportReadingResult { ExamId=exam.Id,Version=exam.Version,Status="IMPORTED_DRAFT",Questions=100,Images=27 };
    }
    public Task ExpireAsync() => Execute("EXEC toeic.ExpireDueAttempts @BatchSize=100");
}
