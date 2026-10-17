using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace LeHoangMy_1381.MyProject.Authorization;

public class MyProjectAuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        context.CreatePermission(PermissionNames.Pages_Exams, new FixedLocalizableString("Quản lý đề TOEIC"));
        context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
        context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
        context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
        context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, MyProjectConsts.LocalizationSourceName);
    }
}
