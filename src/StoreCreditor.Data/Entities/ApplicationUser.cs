using Microsoft.AspNetCore.Identity;

namespace StoreCreditor.Data.Entities;

/// <summary>
/// Application user for StoreCreditor administrative access.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
