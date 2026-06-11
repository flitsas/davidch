using Flit.Identity.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Identity.Api.Middleware;

public sealed class JwtCookieAuthenticationMiddleware(RequestDelegate next, RsaJwtService jwt)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(AuthCookieOptions.AccessCookieName, out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            try
            {
                context.User = jwt.Validate(token);
            }
            catch (SecurityTokenExpiredException)
            {
                // leave unauthenticated; refresh endpoint handles renewal
            }
            catch (SecurityTokenException)
            {
                // invalid token — leave unauthenticated
            }
        }

        await next(context);
    }
}
