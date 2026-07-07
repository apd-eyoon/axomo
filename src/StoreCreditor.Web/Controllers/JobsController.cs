using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreCreditor.Services.Jobs;

namespace StoreCreditor.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class JobsController(IBackgroundJobClient backgroundJobs) : Controller
{
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RunEmployeeImport()
    {
        backgroundJobs.Enqueue<EmployeeImportJob>(job => job.RunAsync(CancellationToken.None));
        TempData["Message"] = "Employee import job queued.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RunStoreCredit()
    {
        backgroundJobs.Enqueue<StoreCreditJob>(job => job.RunAsync(CancellationToken.None));
        TempData["Message"] = "Store credit job queued.";
        return RedirectToAction(nameof(Index));
    }
}
