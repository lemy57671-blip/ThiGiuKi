using Abp.Authorization;
using Abp.Domain.Uow;
using LeHoangMy_1381.MyProject.Authorization.Roles;
using LeHoangMy_1381.MyProject.Authorization.Users;
using LeHoangMy_1381.MyProject.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeHoangMy_1381.MyProject.Identity;

public class SecurityStampValidator : AbpSecurityStampValidator<Tenant, Role, User>
{
    public SecurityStampValidator(
        IOptions<SecurityStampValidatorOptions> options,
        SignInManager signInManager,
        ILoggerFactory loggerFactory,
        IUnitOfWorkManager unitOfWorkManager)
        : base(options, signInManager, loggerFactory, unitOfWorkManager)
    {
    }
}
