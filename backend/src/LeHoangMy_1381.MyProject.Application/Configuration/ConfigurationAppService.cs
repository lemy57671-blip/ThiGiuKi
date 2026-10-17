using Abp.Authorization;
using Abp.Runtime.Session;
using LeHoangMy_1381.MyProject.Configuration.Dto;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : MyProjectAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
