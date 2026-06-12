using System.Text.Json.Serialization;
using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Admin.Crud;

public sealed record ActorInputDto(string RoleLabel);

public sealed record DocumentInputDto(
    string Label,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    DocumentKind Kind);

public sealed record SaveProcedureTypeRequest(
    string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    VehicleQueryMode VehicleQueryMode,
    IReadOnlyList<ActorInputDto> Actors,
    IReadOnlyList<DocumentInputDto> Documents);

public sealed record ActorDetailDto(string RoleLabel, int SortOrder);

public sealed record DocumentDetailDto(
    string Label,
    DocumentKind Kind,
    int SortOrder);

public sealed record ProcedureTypeDetailDto(
    Guid Id,
    string Name,
    string Code,
    VehicleQueryMode VehicleQueryMode,
    bool IsActive,
    IReadOnlyList<ActorDetailDto> Actors,
    IReadOnlyList<DocumentDetailDto> Documents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProcedureTypeCreatedResponse(Guid Id, string Code);

public sealed record UpdateProcedureTypeStatusRequest(bool IsActive);
