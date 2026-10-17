using Abp.Modules;
using Abp.Reflection.Extensions;
using LeHoangMy_1381.MyProject.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace LeHoangMy_1381.MyProject.Web.Host.Startup
{
    [DependsOn(
       typeof(MyProjectWebCoreModule))]
    public class MyProjectWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public MyProjectWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(MyProjectWebHostModule).GetAssembly());
        }
    }
}
