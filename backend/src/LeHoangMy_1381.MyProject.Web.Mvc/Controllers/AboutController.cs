using Abp.AspNetCore.Mvc.Authorization;
using LeHoangMy_1381.MyProject.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace LeHoangMy_1381.MyProject.Web.Controllers;

[AbpMvcAuthorize]
public class AboutController : MyProjectControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}
