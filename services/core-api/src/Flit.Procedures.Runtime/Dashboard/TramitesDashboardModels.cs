namespace Flit.Procedures.Runtime.Dashboard;

public sealed record DashboardCategoryDto(string Key, string Label, int Count, double Percent);

public sealed record DashboardSummaryResponse(
    string From,
    string To,
    int Total,
    IReadOnlyList<DashboardCategoryDto> Categories);

public sealed record DashboardDetailRowDto(
    string Id,
    Guid ProcedureInstanceId,
    DateTimeOffset RadicatedAt,
    string Status,
    string Plate,
    string OwnerName,
    DateTimeOffset UpdatedAt);

public sealed record DashboardDetailResponse(
    IReadOnlyList<DashboardDetailRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DashboardUserItemDto(
    Guid UserId,
    string DisplayName,
    string Email,
    int Count);

public sealed record DashboardUsersTopResponse(IReadOnlyList<DashboardUserItemDto> Items);

public sealed record DashboardUsersSearchResponse(
    IReadOnlyList<DashboardUserSearchItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DashboardUserSearchItemDto(Guid UserId, string Email);

public sealed record DashboardUserStatsResponse(Guid UserId, int Count);
