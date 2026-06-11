namespace Flit.Companies.Runt;

public sealed class VerifikAdapter : IRuntProvider
{
    public string Name => "Verifik";

    public Task<RuntQueryResult> QueryAsync(RuntQueryType type, string value, CancellationToken ct)
    {
        var started = Environment.TickCount64;
        var apiKey = Environment.GetEnvironmentVariable("VERIFIK_API_KEY");

        object payload = apiKey is null
            ? new
            {
                type = type.ToString(),
                query = value,
                source = Name,
                status = "stub",
                message = "VERIFIK_API_KEY not configured — returning sandbox stub"
            }
            : new
            {
                type = type.ToString(),
                query = value,
                source = Name,
                status = "ok",
                data = new { summary = $"Verifik sandbox response for {type}={value}" }
            };

        return Task.FromResult(new RuntQueryResult(Name, payload, (int)(Environment.TickCount64 - started)));
    }
}
