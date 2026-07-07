using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using StoreCreditor.Data.Entities;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Web.Models.Account;
using StoreCreditor.Web.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;

namespace StoreCreditor.Web.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IOptionsMonitor<StoreCreditorAuthenticationOptions> authenticationOptions,
    IEmailService emailService,
    IWebHostEnvironment environment,
    ILogger<AccountController> logger) : Controller
{
    private const string AllowedDomain = "@aimpointdigital.com";

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!IsAllowedEmail(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Only aimpointdigital.com email addresses can register.");
            return View(model);
        }

        var isFirstUser = !await userManager.Users.AnyAsync(cancellationToken);
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            TwoFactorEnabled = authenticationOptions.CurrentValue.UseOtp
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        var role = isFirstUser ? "Admin" : "Operator";
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        await userManager.AddToRoleAsync(user, role);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var callbackUrl = Url.ActionLink(nameof(ConfirmEmail), "Account", new { userId = user.Id, token = encodedToken });
        await emailService.SendEmailAsync(user.Email!, "Confirm your StoreCreditor account", $"Confirm your account: <a href=\"{callbackUrl}\">confirm email</a>", cancellationToken);

        if (environment.IsDevelopment())
        {
            ViewBag.ConfirmationLink = callbackUrl;
        }

        logger.LogInformation("Registered StoreCreditor user {Email}.", model.Email);
        return View("RegisterConfirmation");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        return View(result.Succeeded ? "ConfirmEmail" : "AccessDenied");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!IsAllowedEmail(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Only aimpointdigital.com email addresses can log in.");
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var useOtp = authenticationOptions.CurrentValue.UseOtp;
        var result = useOtp
            ? await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true)
            : await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (result.RequiresTwoFactor)
        {
            var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            await emailService.SendEmailAsync(user.Email!, "StoreCreditor sign-in code", $"Your sign-in code is <strong>{code}</strong>.", HttpContext.RequestAborted);
            TempData["RememberMe"] = model.RememberMe;
            return RedirectToAction(nameof(LoginWith2fa), new { returnUrl });
        }

        if (result.Succeeded)
        {
            if (!useOtp)
            {
                await signInManager.SignInAsync(user, model.RememberMe);
            }

            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult LoginWith2fa(string? returnUrl = null)
    {
        if (!authenticationOptions.CurrentValue.UseOtp)
        {
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new TwoFactorViewModel { RememberMe = TempData["RememberMe"] as bool? ?? false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(TwoFactorViewModel model, string? returnUrl = null)
    {
        if (!authenticationOptions.CurrentValue.UseOtp)
        {
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, model.Code, model.RememberMe, rememberClient: false);
        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        ModelState.AddModelError(string.Empty, "Invalid authentication code.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is { EmailConfirmed: true })
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.ActionLink(nameof(ResetPassword), "Account", new { email = user.Email, token = encodedToken });
            await emailService.SendEmailAsync(user.Email!, "Reset your StoreCreditor password", $"Reset your password: <a href=\"{callbackUrl}\">reset password</a>", cancellationToken);
            if (environment.IsDevelopment())
            {
                ViewBag.ResetLink = callbackUrl;
            }
        }

        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token) =>
        View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result = await userManager.ResetPasswordAsync(user, decodedToken, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Login));
        }

        AddErrors(result);
        return View(model);
    }

    public IActionResult AccessDenied() => View();

    private static bool IsAllowedEmail(string email) =>
        email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase);

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
