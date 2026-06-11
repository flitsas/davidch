using System.Text.Json.Serialization;
using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Admin.Config;

public sealed class MatriculaConfigDto
{
    public bool AllowNewVehicleFiling { get; set; }
    public bool AllowMiscProcedures { get; set; }
}

public sealed class TraspasoConfigDto
{
    public bool OnlyOwnVehicles { get; set; }
}

public sealed class SignatureConfigDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SignatureType SellerSignatureType { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SignatureType BuyerSignatureType { get; set; }
    public bool VaultEnabled { get; set; }
    public string? VaultSettings { get; set; }
}

public sealed class NotificationConfigDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationChannel Channel { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationTarget NotificationTarget { get; set; }
    public string? ClientApiSettings { get; set; }
}

public sealed class PaymentConfigDto
{
    public bool AllowFlitGateway { get; set; }
    public bool AllowOt { get; set; }
    public bool AllowOther { get; set; }
}

public sealed class RuntConfigDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuntProvider PrimaryProvider { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuntProvider SecondaryProvider { get; set; }
    public int FailoverTimeoutMs { get; set; }
    public string ProviderCredentials { get; set; } = "{}";
}

public sealed class ExceptionsBatchRequest
{
    [JsonPropertyName("user_ids")]
    public List<Guid> UserIds { get; set; } = [];
}

public sealed class TrafficAuthorityUpdateItem
{
    [JsonPropertyName("authority_code")]
    public string AuthorityCode { get; set; } = "";
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }
}

public sealed class TrafficAuthoritiesPatchRequest
{
    public List<TrafficAuthorityUpdateItem> Updates { get; set; } = [];
}

public sealed record TrafficAuthorityItem(
    string AuthorityCode,
    string Name,
    string? Region,
    bool IsEnabled);

public sealed record TrafficAuthoritiesResponse(
    List<TrafficAuthorityItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
