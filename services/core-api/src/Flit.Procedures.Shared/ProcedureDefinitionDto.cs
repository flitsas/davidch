using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Shared;

public sealed record ProcedureDefinitionDto(
    Guid Id,
    string Name,
    string Code,
    VehicleQueryMode VehicleQueryMode,
    bool IsActive,
    IReadOnlyList<ActorDefinitionDto> Actors,
    IReadOnlyList<DocumentDefinitionDto> Documents);

public sealed record ActorDefinitionDto(string RoleLabel, int SortOrder);

public sealed record DocumentDefinitionDto(string Label, DocumentKind Kind, int SortOrder);

public sealed record ProcedureTypeSummaryDto(
    Guid Id,
    string Name,
    string Code,
    VehicleQueryMode VehicleQueryMode,
    bool IsActive,
    int ActorCount,
    int DocumentCount,
    DateTimeOffset UpdatedAt);
