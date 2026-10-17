using Abp.Application.Services;
using LeHoangMy_1381.MyProject.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}
