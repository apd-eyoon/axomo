using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StoreCreditor.Data.Repositories;
using StoreCreditor.Services.Options;
using StoreCreditor.Web.Models.Dashboard;
using StoreCreditor.Web.Models;

namespace StoreCreditor.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public async Task<IActionResult> Index(
        [FromServices] IEmployeeRepository employeeRepository,
        [FromServices] IStoreCreditRepository storeCreditRepository,
        [FromServices] IOptionsMonitor<FeatureFlagOptions> featureFlags,
        CancellationToken cancellationToken)
    {
        var model = new DashboardViewModel
        {
            PendingEmployees = await employeeRepository.CountPendingAsync(cancellationToken),
            ProcessedEmployees = await employeeRepository.CountProcessedAsync(cancellationToken),
            SuccessfulCredits = await storeCreditRepository.CountSuccessfulAsync(cancellationToken),
            RecentEmployees = await employeeRepository.GetRecentAsync(10, cancellationToken),
            RecentCredits = await storeCreditRepository.GetRecentAsync(10, cancellationToken),
            FeatureFlags = featureFlags.CurrentValue
        };

        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
