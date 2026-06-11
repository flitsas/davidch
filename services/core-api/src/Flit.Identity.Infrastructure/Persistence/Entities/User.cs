using Flit.Identity.Shared.Domain;

namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Email { get; set; } = "";
    public string? PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public int TokenVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
