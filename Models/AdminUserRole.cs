namespace Payroll.Models;

public class AdminUserRole
{
    public int AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
