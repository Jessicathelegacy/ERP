using System.ComponentModel.DataAnnotations;

namespace Payroll.Models;

public class Role
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<AdminUserRole> AdminUserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
