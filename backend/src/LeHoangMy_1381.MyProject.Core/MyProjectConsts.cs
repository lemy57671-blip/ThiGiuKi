using LeHoangMy_1381.MyProject.Debugging;

namespace LeHoangMy_1381.MyProject;

public class MyProjectConsts
{
    public const string LocalizationSourceName = "MyProject";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "9fdaeffb7f5c4a50a4e0c6c59f397ba8";
}
