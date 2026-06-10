namespace Flit.Identity.Notifications;

public interface IEmailSender
{
    Task SendInvitationAsync(string email, string activationLink, CancellationToken ct = default);
}
