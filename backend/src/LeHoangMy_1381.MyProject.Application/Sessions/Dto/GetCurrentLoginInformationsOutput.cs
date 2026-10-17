namespace LeHoangMy_1381.MyProject.Sessions.Dto;

public class GetCurrentLoginInformationsOutput
{
    public ApplicationInfoDto Application { get; set; }

    public UserLoginInfoDto User { get; set; }

    public TenantLoginInfoDto Tenant { get; set; }
}
