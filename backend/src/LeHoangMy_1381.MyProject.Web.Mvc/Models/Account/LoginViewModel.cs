using Abp.Auditing;
using System.ComponentModel.DataAnnotations;

namespace LeHoangMy_1381.MyProject.Web.Models.Account;

public class LoginViewModel
{
    [Required]
    public string UsernameOrEmailAddress { get; set; }

    [Required]
    [DisableAuditing]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}
