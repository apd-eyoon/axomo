using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreCreditor.Data.Repositories;

namespace StoreCreditor.Web.Controllers;

[Authorize]
public sealed class EmployeeQueueController(IEmployeeRepository employeeRepository) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var employees = await employeeRepository.GetRecentAsync(100, cancellationToken);
        return View(employees);
    }
}
