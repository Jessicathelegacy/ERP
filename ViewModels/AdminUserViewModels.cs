using System.ComponentModel.DataAnnotations;

namespace Payroll.ViewModels;

public class AdminUserCreateViewModel
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> SelectedRoleIds { get; set; } = [];
}

public class AdminUserEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<int> SelectedRoleIds { get; set; } = [];
}

public class ChangePasswordViewModel
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
