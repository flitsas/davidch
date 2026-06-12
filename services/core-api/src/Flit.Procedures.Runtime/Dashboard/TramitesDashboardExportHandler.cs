using ClosedXML.Excel;
using Flit.Identity.Shared.Auth;
using Flit.Procedures.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Runtime.Dashboard;

public sealed class TramitesDashboardExportHandler(ProceduresDbContext db)
{
    private const int MaxRows = 10_000;

    public async Task<IResult> HandleAsync(
        CurrentUser user,
        string category,
        string? from,
        string? to,
        Guid? tenantId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Results.Json(
                new { code = "VALIDATION_ERROR", message = "category is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (TramitesDashboardDateRange.TryParse(from, to, out var range) is { } rangeError)
        {
            return rangeError;
        }

        if (TramitesDashboardQuery.TryResolveTenant(user, tenantId, out var effectiveTenantId) is { } tenantError)
        {
            return tenantError;
        }

        var instances = await TramitesDashboardQuery.Apply(db, effectiveTenantId, range!)
            .Include(i => i.Actors)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var rows = instances
            .Where(i => ProcedureCategoryClassifier.MatchesCategory(i.ProcedureTypeCode, category))
            .Select(TramitesDashboardDetailHandler.ToRow)
            .ToList();

        if (rows.Count > MaxRows)
        {
            return Results.Json(
                new
                {
                    code = "EXPORT_LIMIT_EXCEEDED",
                    message = $"Export exceeds {MaxRows} rows. Narrow the date range.",
                },
                statusCode: StatusCodes.Status400BadRequest);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Trámites");
        sheet.Cell(1, 1).Value = "ID";
        sheet.Cell(1, 2).Value = "Fecha radicación";
        sheet.Cell(1, 3).Value = "Estado";
        sheet.Cell(1, 4).Value = "Placa";
        sheet.Cell(1, 5).Value = "Nombre propietario";
        sheet.Cell(1, 6).Value = "Fecha actualización";

        var rowIndex = 2;
        foreach (var row in rows)
        {
            sheet.Cell(rowIndex, 1).Value = row.Id;
            sheet.Cell(rowIndex, 2).Value = row.RadicatedAt.UtcDateTime;
            sheet.Cell(rowIndex, 3).Value = row.Status;
            sheet.Cell(rowIndex, 4).Value = row.Plate;
            sheet.Cell(rowIndex, 5).Value = row.OwnerName;
            sheet.Cell(rowIndex, 6).Value = row.UpdatedAt.UtcDateTime;
            rowIndex++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"tramites-{category}-{range!.From:yyyy-MM-dd}-{range.To:yyyy-MM-dd}.xlsx";
        return Results.File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
