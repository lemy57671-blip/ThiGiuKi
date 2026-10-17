using Abp.Application.Services;
using Abp.Application.Services.Dto;
using LeHoangMy_1381.MyProject.Roles.Dto;
using LeHoangMy_1381.MyProject.Users.Dto;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Users;

public interface IUserAppService : IAsyncCrudAppService<UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>
{
    Task DeActivate(EntityDto<long> user);
    Task Activate(EntityDto<long> user);
    Task<ListResultDto<RoleDto>> GetRoles();
    Task ChangeLanguage(ChangeUserLanguageDto input);

    Task<bool> ChangePassword(ChangePasswordDto input);
}
