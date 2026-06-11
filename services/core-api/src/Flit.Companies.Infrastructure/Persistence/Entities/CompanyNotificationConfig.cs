using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Infrastructure.Persistence.Entities;

public class CompanyNotificationConfig
{
    public Guid CompanyId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.FlitSmtp;
    public NotificationTarget NotificationTarget { get; set; } = NotificationTarget.Filer;
    public string? ClientApiSettings { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public Company? Company { get; set; }
}
