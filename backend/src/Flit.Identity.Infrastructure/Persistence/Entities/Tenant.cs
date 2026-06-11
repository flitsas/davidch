namespace Flit.Identity.Infrastructure.Persistence.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
