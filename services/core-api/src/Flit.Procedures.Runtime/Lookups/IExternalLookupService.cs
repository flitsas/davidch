using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Runtime.Lookups;

public interface IExternalLookupService
{
    Task<object> QueryRuesAsync(string nit, CancellationToken ct);

    Task<object> QuerySimitAsync(DocumentIdType documentType, string documentNumber, CancellationToken ct);

    Task<object> QueryRnmcAsync(DocumentIdType documentType, string documentNumber, CancellationToken ct);
}
