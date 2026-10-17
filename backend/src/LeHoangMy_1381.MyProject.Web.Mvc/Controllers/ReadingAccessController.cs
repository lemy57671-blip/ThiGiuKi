using Abp.Auditing;
using Abp.Authorization.Users;
using Abp.Domain.Uow;
using Abp.MultiTenancy;
using Abp.UI;
using LeHoangMy_1381.MyProject.Authentication.JwtBearer;
using LeHoangMy_1381.MyProject.Authorization.Users;
using LeHoangMy_1381.MyProject.Controllers;
using LeHoangMy_1381.MyProject.Identity;
using LeHoangMy_1381.MyProject.MultiTenancy;
using LeHoangMy_1381.MyProject.Web.Reading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace LeHoangMy_1381.MyProject.Web.Controllers;

[AllowAnonymous, DisableAuditing, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReadingAccessController : MyProjectControllerBase
{
    private static readonly SemaphoreSlim StudentProvisioning = new(1, 1);
    private readonly ReadingOpenAccess _access;
    private readonly TenantManager _tenants;
    private readonly UserManager _users;
    private readonly UserClaimsPrincipalFactory _claims;
    private readonly SignInManager _signIn;
    private readonly TokenAuthConfiguration _tokens;
    private readonly IUnitOfWorkManager _unitOfWork;

    public ReadingAccessController(ReadingOpenAccess access, TenantManager tenants, UserManager users,
        UserClaimsPrincipalFactory claims, SignInManager signIn, TokenAuthConfiguration tokens,
        IUnitOfWorkManager unitOfWork)
    {
        _access = access; _tenants = tenants; _users = users; _claims = claims;
        _signIn = signIn; _tokens = tokens; _unitOfWork = unitOfWork;
    }

    [HttpGet("/ReadingAccess/Teacher"), UnitOfWork]
    public async Task<IActionResult> Teacher(string returnUrl = "/Reading")
    {
        if (!_access.Enabled) return NotFound();
        var tenant = await GetDefaultTenantAsync();
        using (_unitOfWork.Current.SetTenantId(tenant.Id))
        using (AbpSession.Use(tenant.Id, null))
        {
            var user = await _users.FindByNameAsync(AbpUserBase.AdminUserName);
            if (user == null || !user.IsActive) throw new UserFriendlyException("Không có giảng viên đang hoạt động trong dữ liệu mẫu.");
            await _users.InitializeOptionsAsync(tenant.Id);
            var principal = await _claims.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity;
            identity.AddClaim(new Claim(ReadingOpenAccess.TeacherClaim, "true"));
            await _signIn.SignInAsync(identity, isPersistent: true);
        }
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/Reading");
    }

    [HttpPost("/api/ReadingAccess/Student"), IgnoreAntiforgeryToken, RequestSizeLimit(2048)]
    public async Task<IActionResult> Student([FromBody] ReadingVisitorInput input)
    {
        if (!_access.Enabled) return NotFound();
        if (input == null || !ModelState.IsValid) return BadRequest("Yêu cầu không hợp lệ.");
        var visitorKey = input.VisitorKey;
        if (string.IsNullOrEmpty(visitorKey)) visitorKey = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        if (!IsValidVisitorKey(visitorKey)) return BadRequest("Khóa người học không hợp lệ.");

        // The private random key stays on the browser. Only its digest is used as an account name.
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(visitorKey))).ToLowerInvariant();
        var userName = "reading.visitor." + digest;
        var tenantId = 0;
        long userId = 0;
        ClaimsIdentity identity;
        await StudentProvisioning.WaitAsync(HttpContext.RequestAborted);
        try
        {
            // Commit before releasing the lock so two tabs cannot create duplicate visitor accounts.
            using var work = _unitOfWork.Begin(TransactionScopeOption.RequiresNew);
            var tenant = await GetDefaultTenantAsync();
            tenantId = tenant.Id;
            using (_unitOfWork.Current.SetTenantId(tenant.Id))
            using (AbpSession.Use(tenant.Id, null))
            {
                await _users.InitializeOptionsAsync(tenant.Id);
                var user = await _users.FindByNameAsync(userName);
                if (user == null)
                {
                    user = new User
                    {
                        TenantId = tenant.Id, UserName = userName, Name = "Người", Surname = "học",
                        EmailAddress = digest + "@visitor.invalid", IsActive = true, IsEmailConfirmed = true,
                        Roles = new List<UserRole>()
                    };
                    user.SetNormalizedNames();
                    // No password is accepted from or returned to the application.
                    CheckErrors(await _users.CreateAsync(user, "Aa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12))));
                    await _unitOfWork.Current.SaveChangesAsync();
                }
                if (!user.IsActive) return StatusCode(403);
                userId = user.Id;
                identity = (ClaimsIdentity)(await _claims.CreateAsync(user)).Identity;
            }
            await work.CompleteAsync();
        }
        finally { StudentProvisioning.Release(); }

        var now = DateTime.UtcNow;
        var claims = identity.Claims.ToList();
        claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
        var token = new JwtSecurityToken(_tokens.Issuer, _tokens.Audience, claims, now, now.Add(_tokens.Expiration), _tokens.SigningCredentials);
        return Json(new
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpireInSeconds = (int)_tokens.Expiration.TotalSeconds, UserId = userId, TenantId = tenantId,
            VisitorKey = visitorKey, DisplayName = "Người học"
        });
    }

    public static bool IsValidVisitorKey(string key) => key != null && key.Length == 43 &&
        key.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');

    private async Task<Tenant> GetDefaultTenantAsync()
    {
        using (_unitOfWork.Current.SetTenantId(null))
        {
            var tenant = await _tenants.FindByTenancyNameAsync(AbpTenantBase.DefaultTenantName);
            if (tenant == null || !tenant.IsActive) throw new UserFriendlyException("Không tìm thấy dữ liệu mẫu Default đang hoạt động.");
            return tenant;
        }
    }
}

public class ReadingVisitorInput
{
    [DisableAuditing]
    public string VisitorKey { get; set; }
}
