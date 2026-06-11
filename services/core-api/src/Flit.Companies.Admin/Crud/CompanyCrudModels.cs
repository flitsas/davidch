using System.Text.Json.Serialization;
using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Admin.Crud;

public sealed class CreateCompanyRequest
{
    public string Mode { get; set; } = "";
    public string Nit { get; set; } = "";
    [JsonPropertyName("legal_name")]
    public string LegalName { get; set; } = "";
    public string? Slug { get; set; }
    [JsonPropertyName("tenant_id")]
    public Guid? TenantId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
}

public sealed class UpdateCompanyRequest
{
    public string Nit { get; set; } = "";
    [JsonPropertyName("legal_name")]
    public string LegalName { get; set; } = "";
}

public sealed class UpdateCompanyStatusRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CompanyStatus Status { get; set; }
}

public sealed record CompanyDetailResponse(
    Guid Id,
    Guid TenantId,
    string Nit,
    string LegalName,
    CompanyStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TenantSummary Tenant);

public sealed record TenantSummary(Guid Id, string Name, string Slug, bool IsActive);

public sealed record CompanyCreatedResponse(Guid Id, Guid TenantId);
