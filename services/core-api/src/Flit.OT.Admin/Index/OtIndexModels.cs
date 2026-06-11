using Flit.OT.Shared.Domain;

namespace Flit.OT.Admin.Index;

public sealed class OtIndexRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
    public string? Divipol { get; set; }
    public string? Name { get; set; }
    public Guid? Id { get; set; }
    public DateTimeOffset? AuditFrom { get; set; }
    public DateTimeOffset? AuditTo { get; set; }
}

public sealed record OtIndexItem(
    Guid Id,
    Guid TenantId,
    string DivipolCode,
    string DisplayName,
    OtStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OtIndexResponse(
    IReadOnlyList<OtIndexItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
