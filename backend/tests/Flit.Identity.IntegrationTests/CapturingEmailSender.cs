using System.Collections.Concurrent;
using Flit.Identity.Notifications;

namespace Flit.Identity.IntegrationTests;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _linksByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task SendInvitationAsync(string email, string activationLink, CancellationToken ct = default)
    {
        _linksByEmail[email] = activationLink;
        return Task.CompletedTask;
    }

    public string? GetLatestLink(string email) =>
        _linksByEmail.TryGetValue(email, out var link) ? link : null;

    public string? ExtractToken(string email)
    {
        var link = GetLatestLink(email);
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
