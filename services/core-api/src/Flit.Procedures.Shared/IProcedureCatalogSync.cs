namespace Flit.Procedures.Shared;

public interface IProcedureCatalogSync
{
    Task UpsertAsync(string code, string name, CancellationToken ct);
}
