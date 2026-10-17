namespace LeHoangMy_1381.MyProject.Debugging;

public static class DebugHelper
{
    public static bool IsDebug
    {
        get
        {
#pragma warning disable
#if DEBUG
            return true;
#endif
            return false;
#pragma warning restore
        }
    }
}
