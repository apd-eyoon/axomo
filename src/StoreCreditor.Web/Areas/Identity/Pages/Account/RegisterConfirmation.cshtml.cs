using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class RegisterConfirmationModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ConfirmationLink { get; set; }

    public void OnGet()
    {
    }
}
