using System.Net.Security;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using StoreCreditor.Services.Interfaces;
using StoreCreditor.Services.Options;

namespace StoreCreditor.Services;

public sealed class SmtpEmailService(IOptionsMonitor<SmtpOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(currentOptions.Host))
        {
            logger.LogWarning("SMTP host is not configured. Email to {Email} with subject {Subject} was skipped.", toEmail, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(currentOptions.FromName, currentOptions.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = (_, _, _, sslPolicyErrors) =>
            ValidateServerCertificate(sslPolicyErrors, currentOptions);

        var secureSocketOptions = ResolveSecureSocketOptions(currentOptions);
        client.Timeout = Math.Max(1, currentOptions.ConnectTimeoutSeconds) * 1000;

        logger.LogInformation(
            "Connecting to SMTP server {Host}:{Port} using {SecureSocketOptions}. Certificate validation bypass enabled: {AllowInvalidServerCertificate}.",
            currentOptions.Host,
            currentOptions.Port,
            secureSocketOptions,
            currentOptions.AllowInvalidServerCertificate);

        await client.ConnectAsync(currentOptions.Host, currentOptions.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(currentOptions.UserName))
        {
            await client.AuthenticateAsync(currentOptions.UserName, currentOptions.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static SecureSocketOptions ResolveSecureSocketOptions(SmtpOptions options)
    {
        if (!options.EnableSsl)
        {
            return SecureSocketOptions.None;
        }

        return options.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }

    private bool ValidateServerCertificate(SslPolicyErrors sslPolicyErrors, SmtpOptions options)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (options.AllowInvalidServerCertificate)
        {
            logger.LogWarning(
                "SMTP server certificate validation is disabled. Accepting certificate with errors: {SslPolicyErrors}.",
                sslPolicyErrors);
            return true;
        }

        logger.LogError("SMTP server certificate validation failed: {SslPolicyErrors}.", sslPolicyErrors);
        return false;
    }
}
