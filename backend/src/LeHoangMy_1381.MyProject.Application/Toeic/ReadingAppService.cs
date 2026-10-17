using Abp.Application.Services;
using Abp.Authorization;
using Abp.Auditing;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using LeHoangMy_1381.MyProject.Authorization;
using LeHoangMy_1381.MyProject.Toeic;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace LeHoangMy_1381.MyProject.Reading;

// Explicit controllers adapt MVC cookie+CSRF and bearer-only student HTTP routes.
// Keep the application boundary shared and prevent duplicate dynamic exposure.
[AbpAuthorize, RemoteService(false)]
public class ReadingAppService : MyProjectAppServiceBase, IReadingAppService
{
    private readonly IReadingStore _store;
    public ReadingAppService(IReadingStore store) { _store=store; }
    private ReadingActor Actor => new(AbpSession.GetUserId(),AbpSession.TenantId);
    private static long AttemptId(string id) => long.TryParse(id,out var n) && n>0 ? n : throw new UserFriendlyException("AttemptId không hợp lệ.");
    public Task<List<ExamSummary>> GetExamsAsync() => _store.ListAsync(Actor,false);
    public Task<List<AttemptState>> GetAttemptsAsync() => _store.AttemptsAsync(Actor);
    public Task<AttemptState> GetStateAsync(string id) => _store.StateAsync(Actor,AttemptId(id));
    public Task<ReadingPaper> GetPaperAsync(string id) => _store.PaperAsync(Actor,AttemptId(id));
    public Task<AttemptReview> GetReviewAsync(string id) => _store.ReviewAttemptAsync(Actor,AttemptId(id));
    public Task<AttemptState> StartAsync(StartReadingInput input) => _store.StartAsync(Actor,input);
    public Task<SavedAnswer> SaveAsync(SaveReadingInput input) => _store.SaveAsync(Actor,input);
    public Task<AttemptState> SubmitAsync(string id) => _store.SubmitAsync(Actor,AttemptId(id));
}
public interface IReadingAppService : IApplicationService
{
    Task<List<ExamSummary>> GetExamsAsync();
    Task<List<AttemptState>> GetAttemptsAsync();
    Task<AttemptState> GetStateAsync(string id);
    Task<ReadingPaper> GetPaperAsync(string id);
    Task<AttemptReview> GetReviewAsync(string id);
    Task<AttemptState> StartAsync(StartReadingInput input);
    Task<SavedAnswer> SaveAsync(SaveReadingInput input);
    Task<AttemptState> SubmitAsync(string id);
}
[AbpAuthorize(PermissionNames.Pages_Exams), RemoteService(false)]
public class ReadingAdminAppService : MyProjectAppServiceBase, IReadingAdminAppService
{
    private readonly IReadingStore _store;
    private readonly IUnitOfWorkManager _units;
    public ReadingAdminAppService(IReadingStore store, IUnitOfWorkManager units) { _store=store; _units=units; }
    private ReadingActor Actor => new(AbpSession.GetUserId(),AbpSession.TenantId,!AbpSession.TenantId.HasValue);
    public Task<List<ExamSummary>> GetExamsAsync() => _store.ListAsync(Actor,true);
    public Task<AdminPreview> GetPreviewAsync(int id) => _store.PreviewAsync(Actor,id);
    public Task<ExamStatistics> GetStatisticsAsync(int id, int? part = null) => _store.StatisticsAsync(Actor,id,part);
    [UnitOfWork] public Task EditAsync(EditQuestionInput input) => _store.EditAsync(Actor,input);
    [UnitOfWork, DisableAuditing] public Task EditBlockAsync(EditReadingBlockInput input) => _store.EditBlockAsync(Actor,input);
    [UnitOfWork] public Task ReviewAsync(ReviewInput input) => _store.ReviewAsync(Actor,input);
    [UnitOfWork] public Task PublishAsync(ReviewInput input) => _store.PublishAsync(Actor,input);
    [UnitOfWork] public async Task DeleteDraftAsync(ManageReadingExamInput input)
    {
        using var unit = _units.Begin(TransactionScopeOption.RequiresNew);
        await _store.DeleteDraftAsync(Actor,input);
        await unit.CompleteAsync();
    }
    [UnitOfWork] public async Task ArchiveAsync(ManageReadingExamInput input)
    {
        using var unit = _units.Begin(TransactionScopeOption.RequiresNew);
        await _store.ArchiveAsync(Actor,input);
        await unit.CompleteAsync();
    }
    public Task ValidateAsync(int id) => _store.ValidateAsync(Actor,id);
    [UnitOfWork, DisableAuditing] public async Task<ImportReadingResult> ImportAsync(byte[] data, string fileName, bool createNewVersion = false)
    {
        // Roll back even when MVC catches the validation error to render T03.
        using var unit = _units.Begin(TransactionScopeOption.RequiresNew);
        var result = await _store.ImportAsync(Actor,data,fileName,createNewVersion);
        await unit.CompleteAsync();
        return result;
    }
}
public interface IReadingAdminAppService : IApplicationService
{
    Task<List<ExamSummary>> GetExamsAsync();
    Task<AdminPreview> GetPreviewAsync(int id);
    Task<ExamStatistics> GetStatisticsAsync(int id, int? part = null);
    Task EditAsync(EditQuestionInput input);
    Task EditBlockAsync(EditReadingBlockInput input);
    Task ReviewAsync(ReviewInput input);
    Task PublishAsync(ReviewInput input);
    Task DeleteDraftAsync(ManageReadingExamInput input);
    Task ArchiveAsync(ManageReadingExamInput input);
    Task ValidateAsync(int id);
    Task<ImportReadingResult> ImportAsync(byte[] data, string fileName, bool createNewVersion = false);
}
