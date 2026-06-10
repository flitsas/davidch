using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Api.Middleware;

public sealed class TokenVersionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IdentityDbContext db)
    {
        var sub = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null)
        {
            await next(context);
            return;
        }

        var claimVersion = int.Parse(context.User.FindFirstValue("token_version") ?? "0");
        var userId = Guid.Parse(sub);

        if (!context.Items.ContainsKey("DbTokenVersion"))
        {
            var dbVersion = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => (int?)u.TokenVersion)
                .SingleOrDefaultAsync(context.RequestAborted);

            context.Items["DbTokenVersion"] = dbVersion;
        }

        var storedVersion = (int?)context.Items["DbTokenVersion"];
        if (storedVersion is null || storedVersion.Value != claimVersion)
        {
            context.Response.Headers["X-Session-Revoked"] = "true";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = ApiErrorCodes.SessionRevoked,
                message = "Session revoked."
            });
            return;
        }

        await next(context);
    }
}
