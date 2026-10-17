using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using LeHoangMy_1381.MyProject.MultiTenancy;

namespace LeHoangMy_1381.MyProject.Sessions.Dto;

[AutoMapFrom(typeof(Tenant))]
public class TenantLoginInfoDto : EntityDto
{
    public string TenancyName { get; set; }

    public string Name { get; set; }
}
