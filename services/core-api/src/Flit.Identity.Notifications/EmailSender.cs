using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Flit.Identity.Notifications;

public sealed class EmailSender(IConfiguration config) : IEmailSender
{
    public Task SendInvitationAsync(string email, string activationLink, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(GetFromAddress());
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "You've been invited to FLIT";
        message.Body = new Multipart("alternative")
        {
            new TextPart("plain") { Text = $"You've been invited to FLIT. Activate your account: {activationLink}" },
            new TextPart("html")
            {
                Text = $"""
                    <p>You've been invited to FLIT.</p>
                    <p><a href="{activationLink}">Activate your account</a></p>
                    """
            }
        };

        return SendAsync(message, ct);
    }

    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(GetFromAddress());
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Reset your FLIT password";
        message.Body = new Multipart("alternative")
        {
            new TextPart("plain") { Text = $"Reset your FLIT password: {resetLink}" },
            new TextPart("html")
            {
                Text = $"""
                    <p>Reset your FLIT password.</p>
                    <p><a href="{resetLink}">Set a new password</a></p>
                    """
            }
        };

        return SendAsync(message, ct);
    }

    private MailboxAddress GetFromAddress()
    {
        var email = config["Smtp:From"] ?? "identity@flit.local";
        var name = config["Smtp:FromName"];
        return string.IsNullOrWhiteSpace(name)
            ? MailboxAddress.Parse(email)
            : new MailboxAddress(name, email);
    }

    private async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        var host = config["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(config["Smtp:Port"], out var parsedPort) ? parsedPort : 1025;

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, ResolveSocketOptions(port), ct);

        var user = config["Smtp:User"];
        var password = config["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
            await client.AuthenticateAsync(user, password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    private SecureSocketOptions ResolveSocketOptions(int port)
    {
        if (bool.TryParse(config["Smtp:UseSsl"], out var useSsl) && useSsl)
            return SecureSocketOptions.SslOnConnect;

        return port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None,
        };
    }
}
