using System.ComponentModel.DataAnnotations;

namespace Payroll.ViewModels;

public class RoleFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> GrantedModules { get; set; } = [];
}

public class RoleListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
}
