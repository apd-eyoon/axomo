using StoreCreditor.Data.Entities;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Web.Models.Dashboard;

public sealed class DashboardViewModel
{
    public int PendingEmployees { get; set; }

    public int ProcessedEmployees { get; set; }

    public int SuccessfulCredits { get; set; }

    public FeatureFlagOptions FeatureFlags { get; set; } = new();

    public IReadOnlyList<EmployeeStaging> RecentEmployees { get; set; } = [];

    public IReadOnlyList<EmployeeCreditHistory> RecentCredits { get; set; } = [];
}
