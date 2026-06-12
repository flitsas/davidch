namespace Flit.Procedures.Shared;

public interface IProcedureDefinitionService
{
    Task<ProcedureDefinitionDto?> GetByCodeAsync(string procedureTypeCode, CancellationToken ct);

    Task<IReadOnlyList<ProcedureTypeSummaryDto>> ListActiveAsync(CancellationToken ct);
}
