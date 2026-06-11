using Flit.Identity.IntegrationTests.Database;
using Flit.Procedures.Admin.Services;
using Flit.Procedures.Infrastructure.Persistence;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared;
using Flit.Procedures.Shared.Domain;
using Flit.OT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Procedures;

public class ProceduresMigrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public ProceduresMigrationTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Migration_creates_procedure_types_schema()
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
        Assert.Contains("procedure_types", names);
        Assert.Contains("procedure_type_actors", names);
        Assert.Contains("procedure_type_documents", names);
    }

    private sealed record TableRow(string TableName);
}

public class ProcedureDefinitionServiceTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public ProcedureDefinitionServiceTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SaveAsync_enforces_unique_name_and_syncs_ot_catalog()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ProcedureTypeWriter>();
        var service = scope.ServiceProvider.GetRequiredService<IProcedureDefinitionService>();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
        var proceduresDb = scope.ServiceProvider.GetRequiredService<ProceduresDbContext>();

        var entity = new ProcedureType
        {
            Name = "Cambio de color",
            Code = ProcedureCodeGenerator.FromName("Cambio de color"),
            VehicleQueryMode = VehicleQueryMode.Plate,
            IsActive = true,
        };

        await writer.SaveAsync(
            entity,
            [("Vendedor", 1), ("Comprador", 2)],
            [("Escritura pública", DocumentKind.Static, 1)],
            CancellationToken.None);

        var definition = await service.GetByCodeAsync("CAMBIO_DE_COLOR", CancellationToken.None);
        Assert.NotNull(definition);
        Assert.Equal("Cambio de color", definition!.Name);
        Assert.Equal(2, definition.Actors.Count);
        Assert.Single(definition.Documents);

        var catalog = await otDb.ProcedureTypeCatalog.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Code == "CAMBIO_DE_COLOR");
        Assert.NotNull(catalog);
        Assert.Equal("Cambio de color", catalog!.Name);

        var duplicate = new ProcedureType
        {
            Name = "Cambio de color",
            Code = "CAMBIO_DE_COLOR_DUP",
            VehicleQueryMode = VehicleQueryMode.Vin,
            IsActive = true,
        };

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await writer.SaveAsync(
                duplicate,
                [("Propietario", 1)],
                [],
                CancellationToken.None));

        proceduresDb.ChangeTracker.Clear();
    }

    [Fact]
    public async Task ListActiveAsync_excludes_inactive_types()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ProcedureTypeWriter>();
        var service = scope.ServiceProvider.GetRequiredService<IProcedureDefinitionService>();

        await writer.SaveAsync(
            new ProcedureType
            {
                Name = "Tipo activo prueba",
                Code = "TIPO_ACTIVO_PRUEBA",
                VehicleQueryMode = VehicleQueryMode.Vin,
                IsActive = true,
            },
            [("Interviniente", 1)],
            [],
            CancellationToken.None);

        await writer.SaveAsync(
            new ProcedureType
            {
                Name = "Tipo inactivo prueba",
                Code = "TIPO_INACTIVO_PRUEBA",
                VehicleQueryMode = VehicleQueryMode.Plate,
                IsActive = false,
            },
            [("Interviniente", 1)],
            [],
            CancellationToken.None);

        var active = await service.ListActiveAsync(CancellationToken.None);
        Assert.Contains(active, p => p.Code == "TIPO_ACTIVO_PRUEBA");
        Assert.DoesNotContain(active, p => p.Code == "TIPO_INACTIVO_PRUEBA");

        var inactiveLookup = await service.GetByCodeAsync("TIPO_INACTIVO_PRUEBA", CancellationToken.None);
        Assert.Null(inactiveLookup);
    }
}

public class ProcedureCodeGeneratorTests
{
    [Theory]
    [InlineData("Traspaso de propiedad", "TRASPASO_DE_PROPIEDAD")]
    [InlineData("Matrícula inicial", "MATRICULA_INICIAL")]
    [InlineData("Cambio-de-color", "CAMBIO_DE_COLOR")]
    public void FromName_generates_uppercase_slug(string name, string expected) =>
        Assert.Equal(expected, ProcedureCodeGenerator.FromName(name));
}
