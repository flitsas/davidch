using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Companies.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace Flit.Companies.Runt;

public sealed class RuntProxy(
    VerifikAdapter verifik,
    IntempoStubAdapter intempo,
    ILogger<RuntProxy> logger)
{
    public async Task<RuntQueryResult> ExecuteAsync(
        CompanyRuntConfig config,
        RuntQueryType type,
        string value,
        CancellationToken ct)
    {
        var primary = ResolveProvider(config.PrimaryProvider);
        var secondary = ResolveProvider(config.SecondaryProvider);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(config.FailoverTimeoutMs);
            var result = await primary.QueryAsync(type, value, timeoutCts.Token);
            LogQuery(config.CompanyId, type, result.Provider, result.LatencyMs, null);
            return result;
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            var reason = ex is OperationCanceledException ? "timeout" : "5xx";
            logger.LogWarning(ex, "RUNT primary provider failed for company {CompanyId}, failing over", config.CompanyId);
            var fallback = await secondary.QueryAsync(type, value, ct);
            LogQuery(config.CompanyId, type, fallback.Provider, fallback.LatencyMs, reason);
            return new RuntQueryResult(
                fallback.Provider,
                fallback.Payload,
                fallback.LatencyMs,
                reason);
        }
    }

    private IRuntProvider ResolveProvider(RuntProvider provider) =>
        provider == RuntProvider.Intempo ? intempo : verifik;

    private void LogQuery(Guid companyId, RuntQueryType type, string provider, int latencyMs, string? failoverReason)
    {
        logger.LogInformation(
            "RUNT query company={CompanyId} type={Type} provider={Provider} latency_ms={Latency} failover={Failover}",
            companyId,
            type,
            provider,
            latencyMs,
            failoverReason);
    }
}
