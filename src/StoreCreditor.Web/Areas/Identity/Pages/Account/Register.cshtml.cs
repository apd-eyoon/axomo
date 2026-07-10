using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoreCreditor.Data.Entities;
using StoreCreditor.Web.Options;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account;

public sealed class RegisterModel(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptionsMonitor<StoreCreditorAuthenticationOptions> authenticationOptions,
    ILogger<RegisterModel> logger) : PageModel
{
    private const string AllowedDomain = "@aimpointdigital.com";

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

        if (!Input.Email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(Input.Email), "Only aimpointdigital.com email addresses can register.");
            return Page();
        }

        var isFirstUser = !await userManager.Users.AnyAsync(cancellationToken);
        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,
            DisplayName = Input.DisplayName,
            TwoFactorEnabled = authenticationOptions.CurrentValue.UseOtp
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        var role = isFirstUser ? "Admin" : "Operator";
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        await userManager.AddToRoleAsync(user, role);

        logger.LogInformation("Registered StoreCreditor user {Email}.", Input.Email);
        return RedirectToPage("./RegisterConfirmation", new { email = Input.Email });
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
