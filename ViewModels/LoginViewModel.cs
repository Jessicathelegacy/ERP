using System.ComponentModel.DataAnnotations;

namespace Payroll.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
