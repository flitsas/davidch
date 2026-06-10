using System.Text.Json;
using Flit.Identity.Auth;
using Flit.Identity.Shared.Errors;

namespace Flit.Identity.Api.Middleware;

public sealed class RateLimitingMiddleware(RequestDelegate next, AuthRateLimiter rateLimiter)
{
    private static readonly HashSet<string> RateLimitedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/forgot-password"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && RateLimitedPaths.Contains(context.Request.Path.Value ?? ""))
        {
            var email = await ReadEmailAsync(context.Request);
            if (email is not null)
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!rateLimiter.TryAcquire(ip, email))
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsJsonAsync(new { code = ApiErrorCodes.RateLimited });
                    return;
                }
            }
        }

        await next(context);
    }

    private static async Task<string?> ReadEmailAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("email", out var emailProp)
                && emailProp.ValueKind == JsonValueKind.String)
            {
                var email = emailProp.GetString();
                return string.IsNullOrWhiteSpace(email) ? null : email.ToLowerInvariant();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
