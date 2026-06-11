using System.Net;
using System.Net.Http.Json;

namespace Flit.Identity.IntegrationTests.Auth;

public class RateLimitTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public RateLimitTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_rate_limit_returns_429_RATE_LIMITED()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        var email = $"ratelimit.{Guid.NewGuid():N}@example.com";
        var client = _factory.AnonymousClient;

        for (var i = 0; i < 5; i++)
        {
            var attempt = await client.PostAsJsonAsync("/api/auth/login",
                new { email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("RATE_LIMITED", await limited.ReadErrorCodeAsync());
    }
}
