using Abp.Web.Security.AntiForgery;
using LeHoangMy_1381.MyProject.Controllers;
using Microsoft.AspNetCore.Antiforgery;

namespace LeHoangMy_1381.MyProject.Web.Host.Controllers
{
    public class AntiForgeryController : MyProjectControllerBase
    {
        private readonly IAntiforgery _antiforgery;
        private readonly IAbpAntiForgeryManager _antiForgeryManager;

        public AntiForgeryController(IAntiforgery antiforgery, IAbpAntiForgeryManager antiForgeryManager)
        {
            _antiforgery = antiforgery;
            _antiForgeryManager = antiForgeryManager;
        }

        public void GetToken()
        {
            _antiforgery.SetCookieTokenAndHeader(HttpContext);
        }

        public void SetCookie()
        {
            _antiForgeryManager.SetCookie(HttpContext);
        }
    }
}
