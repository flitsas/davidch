using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Tramites;

public class TramitesMigrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public TramitesMigrationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Migration_creates_procedure_instance_tables()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProceduresDbContext>();

        var tables = await db.Database.SqlQueryRaw<TableRow>(
            """
            SELECT table_name AS "TableName"
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            """
        ).ToListAsync();

        var names = tables.Select(t => t.TableName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("procedure_instances", names);
        Assert.Contains("procedure_instance_actors", names);
        Assert.Contains("procedure_instance_documents", names);
    }

    private sealed record TableRow(string TableName);
}
