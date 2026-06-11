using System.Net;
using Microsoft.AspNetCore.Http;

namespace Flit.Companies.Runt;

public sealed class IntempoStubAdapter(IHttpContextAccessor httpContextAccessor) : IRuntProvider
{
    public string Name => "Intempo";

    public async Task<RuntQueryResult> QueryAsync(RuntQueryType type, string value, CancellationToken ct)
    {
        var started = Environment.TickCount64;
        var mode = httpContextAccessor.HttpContext?.Request.Headers["X-Runt-Stub-Mode"].FirstOrDefault()
            ?? "success";

        switch (mode.ToLowerInvariant())
        {
            case "timeout":
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                break;
            case "5xx":
                throw new HttpRequestException("Intempo stub 503", null, HttpStatusCode.ServiceUnavailable);
        }

        var payload = new
        {
            type = type.ToString(),
            query = value,
            source = Name,
            status = "ok",
            data = new { summary = $"Stub response for {type}={value}" }
        };

        return new RuntQueryResult(Name, payload, (int)(Environment.TickCount64 - started));
    }
}
