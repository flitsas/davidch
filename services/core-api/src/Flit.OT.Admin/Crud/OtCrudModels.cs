using System.Text.Json.Serialization;
using Flit.OT.Shared.Domain;

namespace Flit.OT.Admin.Crud;

public sealed class CreateOtRequest
{
    public string Mode { get; set; } = "";
    [JsonPropertyName("divipol_code")]
    public string DivipolCode { get; set; } = "";
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";
    public string? Slug { get; set; }
    [JsonPropertyName("tenant_id")]
    public Guid? TenantId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OtStatus Status { get; set; } = OtStatus.Active;
}

public sealed class UpdateOtRequest
{
    [JsonPropertyName("divipol_code")]
    public string DivipolCode { get; set; } = "";
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";
}

public sealed class UpdateOtStatusRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OtStatus Status { get; set; }
}

public sealed record OtDetailResponse(
    Guid Id,
    Guid TenantId,
    string DivipolCode,
    string DisplayName,
    OtStatus Status,
    IntegrationMode IntegrationMode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    OtTenantSummary Tenant);

public sealed record OtTenantSummary(Guid Id, string Name, string Slug, bool IsActive);

public sealed record OtCreatedResponse(Guid Id, Guid TenantId);
