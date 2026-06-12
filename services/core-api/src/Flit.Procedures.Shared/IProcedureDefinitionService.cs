namespace Flit.Procedures.Shared;

public interface IProcedureDefinitionService
{
    Task<ProcedureDefinitionDto?> GetByIdAsync(Guid procedureTypeId, CancellationToken ct);

    Task<ProcedureDefinitionDto?> GetByCodeAsync(string procedureTypeCode, CancellationToken ct);

    Task<IReadOnlyList<ProcedureTypeSummaryDto>> ListActiveAsync(CancellationToken ct);
}
