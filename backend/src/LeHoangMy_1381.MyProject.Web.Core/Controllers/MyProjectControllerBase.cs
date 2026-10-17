using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace LeHoangMy_1381.MyProject.Controllers
{
    public abstract class MyProjectControllerBase : AbpController
    {
        protected MyProjectControllerBase()
        {
            LocalizationSourceName = MyProjectConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
