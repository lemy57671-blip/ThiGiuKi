using Abp.AspNetCore.Mvc.ViewComponents;

namespace LeHoangMy_1381.MyProject.Web.Views;

public abstract class MyProjectViewComponent : AbpViewComponent
{
    protected MyProjectViewComponent()
    {
        LocalizationSourceName = MyProjectConsts.LocalizationSourceName;
    }
}
