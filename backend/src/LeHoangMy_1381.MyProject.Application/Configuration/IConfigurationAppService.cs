using LeHoangMy_1381.MyProject.Configuration.Dto;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
