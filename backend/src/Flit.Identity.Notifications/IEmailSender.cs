namespace Flit.Identity.Notifications;

public interface IEmailSender
{
    Task SendInvitationAsync(string email, string activationLink, CancellationToken ct = default);
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken ct = default);
}
