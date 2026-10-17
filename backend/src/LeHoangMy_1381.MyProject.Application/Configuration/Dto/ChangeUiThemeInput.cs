using System.ComponentModel.DataAnnotations;

namespace LeHoangMy_1381.MyProject.Configuration.Dto;

public class ChangeUiThemeInput
{
    [Required]
    [StringLength(32)]
    public string Theme { get; set; }
}
