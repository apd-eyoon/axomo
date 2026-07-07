using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        Input = new InputModel { Email = user.Email ?? string.Empty, DisplayName = user.DisplayName ?? string.Empty };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Input.Email = user.Email ?? string.Empty;
            return Page();
        }

        user.DisplayName = Input.DisplayName;
        await userManager.UpdateAsync(user);
        StatusMessage = "Profile updated.";
        return RedirectToPage();
    }

    public sealed class InputModel
    {
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }
    }
}
