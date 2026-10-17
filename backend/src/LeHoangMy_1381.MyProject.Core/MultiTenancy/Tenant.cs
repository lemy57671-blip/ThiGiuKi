using Abp.MultiTenancy;
using LeHoangMy_1381.MyProject.Authorization.Users;

namespace LeHoangMy_1381.MyProject.MultiTenancy;

public class Tenant : AbpTenant<User>
{
    public Tenant()
    {
    }

    public Tenant(string tenancyName, string name)
        : base(tenancyName, name)
    {
    }
}
