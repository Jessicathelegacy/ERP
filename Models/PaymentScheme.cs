using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payroll.Models;

public class PaymentScheme
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasicSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeRatePerHour { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AllowanceAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = [];
}
