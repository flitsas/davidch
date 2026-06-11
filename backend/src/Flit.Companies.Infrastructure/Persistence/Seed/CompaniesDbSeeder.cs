using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Companies.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Infrastructure.Persistence.Seed;

public static class CompaniesDbSeeder
{
    public static async Task SeedAsync(CompaniesDbContext db, CancellationToken ct = default)
    {
        await SeedRuntProvidersAsync(db, ct);
        await SeedTrafficAuthoritiesAsync(db, ct);
    }

    private static async Task SeedRuntProvidersAsync(CompaniesDbContext db, CancellationToken ct)
    {
        if (await db.RuntProviderCatalog.AnyAsync(ct))
        {
            return;
        }

        db.RuntProviderCatalog.AddRange(
            new RuntProviderCatalog { Id = RuntProvider.Verifik, Name = "Verifik", IsActive = true },
            new RuntProviderCatalog { Id = RuntProvider.Intempo, Name = "Intempo", IsActive = true });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedTrafficAuthoritiesAsync(CompaniesDbContext db, CancellationToken ct)
    {
        if (await db.TrafficAuthorities.AnyAsync(ct))
        {
            return;
        }

        var authorities = new[]
        {
            new TrafficAuthority { Code = "11001000", Name = "Secretaría Distrital de Movilidad - Bogotá", Region = "Cundinamarca" },
            new TrafficAuthority { Code = "05001000", Name = "Secretaría de Movilidad - Medellín", Region = "Antioquia" },
            new TrafficAuthority { Code = "76001000", Name = "STTMP Cali", Region = "Valle del Cauca" },
            new TrafficAuthority { Code = "08001000", Name = "ATM Barranquilla", Region = "Atlántico" },
            new TrafficAuthority { Code = "13001000", Name = "Oficina Tránsito Cartagena", Region = "Bolívar" },
            new TrafficAuthority { Code = "68001000", Name = "SETP Bucaramanga", Region = "Santander" }
        };

        db.TrafficAuthorities.AddRange(authorities);
        await db.SaveChangesAsync(ct);
    }
}
