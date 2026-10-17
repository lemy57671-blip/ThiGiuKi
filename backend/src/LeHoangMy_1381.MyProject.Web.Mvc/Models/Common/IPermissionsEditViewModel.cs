using LeHoangMy_1381.MyProject.Roles.Dto;
using System.Collections.Generic;

namespace LeHoangMy_1381.MyProject.Web.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }
}