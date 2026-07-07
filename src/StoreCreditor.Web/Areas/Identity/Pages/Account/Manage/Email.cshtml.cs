using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using StoreCreditor.Data.Entities;
using StoreCreditor.Services.Interfaces;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class EmailModel(UserManager<ApplicationUser> userManager, IEmailService emailService) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public string Email { get; private set; } = string.Empty;

    public bool IsEmailConfirmed { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callbackUrl = Url.PageLink("/Account/ConfirmEmail", "Identity", new { userId = user.Id, token = encodedToken });
        await emailService.SendEmailAsync(user.Email!, "Confirm your StoreCreditor account", $"Confirm your account: <a href=\"{callbackUrl}\">confirm email</a>", cancellationToken);
        StatusMessage = "Confirmation email sent.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Email = user.Email ?? string.Empty;
        IsEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);
    }
}
