namespace Payroll.Models;

public static class Modules
{
    public const string Dashboard             = "Dashboard";
    public const string Employees             = "Employees";
    public const string PaymentSchemes        = "PaymentSchemes";
    public const string Payroll               = "Payroll";
    public const string BatchPayroll          = "BatchPayroll";
    public const string BatchPayrollApproval  = "BatchPayrollApproval";
    public const string Payslip               = "Payslip";
    public const string AdminUsers            = "AdminUsers";
    public const string Roles                 = "Roles";

    public static readonly IReadOnlyList<(string Key, string DisplayName, string Icon)> All =
    [
        (Dashboard,            "Dashboard",              "bi-speedometer2"),
        (Employees,            "Employees",              "bi-people-fill"),
        (PaymentSchemes,       "Payment Schemes",        "bi-cash-stack"),
        (Payroll,              "Payroll",                "bi-receipt-cutoff"),
        (BatchPayroll,         "Batch Payroll",          "bi-layers-fill"),
        (BatchPayrollApproval, "Batch Payroll Approval", "bi-patch-check-fill"),
        (Payslip,              "Payslip",                "bi-printer-fill"),
        (AdminUsers,           "Admin Users",            "bi-shield-lock-fill"),
        (Roles,                "Roles",                  "bi-tags-fill"),
    ];
}
