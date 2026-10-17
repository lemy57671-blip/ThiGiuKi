using System.ComponentModel.DataAnnotations;

using Abp.Auditing;
using Abp.Authorization.Users;

namespace LeHoangMy_1381.MyProject.Users.Dto;

public class ResetPasswordDto
{
    [Required]
    [DisableAuditing]
    [StringLength(AbpUserBase.MaxPlainPasswordLength)]
    public string AdminPassword { get; set; }

    [Required]
    public long UserId { get; set; }

    [Required]
    [DisableAuditing]
    [StringLength(AbpUserBase.MaxPlainPasswordLength)]
    public string NewPassword { get; set; }
}
