using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StoreCreditor.Data.Entities;
using StoreCreditor.Web.Options;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class TwoFactorAuthenticationModel(
    UserManager<ApplicationUser> userManager,
    IOptionsMonitor<StoreCreditorAuthenticationOptions> authenticationOptions) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsTwoFactorEnabled { get; private set; }

    public bool IsOtpEnabledForApp { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        IsOtpEnabledForApp = authenticationOptions.CurrentValue.UseOtp;
        IsTwoFactorEnabled = IsOtpEnabledForApp && await userManager.GetTwoFactorEnabledAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        if (!authenticationOptions.CurrentValue.UseOtp)
        {
            StatusMessage = "Email OTP is disabled in appsettings.json.";
            return RedirectToPage();
        }

        var enabled = await userManager.GetTwoFactorEnabledAsync(user);
        await userManager.SetTwoFactorEnabledAsync(user, !enabled);
        StatusMessage = !enabled ? "Email two-factor authentication enabled." : "Email two-factor authentication disabled.";
        return RedirectToPage();
    }
}
