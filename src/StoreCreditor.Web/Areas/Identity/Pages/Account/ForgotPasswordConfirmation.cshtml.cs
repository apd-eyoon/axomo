using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class ForgotPasswordConfirmationModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ResetLink { get; set; }

    public void OnGet()
    {
    }
}
