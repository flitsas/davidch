using System.Text.Json.Serialization;
using Flit.OT.Shared.Domain;

namespace Flit.OT.Admin.Config;

public sealed class IntegrationConfigDto
{
    [JsonPropertyName("integration_mode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IntegrationMode IntegrationMode { get; set; }
}

public sealed class PutIntegrationRequest
{
    [JsonPropertyName("integration_mode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IntegrationMode IntegrationMode { get; set; }
}
