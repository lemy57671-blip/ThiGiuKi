using System.Collections.Generic;

namespace LeHoangMy_1381.MyProject.Authentication.External
{
    public interface IExternalAuthConfiguration
    {
        List<ExternalLoginProviderInfo> Providers { get; }
    }
}
