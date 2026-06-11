using System.Text.Json.Serialization;

namespace Flit.OT.Admin.Config;

public sealed class DocumentOrderItemInput
{
    [JsonPropertyName("document_type_code")]
    public string DocumentTypeCode { get; set; } = "";
    public int Position { get; set; }
    [JsonPropertyName("is_included")]
    public bool IsIncluded { get; set; } = true;
}

public sealed class PutDocumentOrderRequest
{
    public List<DocumentOrderItemInput> Items { get; set; } = [];
}

public sealed record DocumentOrderItemResponse(
    [property: JsonPropertyName("document_type_code")] string DocumentTypeCode,
    [property: JsonPropertyName("document_type_name")] string DocumentTypeName,
    int Position,
    [property: JsonPropertyName("is_included")] bool IsIncluded);

public sealed record DocumentOrderResponse(
    [property: JsonPropertyName("procedure_type_code")] string ProcedureTypeCode,
    IReadOnlyList<DocumentOrderItemResponse> Items);

public sealed record ProcedureTypeSummary(string Code, string Name);
