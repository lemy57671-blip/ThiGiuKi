using System.ComponentModel.DataAnnotations;

namespace LeHoangMy_1381.MyProject.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}