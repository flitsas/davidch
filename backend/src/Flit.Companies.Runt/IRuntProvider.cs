namespace Flit.Companies.Runt;

public sealed record RuntQueryResult(
    string Provider,
    object Payload,
    int LatencyMs,
    string? FailoverReason = null);

public interface IRuntProvider
{
    string Name { get; }
    Task<RuntQueryResult> QueryAsync(RuntQueryType type, string value, CancellationToken ct);
}
