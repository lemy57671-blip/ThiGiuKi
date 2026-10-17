using System.ComponentModel.DataAnnotations;

namespace LeHoangMy_1381.MyProject.Users.Dto;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword { get; set; }
}
