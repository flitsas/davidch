using Microsoft.AspNetCore.Http;

namespace Flit.Procedures.Runtime.Dashboard;

public sealed record TramitesDashboardDateRange(
    DateOnly From,
    DateOnly To,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc)
{
    public static IResult? TryParse(
        string? from,
        string? to,
        out TramitesDashboardDateRange? range)
    {
        range = null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = ParseOrDefault(from, today.AddDays(-30));
        var toDate = ParseOrDefault(to, today);

        if (fromDate > toDate)
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "from must be <= to." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (toDate.DayNumber - fromDate.DayNumber > 366)
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "Date range cannot exceed 366 days." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        range = new TramitesDashboardDateRange(
            fromDate,
            toDate,
            new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(toDate.ToDateTime(new TimeOnly(23, 59, 59)), TimeSpan.Zero));
        return null;
    }

    private static DateOnly ParseOrDefault(string? value, DateOnly fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : DateOnly.Parse(value);
}
