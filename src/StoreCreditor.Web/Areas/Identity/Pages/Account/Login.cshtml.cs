using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StoreCreditor.Data.Entities;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Web.Options;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptionsMonitor<StoreCreditorAuthenticationOptions> authenticationOptions,
    IEmailService emailService,
    IWebHostEnvironment environment,
    ILogger<LoginModel> logger) : PageModel
{
    private const string AllowedDomain = "@aimpointdigital.com";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IReadOnlyList<AuthenticationScheme> ExternalLogins { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl, Url);
        ExternalLogins = await LoadExternalLoginSchemesAsync();
        if (Request.Query.TryGetValue("externalError", out var externalError))
        {
            ModelState.AddModelError(string.Empty, externalError.ToString());
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ReturnUrl = NormalizeReturnUrl(ReturnUrl, Url);

        if (!ModelState.IsValid)
        {
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        //if (!IsAllowedEmail(Input.Email))
        //{
        //    ModelState.AddModelError(nameof(Input.Email), "Only aimpointdigital.com email addresses can log in.");
        //    return Page();
        //}

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        var useOtp = authenticationOptions.CurrentValue.UseOtp;
        var result = useOtp
            ? await signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true)
            : await signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);
        if (result.RequiresTwoFactor)
        {
            var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            try
            {
                await emailService.SendEmailAsync(user.Email!, "StoreCreditor sign-in code", $"Your sign-in code is <strong>{code}</strong>.", cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to send two-factor code to {Email}.", user.Email);
                if (environment.IsDevelopment())
                {
                    TempData["DevelopmentTwoFactorCode"] = code;
                    TempData["TwoFactorDeliveryWarning"] = "The sign-in code email could not be sent. Use the code shown below for this local sign-in.";
                }
            }

            return RedirectToPage("./LoginWith2fa", new { ReturnUrl, Input.RememberMe });
        }

        if (result.Succeeded)
        {
            if (!useOtp)
            {
                await signInManager.SignInAsync(user, Input.RememberMe);
            }

            logger.LogInformation("User {Email} logged in.", Input.Email);
            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Try again later.");
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        ExternalLogins = await LoadExternalLoginSchemesAsync();
        return Page();
    }

    public IActionResult OnPostExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./Login", pageHandler: "ExternalLoginCallback", values: new { ReturnUrl = returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetExternalLoginCallbackAsync(
        string? returnUrl = null,
        string? remoteError = null,
        CancellationToken cancellationToken = default)
    {
        ReturnUrl = NormalizeReturnUrl(returnUrl, Url);

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ModelState.AddModelError(string.Empty, $"Microsoft Entra ID sign-in failed: {remoteError}");
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ModelState.AddModelError(string.Empty, "Microsoft Entra ID sign-in information could not be loaded.");
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            logger.LogInformation("User logged in with {LoginProvider}.", info.LoginProvider);
            return LocalRedirect(ReturnUrl);
        }

        var email = GetClaim(info.Principal, ClaimTypes.Email, "email", "preferred_username", "upn");
        if (string.IsNullOrWhiteSpace(email) || !IsAllowedEmail(email))
        {
            ModelState.AddModelError(string.Empty, "Only aimpointdigital.com Microsoft Entra ID accounts can sign in.");
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var isFirstUser = !await userManager.Users.AnyAsync(cancellationToken);
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = GetClaim(info.Principal, ClaimTypes.Name, "name") ?? email
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                AddErrors(createResult);
                ExternalLogins = await LoadExternalLoginSchemesAsync();
                return Page();
            }

            var role = isFirstUser ? "Admin" : "Operator";
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            await userManager.AddToRoleAsync(user, role);
        }
        else if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        var loginResult = await userManager.AddLoginAsync(user, info);
        if (!loginResult.Succeeded)
        {
            AddErrors(loginResult);
            ExternalLogins = await LoadExternalLoginSchemesAsync();
            return Page();
        }

        await signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
        logger.LogInformation("User {Email} logged in with {LoginProvider}.", email, info.LoginProvider);
        return LocalRedirect(ReturnUrl);
    }

    private async Task<IReadOnlyList<AuthenticationScheme>> LoadExternalLoginSchemesAsync() =>
        (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

    private static string NormalizeReturnUrl(string? returnUrl, IUrlHelper urlHelper) =>
        string.IsNullOrWhiteSpace(returnUrl) ? urlHelper.Content("~/") : returnUrl;

    private static bool IsAllowedEmail(string email) =>
        email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase);

    private static string? GetClaim(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
