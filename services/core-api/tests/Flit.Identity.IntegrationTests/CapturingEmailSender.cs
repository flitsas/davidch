using System.Collections.Concurrent;
using Flit.Identity.Notifications;

namespace Flit.Identity.IntegrationTests;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _invitationLinksByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _resetLinksByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task SendInvitationAsync(string email, string activationLink, CancellationToken ct = default)
    {
        _invitationLinksByEmail[email] = activationLink;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken ct = default)
    {
        _resetLinksByEmail[email] = resetLink;
        return Task.CompletedTask;
    }

    public string? GetLatestInvitationLink(string email) =>
        _invitationLinksByEmail.TryGetValue(email, out var link) ? link : null;

    public string? GetLatestResetLink(string email) =>
        _resetLinksByEmail.TryGetValue(email, out var link) ? link : null;

    public string? ExtractToken(string email) =>
        ExtractTokenFromLink(GetLatestInvitationLink(email));

    public string? ExtractResetToken(string email) =>
        ExtractTokenFromLink(GetLatestResetLink(email));

    private static string? ExtractTokenFromLink(string? link)
    {
        if (link is null)
        {
            return null;
        }

        var query = link.Split('?', 2);
        if (query.Length < 2)
        {
            return null;
        }

        foreach (var part in query[1].Split('&'))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] == "token")
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }
}
