using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using LeHoangMy_1381.MyProject.Authorization;
using LeHoangMy_1381.MyProject.Controllers;
using LeHoangMy_1381.MyProject.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Tenants)]
public class TenantsController : MyProjectControllerBase
{
    private readonly ITenantAppService _tenantAppService;

    public TenantsController(ITenantAppService tenantAppService)
    {
        _tenantAppService = tenantAppService;
    }

    public ActionResult Index() => View();

    public async Task<ActionResult> EditModal(int tenantId)
    {
        var tenantDto = await _tenantAppService.GetAsync(new EntityDto(tenantId));
        return PartialView("_EditModal", tenantDto);
    }
}
