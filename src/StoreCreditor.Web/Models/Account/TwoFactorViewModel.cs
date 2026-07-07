using System.ComponentModel.DataAnnotations;

namespace StoreCreditor.Web.Models.Account;

public sealed class TwoFactorViewModel
{
    [Required]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
