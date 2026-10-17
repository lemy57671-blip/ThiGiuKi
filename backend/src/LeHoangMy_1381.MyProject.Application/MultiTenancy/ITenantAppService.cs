using Abp.Application.Services;
using LeHoangMy_1381.MyProject.MultiTenancy.Dto;

namespace LeHoangMy_1381.MyProject.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}
