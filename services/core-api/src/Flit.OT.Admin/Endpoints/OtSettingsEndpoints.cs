using Flit.OT.Admin.Auth;
using Flit.OT.Admin.Config;
using Flit.OT.Admin.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Flit.OT.Admin.Endpoints;

public static class OtSettingsEndpoints
{
    public static IEndpointRouteBuilder MapOtSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ot/settings")
            .RequireOtAdmin();

        group.MapGet("/config/integration", GetIntegrationAsync);
        group.MapPut("/config/integration", PutIntegrationAsync);

        return app;
    }

    private static Task<IResult> GetIntegrationAsync(OtSettingsHandler handler, CancellationToken ct) =>
        handler.GetIntegrationAsync(ct);

    private static Task<IResult> PutIntegrationAsync(
        PutIntegrationRequest request,
        OtSettingsHandler handler,
        CancellationToken ct) =>
        handler.PutIntegrationAsync(request, ct);
}
