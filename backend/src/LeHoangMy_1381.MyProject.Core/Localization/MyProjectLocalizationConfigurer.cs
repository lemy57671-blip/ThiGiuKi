using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace LeHoangMy_1381.MyProject.Localization;

public static class MyProjectLocalizationConfigurer
{
    public static void Configure(ILocalizationConfiguration localizationConfiguration)
    {
        localizationConfiguration.Sources.Add(
            new DictionaryBasedLocalizationSource(MyProjectConsts.LocalizationSourceName,
                new XmlEmbeddedFileLocalizationDictionaryProvider(
                    typeof(MyProjectLocalizationConfigurer).GetAssembly(),
                    "LeHoangMy_1381.MyProject.Localization.SourceFiles"
                )
            )
        );
    }
}
