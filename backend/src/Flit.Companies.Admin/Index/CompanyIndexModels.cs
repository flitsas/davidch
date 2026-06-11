using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Admin.Index;

public sealed class CompanyIndexRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
    public string? Nit { get; set; }
    public string? Name { get; set; }
    public Guid? Id { get; set; }
    public DateTimeOffset? AuditFrom { get; set; }
    public DateTimeOffset? AuditTo { get; set; }
}

public sealed record CompanyIndexItem(
    Guid Id,
    Guid TenantId,
    string Nit,
    string LegalName,
    CompanyStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CompanyIndexResponse(
    IReadOnlyList<CompanyIndexItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
