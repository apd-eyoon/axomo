using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using StoreCreditor.Data.Entities;
using StoreCreditor.Services.Interfaces;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IWebHostEnvironment environment) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var resetLink = string.Empty;
        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is { EmailConfirmed: true })
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            resetLink = Url.PageLink("./ResetPassword", values: new { email = user.Email, token = encodedToken }) ?? string.Empty;
            try
            {
                await emailService.SendEmailAsync(user.Email!, "Reset your StoreCreditor password", $"Reset your password: <a href=\"{resetLink}\">reset password</a>", cancellationToken);
            }
            catch
            {
                // The reset link is still shown on the confirmation page for local recovery.
            }
        }

        return environment.IsDevelopment()
            ? RedirectToPage("./ForgotPasswordConfirmation", new { resetLink })
            : RedirectToPage("./ForgotPasswordConfirmation");
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
