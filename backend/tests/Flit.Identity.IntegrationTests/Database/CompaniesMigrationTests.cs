using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Shared.Domain;
using Flit.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Database;

public class CompaniesMigrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public CompaniesMigrationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Migration_creates_companies_schema_with_identity_fk()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();

        var tableNames = await GetPublicTableNamesAsync(companiesDb);

        Assert.Contains("companies", tableNames);
        Assert.Contains("company_matricula_config", tableNames);
        Assert.Contains("company_traspaso_config", tableNames);
        Assert.Contains("tenant_user_exceptions", tableNames);
        Assert.Contains("company_signature_config", tableNames);
        Assert.Contains("company_notification_config", tableNames);
        Assert.Contains("company_payment_config", tableNames);
        Assert.Contains("company_runt_config", tableNames);
        Assert.Contains("traffic_authorities", tableNames);
        Assert.Contains("company_traffic_authority_matrix", tableNames);
        Assert.Contains("runt_provider_catalog", tableNames);

        var fkToTenants = await companiesDb.Database.SqlQueryRaw<FkRow>(
            """
            SELECT tc.constraint_name AS "ConstraintName"
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
              ON tc.constraint_name = kcu.constraint_name
             AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
              ON ccu.constraint_name = tc.constraint_name
             AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND tc.table_name = 'companies'
              AND kcu.column_name = 'TenantId'
              AND ccu.table_name = 'Tenants'
            """
        ).ToListAsync();

        Assert.NotEmpty(fkToTenants);
    }

    [Fact]
    public async Task Seed_creates_runt_providers_and_traffic_authorities()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();

        var providers = await companiesDb.RuntProviderCatalog.AsNoTracking().ToListAsync();
        Assert.Contains(providers, p => p.Id == RuntProvider.Verifik && p.IsActive);
        Assert.Contains(providers, p => p.Id == RuntProvider.Intempo && p.IsActive);

        var authorities = await companiesDb.TrafficAuthorities.AsNoTracking().ToListAsync();
        Assert.True(authorities.Count >= 6);
        Assert.All(authorities, a => Assert.False(string.IsNullOrWhiteSpace(a.Code)));

        var nitIndex = await companiesDb.Database.SqlQueryRaw<IndexRow>(
            """
            SELECT indexname AS "IndexName"
            FROM pg_indexes
            WHERE tablename = 'companies' AND indexname = 'IX_companies_Nit'
            """
        ).ToListAsync();
        Assert.NotEmpty(nitIndex);

        var tenantIndex = await companiesDb.Database.SqlQueryRaw<IndexRow>(
            """
            SELECT indexname AS "IndexName"
            FROM pg_indexes
            WHERE tablename = 'companies' AND indexname = 'IX_companies_TenantId'
            """
        ).ToListAsync();
        Assert.NotEmpty(tenantIndex);
    }

    [Fact]
    public async Task Migration_down_removes_companies_tables_without_affecting_identity()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var identityTenantCountBefore = await identityDb.Tenants.CountAsync();

        var migrator = companiesDb.Database.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("0");

            var tableNames = await GetPublicTableNamesAsync(companiesDb);
            Assert.DoesNotContain("companies", tableNames);
            Assert.DoesNotContain("traffic_authorities", tableNames);
            Assert.Contains("Tenants", tableNames);
            Assert.Contains("Users", tableNames);

            var identityTenantCountAfter = await identityDb.Tenants.CountAsync();
            Assert.Equal(identityTenantCountBefore, identityTenantCountAfter);
        }
        finally
        {
            await migrator.MigrateAsync();
        }
    }

    private static async Task<HashSet<string>> GetPublicTableNamesAsync(DbContext db)
    {
        var names = await db.Database.SqlQueryRaw<TableRow>(
            """
            SELECT table_name AS "TableName"
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            """
        ).ToListAsync();

        return names.Select(n => n.TableName).ToHashSet(StringComparer.Ordinal);
    }

    private sealed record TableRow(string TableName);
    private sealed record FkRow(string ConstraintName);
    private sealed record IndexRow(string IndexName);
}
