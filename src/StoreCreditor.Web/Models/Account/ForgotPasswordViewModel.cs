using System.ComponentModel.DataAnnotations;

namespace StoreCreditor.Web.Models.Account;

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
