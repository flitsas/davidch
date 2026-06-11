using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Seed;
using Flit.OT.Infrastructure.Persistence.Entities;
using Flit.OT.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Infrastructure.Persistence.Seed;

/// <summary>
/// Links the dev tenant-a to an OT profile for local Docker / E2E flows.
/// </summary>
public static class DevOtSeeder
{
    public static async Task SeedAsync(
        OtDbContext otDb,
        IdentityDbContext identityDb,
        CancellationToken ct = default)
    {
        if (await otDb.OtProfiles.AnyAsync(ct))
        {
            return;
        }

        var tenant = await identityDb.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Slug == DevTenantSeeder.TenantSlug, ct);

        if (tenant is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        otDb.OtProfiles.Add(new OtProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            DivipolCode = "11001000",
            DisplayName = "OT Demo Tenant A",
            Status = OtStatus.Active,
            IntegrationMode = IntegrationMode.Dashboard,
            CreatedAt = now,
            UpdatedAt = now
        });

        await OtDefaultOrderFactory.SeedOrderItemsAsync(otDb, tenant.Id, now, ct);
        await otDb.SaveChangesAsync(ct);
    }
}
