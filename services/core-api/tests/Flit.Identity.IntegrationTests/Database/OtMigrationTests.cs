using Flit.OT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Database;

public class OtMigrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public OtMigrationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Migration_creates_ot_schema_with_tenant_fk()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();

        var tables = await otDb.Database.SqlQueryRaw<TableRow>(
            """
            SELECT table_name AS "TableName"
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            """
        ).ToListAsync();

        var names = tables.Select(t => t.TableName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("ot_profiles", names);
        Assert.Contains("procedure_type_catalog", names);
        Assert.Contains("document_type_catalog", names);
        Assert.Contains("procedure_document_defaults", names);
        Assert.Contains("ot_document_order_items", names);
    }

    private sealed record TableRow(string TableName);
}
