using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

[Authorize]
public sealed class LogoutModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is not null)
        {
            var logins = await userManager.GetLoginsAsync(user);
            if (logins.Any(login => login.LoginProvider == "MicrosoftEntraId"))
            {
                return SignOut(
                    new AuthenticationProperties { RedirectUri = Url.Content("~/Identity/Account/Login") },
                    IdentityConstants.ApplicationScheme,
                    IdentityConstants.ExternalScheme,
                    "MicrosoftEntraId");
            }
        }

        await signInManager.SignOutAsync();
        return LocalRedirect(Url.Content("~/Identity/Account/Login"));
    }
}
