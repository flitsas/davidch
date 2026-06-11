using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Flit.Identity.Notifications;

public sealed class EmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendInvitationAsync(string email, string activationLink, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(config["Smtp:From"] ?? "identity@flit.local"));
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

        using var client = new SmtpClient();
        var port = int.TryParse(config["Smtp:Port"], out var parsedPort) ? parsedPort : 1025;
        var useSsl = bool.TryParse(config["Smtp:UseSsl"], out var parsedSsl) && parsedSsl;
        await client.ConnectAsync(config["Smtp:Host"] ?? "localhost", port, useSsl, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(config["Smtp:From"] ?? "identity@flit.local"));
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

        using var client = new SmtpClient();
        var port = int.TryParse(config["Smtp:Port"], out var parsedPort) ? parsedPort : 1025;
        var useSsl = bool.TryParse(config["Smtp:UseSsl"], out var parsedSsl) && parsedSsl;
        await client.ConnectAsync(config["Smtp:Host"] ?? "localhost", port, useSsl, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
