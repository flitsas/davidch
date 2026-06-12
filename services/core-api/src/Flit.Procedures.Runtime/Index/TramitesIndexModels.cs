namespace Flit.Procedures.Runtime.Index;

public sealed record TramiteIndexItem(
    Guid Id,
    string Reference,
    string ProcedureTypeName,
    string ProcedureTypeCode,
    string OtDisplayName,
    string OtDivipolCode,
    string VehicleQueryValue,
    string Status,
    DateTimeOffset CreatedAt,
    Guid CreatedBy);

public sealed record TramitesIndexResponse(
    IReadOnlyList<TramiteIndexItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
