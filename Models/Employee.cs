using System.ComponentModel.DataAnnotations;

namespace Payroll.Models;

public class Employee
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public int PaymentSchemeId { get; set; }
    public PaymentScheme PaymentScheme { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime JoinDate { get; set; } = DateTime.UtcNow;

    public ICollection<Payroll> Payrolls { get; set; } = [];
}
