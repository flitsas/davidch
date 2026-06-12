using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Runtime.Lookups;

public sealed class StubExternalLookupService : IExternalLookupService
{
    public Task<object> QueryRuesAsync(string nit, CancellationToken ct) =>
        Task.FromResult<object>(new
        {
            nit,
            razonSocial = "Empresa Demo S.A.S.",
            estado = "ACTIVA",
        });

    public Task<object> QuerySimitAsync(DocumentIdType documentType, string documentNumber, CancellationToken ct) =>
        Task.FromResult<object>(new
        {
            documentType = documentType.ToString(),
            documentNumber,
            comparendos = Array.Empty<object>(),
        });

    public Task<object> QueryRnmcAsync(DocumentIdType documentType, string documentNumber, CancellationToken ct) =>
        Task.FromResult<object>(new
        {
            documentType = documentType.ToString(),
            documentNumber,
            medidas = Array.Empty<object>(),
        });
}
