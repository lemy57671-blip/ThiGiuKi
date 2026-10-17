using LeHoangMy_1381.MyProject.Roles.Dto;
using System.Collections.Generic;

namespace LeHoangMy_1381.MyProject.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}
