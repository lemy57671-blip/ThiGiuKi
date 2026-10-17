using LeHoangMy_1381.MyProject.Roles.Dto;
using LeHoangMy_1381.MyProject.Users.Dto;
using System.Collections.Generic;
using System.Linq;

namespace LeHoangMy_1381.MyProject.Web.Models.Users;

public class EditUserModalViewModel
{
    public UserDto User { get; set; }

    public IReadOnlyList<RoleDto> Roles { get; set; }

    public bool UserIsInRole(RoleDto role)
    {
        return User.RoleNames != null && User.RoleNames.Any(r => r == role.NormalizedName);
    }
}
