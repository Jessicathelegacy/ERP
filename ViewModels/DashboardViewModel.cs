using Payroll.Models;

namespace Payroll.ViewModels;

public class DashboardViewModel
{
    public int ActiveEmployees { get; set; }
    public int TotalPaymentSchemes { get; set; }
    public decimal CurrentMonthNetPayout { get; set; }
    public string CurrentMonthName { get; set; } = "";
    public int PendingBatches { get; set; }
    public int DraftPayrolls { get; set; }
    public string? LatestBatchPeriod { get; set; }
    public PayrollBatchStatus? LatestBatchStatus { get; set; }
    public int SubmittedBatches { get; set; }

    public HashSet<string> GrantedModules { get; set; } = [];

    public bool Can(string module) => GrantedModules.Contains(module);
}
