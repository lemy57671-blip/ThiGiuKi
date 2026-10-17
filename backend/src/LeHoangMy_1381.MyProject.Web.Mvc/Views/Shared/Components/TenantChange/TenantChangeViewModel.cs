using Abp.AutoMapper;
using LeHoangMy_1381.MyProject.Sessions.Dto;

namespace LeHoangMy_1381.MyProject.Web.Views.Shared.Components.TenantChange;

[AutoMapFrom(typeof(GetCurrentLoginInformationsOutput))]
public class TenantChangeViewModel
{
    public TenantLoginInfoDto Tenant { get; set; }
}
