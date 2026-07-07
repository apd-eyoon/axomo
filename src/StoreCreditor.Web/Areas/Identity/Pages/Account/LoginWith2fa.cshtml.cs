using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StoreCreditor.Data.Entities;
using StoreCreditor.Web.Options;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class LoginWith2faModel(
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<StoreCreditorAuthenticationOptions> authenticationOptions) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool RememberMe { get; set; }

    [TempData]
    public string? DevelopmentTwoFactorCode { get; set; }

    [TempData]
    public string? TwoFactorDeliveryWarning { get; set; }

    public IActionResult OnGet()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl, Url);
        if (!authenticationOptions.CurrentValue.UseOtp)
        {
            return LocalRedirect(ReturnUrl);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl, Url);
        if (!authenticationOptions.CurrentValue.UseOtp)
        {
            return LocalRedirect(ReturnUrl);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, Input.Code, RememberMe, rememberClient: false);
        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid authentication code.");
        return Page();
    }

    private static string NormalizeReturnUrl(string? returnUrl, IUrlHelper urlHelper) =>
        string.IsNullOrWhiteSpace(returnUrl) ? urlHelper.Content("~/") : returnUrl;

    public sealed class InputModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
