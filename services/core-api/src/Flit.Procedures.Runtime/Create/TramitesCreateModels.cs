using System.Text.Json.Serialization;
using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Runtime.Create;

public sealed record CreateTramiteRequest(
    Guid ProcedureTypeId,
    string OtDivipolCode,
    string VehicleQueryValue,
    IReadOnlyList<CreateTramiteActorRequest> Actors,
    IReadOnlyList<CreateTramiteDocumentRequest>? Documents);

public sealed record CreateTramiteDocumentRequest(
    string Label,
    string FileName,
    long FileSizeBytes);

public sealed record CreateTramiteActorRequest(
    string RoleLabel,
    int SortOrder,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PersonKind PersonKind,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DocumentIdType DocumentType,
    string DocumentNumber,
    CreateLegalRepresentativeRequest? LegalRepresentative);

public sealed record CreateLegalRepresentativeRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DocumentIdType DocumentType,
    string DocumentNumber);

public sealed record TramiteCreatedResponse(Guid Id, string Status);
