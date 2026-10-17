using Abp.AutoMapper;
using LeHoangMy_1381.MyProject.Roles.Dto;
using LeHoangMy_1381.MyProject.Web.Models.Common;

namespace LeHoangMy_1381.MyProject.Web.Models.Roles;

[AutoMapFrom(typeof(GetRoleForEditOutput))]
public class EditRoleModalViewModel : GetRoleForEditOutput, IPermissionsEditViewModel
{
    public bool HasPermission(FlatPermissionDto permission)
    {
        return GrantedPermissionNames.Contains(permission.Name);
    }
}
