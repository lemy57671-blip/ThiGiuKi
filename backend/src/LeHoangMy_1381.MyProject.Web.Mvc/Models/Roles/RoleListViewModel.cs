using LeHoangMy_1381.MyProject.Roles.Dto;
using System.Collections.Generic;

namespace LeHoangMy_1381.MyProject.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}
