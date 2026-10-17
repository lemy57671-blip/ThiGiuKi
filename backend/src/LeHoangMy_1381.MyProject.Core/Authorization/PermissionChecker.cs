using Abp.Authorization;
using LeHoangMy_1381.MyProject.Authorization.Roles;
using LeHoangMy_1381.MyProject.Authorization.Users;

namespace LeHoangMy_1381.MyProject.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
