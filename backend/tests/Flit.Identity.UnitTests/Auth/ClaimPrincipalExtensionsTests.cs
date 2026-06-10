using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flit.Identity.Shared.Auth;

namespace Flit.Identity.UnitTests.Auth;

public class ClaimPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_returns_id_from_NameIdentifier_claim()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id.ToString())
        ]));

        Assert.Equal(id, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_falls_back_to_sub_claim()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString())
        ]));

        Assert.Equal(id, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_returns_null_when_missing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Null(principal.GetUserId());
    }
}
