using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StoreCreditor.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ConfigStatusController(IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        var model = new Dictionary<string, bool>
        {
            ["DefaultConnection"] = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")),
            ["SMTP:Host"] = !string.IsNullOrWhiteSpace(configuration["SMTP:Host"]),
            ["BambooHR:BaseUrl"] = !string.IsNullOrWhiteSpace(configuration["BambooHR:BaseUrl"]),
            ["BambooHR:ApiKey"] = !string.IsNullOrWhiteSpace(configuration["BambooHR:ApiKey"]),
            ["Axomo:BaseUrl"] = !string.IsNullOrWhiteSpace(configuration["Axomo:BaseUrl"]),
            ["Axomo:Credentials"] = !string.IsNullOrWhiteSpace(configuration["Axomo:ApiKey"]) || !string.IsNullOrWhiteSpace(configuration["Axomo:BearerToken"])
        };

        return View(model);
    }
}
