using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class ConfirmEmailModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public string StatusTitle { get; private set; } = "Email confirmed";

    public string StatusMessage { get; private set; } = "Your account is ready.";

    public async Task<IActionResult> OnGetAsync(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            StatusTitle = "Email confirmation failed";
            StatusMessage = "The confirmation link is invalid or expired.";
        }

        return Page();
    }
}
