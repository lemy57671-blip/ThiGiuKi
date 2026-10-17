using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using LeHoangMy_1381.MyProject.Configuration;
using LeHoangMy_1381.MyProject.EntityFrameworkCore;
using LeHoangMy_1381.MyProject.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace LeHoangMy_1381.MyProject.Migrator;

[DependsOn(typeof(MyProjectEntityFrameworkModule))]
public class MyProjectMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public MyProjectMigratorModule(MyProjectEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(MyProjectMigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            MyProjectConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MyProjectMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
