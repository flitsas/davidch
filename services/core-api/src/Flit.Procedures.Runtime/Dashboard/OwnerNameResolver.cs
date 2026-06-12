using System.Text.Json;
using Flit.Procedures.Infrastructure.Persistence.Entities;
using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Runtime.Dashboard;

public static class OwnerNameResolver
{
    private static readonly string[] PreferredRoleTokens = ["propietario", "comprador", "titular"];

    public static string Resolve(IEnumerable<ProcedureInstanceActor> actors)
    {
        var candidates = actors.Where(a => !a.IsLegalRepresentative).ToList();
        var selected = candidates.FirstOrDefault(MatchesPreferredRole)
            ?? candidates.MinBy(a => a.SortOrder);

        if (selected is null)
        {
            return "—";
        }

        var fromJson = TryReadName(selected);
        if (!string.IsNullOrWhiteSpace(fromJson))
        {
            return fromJson;
        }

        return $"{selected.DocumentType} {selected.DocumentNumber}";
    }

    private static bool MatchesPreferredRole(ProcedureInstanceActor actor)
    {
        var role = actor.RoleLabel.ToLowerInvariant();
        return PreferredRoleTokens.Any(token => role.Contains(token, StringComparison.Ordinal));
    }

    private static string? TryReadName(ProcedureInstanceActor actor)
    {
        if (string.IsNullOrWhiteSpace(actor.ExternalDataJson))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(actor.ExternalDataJson);
        var root = doc.RootElement;
        if (actor.PersonKind == PersonKind.Juridica
            && root.TryGetProperty("razonSocial", out var razon))
        {
            return razon.GetString();
        }

        if (root.TryGetProperty("nombre", out var nombre))
        {
            return nombre.GetString();
        }

        return null;
    }
}
