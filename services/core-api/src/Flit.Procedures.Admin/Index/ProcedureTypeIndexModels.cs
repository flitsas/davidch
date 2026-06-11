using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Admin.Index;

public sealed record ProcedureTypeIndexRequest(
    int Page = 1,
    int PageSize = 20,
    string? Sort = null,
    string? Name = null,
    bool? IsActive = null);

public sealed record ProcedureTypeIndexItem(
    Guid Id,
    string Name,
    string Code,
    VehicleQueryMode VehicleQueryMode,
    bool IsActive,
    int ActorCount,
    int DocumentCount,
    DateTimeOffset UpdatedAt);

public sealed record ProcedureTypeIndexResponse(
    IReadOnlyList<ProcedureTypeIndexItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
