namespace Payroll.Models;

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string Module { get; set; } = string.Empty;
}
