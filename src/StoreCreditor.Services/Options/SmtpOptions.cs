namespace StoreCreditor.Services.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "SMTP";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public int ConnectTimeoutSeconds { get; set; }

    public bool AllowInvalidServerCertificate { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = "no-reply@aimpointdigital.com";

    public string FromName { get; set; } = "StoreCreditor";
}
