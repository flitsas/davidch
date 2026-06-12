using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Runtime.Dashboard;
using Flit.Procedures.Shared.Domain;

namespace Flit.Identity.UnitTests.Procedures;

public class OwnerNameResolverTests
{
    [Fact]
    public void Resolve_prefers_comprador_role()
    {
        var actors = new[]
        {
            Actor("Vendedor", 1, "Cc", "111"),
            Actor("Comprador", 2, "Cc", "222", """{"nombre":"María García"}"""),
        };

        Assert.Equal("María García", OwnerNameResolver.Resolve(actors));
    }

    [Fact]
    public void Resolve_falls_back_to_document_when_no_json()
    {
        var actors = new[] { Actor("Vendedor", 1, "Cc", "1234567890") };
        Assert.Equal("Cc 1234567890", OwnerNameResolver.Resolve(actors));
    }

    private static ProcedureInstanceActor Actor(
        string role, int order, string docType, string docNumber, string? json = null) =>
        new()
        {
            RoleLabel = role,
            SortOrder = order,
            PersonKind = PersonKind.Natural,
            DocumentType = Enum.Parse<DocumentIdType>(docType),
            DocumentNumber = docNumber,
            IsLegalRepresentative = false,
            ExternalDataJson = json,
        };
}
