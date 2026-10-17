using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using LeHoangMy_1381.MyProject.Toeic;

namespace LeHoangMy_1381.MyProject.EntityFrameworkCore.Toeic;

public class ReadingExpiryWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IUnitOfWorkManager _units;
    private readonly IIocResolver _resolver;
    public ReadingExpiryWorker(AbpTimer timer, IUnitOfWorkManager units, IIocResolver resolver) : base(timer)
    {
        _units=units; _resolver=resolver; Timer.Period=15000; Timer.RunOnStart=true;
    }
    protected override void DoWork()
    {
        AsyncHelper.RunSync(async () =>
        {
            using var unit = _units.Begin();
            using var store = _resolver.ResolveAsDisposable<IReadingStore>();
            await store.Object.ExpireAsync();
            await unit.CompleteAsync();
        });
    }
}
