using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreCreditor.Data.Repositories;

namespace StoreCreditor.Web.Controllers;

[Authorize]
public sealed class AuditLogsController(IAuditLogRepository auditLogRepository) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var logs = await auditLogRepository.GetRecentAsync(200, cancellationToken);
        return View(logs);
    }
}
