using System.Net;
using System.Net.Http.Json;
using Flit.Procedures.Runtime.Index;

namespace Flit.Identity.IntegrationTests.Tramites;

public class TramitesIndexTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;

    public TramitesIndexTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Index_returns_paginated_items_for_tenant()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var response = await client.GetAsync("/api/v1/tramites/index?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TramitesIndexResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(10, body.PageSize);
    }
}
