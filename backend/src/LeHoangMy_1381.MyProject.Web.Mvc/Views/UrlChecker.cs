using System.Text.RegularExpressions;

namespace LeHoangMy_1381.MyProject.Web.Views;

public static class UrlChecker
{
    private static readonly Regex UrlWithProtocolRegex = new Regex("^.{1,10}://.*$");

    public static bool IsRooted(string url)
    {
        if (url.StartsWith("/"))
        {
            return true;
        }

        if (UrlWithProtocolRegex.IsMatch(url))
        {
            return true;
        }

        return false;
    }
}
