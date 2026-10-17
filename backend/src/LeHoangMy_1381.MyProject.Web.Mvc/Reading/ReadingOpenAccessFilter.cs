using Abp.Dependency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Web.Reading;

public class ReadingOpenAccessFilter : IAsyncAuthorizationFilter, IOrderedFilter, ITransientDependency
{
    private readonly ReadingOpenAccess _access;
    public ReadingOpenAccessFilter(ReadingOpenAccess access) { _access = access; }
    public int Order => int.MinValue;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        var path = request.Path;
        var isTeacherPage = path == "/" || path.StartsWithSegments("/Reading");
        if (_access.Enabled && HttpMethods.IsGet(request.Method) && isTeacherPage &&
            !context.HttpContext.User.HasClaim(ReadingOpenAccess.TeacherClaim, "true"))
        {
            var returnUrl = request.PathBase + request.Path + request.QueryString;
            context.Result = new RedirectResult("/ReadingAccess/Teacher?returnUrl=" + Uri.EscapeDataString(returnUrl));
        }
        return Task.CompletedTask;
    }
}
