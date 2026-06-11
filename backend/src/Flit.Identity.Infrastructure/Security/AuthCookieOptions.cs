using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Flit.Identity.Infrastructure.Security;

public static class AuthCookieOptions
{
    public const string AccessCookieName = "flit_access";
    public const string RefreshCookieName = "flit_refresh";

    public static CookieOptions Access(int minutes, IWebHostEnvironment env) => new()
    {
        HttpOnly = true,
        Secure = UseSecureCookies(env),
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(minutes)
    };

    public static CookieOptions Refresh(int days, IWebHostEnvironment env) => new()
    {
        HttpOnly = true,
        Secure = UseSecureCookies(env),
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromDays(days)
    };

    public static CookieOptions Delete(IWebHostEnvironment env) => new()
    {
        HttpOnly = true,
        Secure = UseSecureCookies(env),
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = DateTimeOffset.UnixEpoch
    };

    private static bool UseSecureCookies(IWebHostEnvironment env) =>
        !env.IsDevelopment() && !string.Equals(env.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
