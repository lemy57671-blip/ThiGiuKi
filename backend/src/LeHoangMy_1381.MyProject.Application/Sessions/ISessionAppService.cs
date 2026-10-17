using Abp.Application.Services;
using LeHoangMy_1381.MyProject.Sessions.Dto;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
