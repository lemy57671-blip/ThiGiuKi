using Abp.MultiTenancy;
using System.ComponentModel.DataAnnotations;

namespace LeHoangMy_1381.MyProject.Authorization.Accounts.Dto;

public class IsTenantAvailableInput
{
    [Required]
    [StringLength(AbpTenantBase.MaxTenancyNameLength)]
    public string TenancyName { get; set; }
}
