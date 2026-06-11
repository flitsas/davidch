using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Identity.IntegrationTests.Ot;

public class OtDocumentOrderTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory;
    private static HttpClient? _superAdminClient;
    private static readonly SemaphoreSlim LoginGate = new(1, 1);
    private static readonly JsonSerializerOptions OtJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() },
    };

    public OtDocumentOrderTests(IdentityWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Reorder_swaps_positions_under_500ms()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var otId = await GetTenantAOtProfileIdAsync();
        var client = await SuperAdminAsync();
        const string procedure = "MATRICULA_INICIAL";

        var get = await client.GetAsync($"/api/v1/admin/ot/{otId}/document-order/{procedure}");
        get.EnsureSuccessStatusCode();
        var current = await get.Content.ReadFromJsonAsync<DocumentOrderResponse>(OtJson);
        Assert.NotNull(current);
        Assert.True(current!.Items.Count >= 2);

        var first = current.Items[0];
        var second = current.Items[1];
        var payload = current.Items.Select(item =>
        {
            if (item.DocumentTypeCode == first.DocumentTypeCode)
            {
                return new { document_type_code = item.DocumentTypeCode, position = second.Position, is_included = item.IsIncluded };
            }

            if (item.DocumentTypeCode == second.DocumentTypeCode)
            {
                return new { document_type_code = item.DocumentTypeCode, position = first.Position, is_included = item.IsIncluded };
            }

            return new { document_type_code = item.DocumentTypeCode, position = item.Position, is_included = item.IsIncluded };
        }).ToList();

        var stopwatch = Stopwatch.StartNew();
        var put = await client.PutAsJsonAsync(
            $"/api/v1/admin/ot/{otId}/document-order/{procedure}",
            new { items = payload });
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Reorder took {stopwatch.ElapsedMilliseconds} ms");

        var updated = await put.Content.ReadFromJsonAsync<DocumentOrderResponse>(OtJson);
        Assert.NotNull(updated);
        var swappedFirst = updated!.Items.Single(i => i.DocumentTypeCode == first.DocumentTypeCode);
        var swappedSecond = updated.Items.Single(i => i.DocumentTypeCode == second.DocumentTypeCode);
        Assert.Equal(first.Position, swappedSecond.Position);
        Assert.Equal(second.Position, swappedFirst.Position);
    }

    [Fact]
    public async Task Put_rejects_duplicate_document_type_code()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var otId = await GetTenantAOtProfileIdAsync();
        var client = await SuperAdminAsync();

        var get = await client.GetAsync($"/api/v1/admin/ot/{otId}/document-order/MATRICULA_INICIAL");
        get.EnsureSuccessStatusCode();
        var current = await get.Content.ReadFromJsonAsync<DocumentOrderResponse>(OtJson);
        Assert.NotNull(current);

        var duplicatePayload = current!.Items
            .Select(i => new { document_type_code = current.Items[0].DocumentTypeCode, position = i.Position, is_included = i.IsIncluded })
            .ToList();

        var put = await client.PutAsJsonAsync(
            $"/api/v1/admin/ot/{otId}/document-order/MATRICULA_INICIAL",
            new { items = duplicatePayload });

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task Put_rejects_non_contiguous_included_positions()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var otId = await GetTenantAOtProfileIdAsync();
        var client = await SuperAdminAsync();

        var get = await client.GetAsync($"/api/v1/admin/ot/{otId}/document-order/MATRICULA_INICIAL");
        get.EnsureSuccessStatusCode();
        var current = await get.Content.ReadFromJsonAsync<DocumentOrderResponse>(OtJson);
        Assert.NotNull(current);

        var payload = current!.Items
            .Select((item, index) => new
            {
                document_type_code = item.DocumentTypeCode,
                position = item.IsIncluded ? index + 2 : item.Position,
                is_included = item.IsIncluded,
            })
            .ToList();

        var put = await client.PutAsJsonAsync(
            $"/api/v1/admin/ot/{otId}/document-order/MATRICULA_INICIAL",
            new { items = payload });

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task Tenant_admin_can_read_and_update_settings_document_order()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var client = await _factory.LoginAsTenantAdminAsync();

        var types = await client.GetAsync("/api/v1/ot/settings/procedure-types");
        types.EnsureSuccessStatusCode();

        var get = await client.GetAsync("/api/v1/ot/settings/document-order/MATRICULA_INICIAL");
        get.EnsureSuccessStatusCode();

        var current = await get.Content.ReadFromJsonAsync<DocumentOrderResponse>(OtJson);
        Assert.NotNull(current);

        var put = await client.PutAsJsonAsync(
            "/api/v1/ot/settings/document-order/MATRICULA_INICIAL",
            new
            {
                items = current!.Items.Select(i => new
                {
                    document_type_code = i.DocumentTypeCode,
                    position = i.Position,
                    is_included = i.IsIncluded,
                }),
            });
        put.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Document_order_service_returns_only_included_items()
    {
        if (!_factory.IsDockerAvailable)
        {
            return;
        }

        await _factory.EnsureTenantSeededAsync();
        var otId = await GetTenantAOtProfileIdAsync();
        var client = await SuperAdminAsync();

        var get = await client.GetAsync($"/api/v1/admin/ot/{otId}/document-order/MATRICULA_INICIAL");
        get.EnsureSuccessStatusCode();
        var current = await get.Content.ReadFromJsonAsync<DocumentOrderResponse>(OtJson);
        Assert.NotNull(current);

        var excludedCode = current!.Items[0].DocumentTypeCode;
        var includedIndex = 0;
        var payload = current.Items.Select(item =>
        {
            var isIncluded = item.DocumentTypeCode != excludedCode;
            var position = isIncluded ? ++includedIndex : item.Position;
            return new
            {
                document_type_code = item.DocumentTypeCode,
                position,
                is_included = isIncluded,
            };
        }).ToList();

        var put = await client.PutAsJsonAsync(
            $"/api/v1/admin/ot/{otId}/document-order/MATRICULA_INICIAL",
            new { items = payload });
        put.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOtDocumentOrderService>();
        var included = await service.GetIncludedOrderAsync(_factory.TenantId, "MATRICULA_INICIAL", CancellationToken.None);

        Assert.DoesNotContain(included, i => i.DocumentTypeCode == excludedCode);
        Assert.True(included.Count >= 4);
        for (var i = 0; i < included.Count; i++)
        {
            Assert.Equal(i + 1, included[i].Position);
        }
    }

    private async Task<Guid> GetTenantAOtProfileIdAsync()
    {
        await _factory.EnsureTenantSeededAsync();
        using var scope = _factory.Services.CreateScope();
        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
        var profile = await otDb.OtProfiles.AsNoTracking()
            .SingleAsync(o => o.TenantId == _factory.TenantId);
        return profile.Id;
    }

    private async Task<HttpClient> SuperAdminAsync()
    {
        if (_superAdminClient is not null)
        {
            return _superAdminClient;
        }

        await LoginGate.WaitAsync();
        try
        {
            _superAdminClient ??= await _factory.LoginAsSuperAdminAsync();
            return _superAdminClient;
        }
        finally
        {
            LoginGate.Release();
        }
    }

    private sealed record DocumentOrderResponse(
        [property: JsonPropertyName("procedure_type_code")] string ProcedureTypeCode,
        List<DocumentOrderItemResponse> Items);

    private sealed record DocumentOrderItemResponse(
        [property: JsonPropertyName("document_type_code")] string DocumentTypeCode,
        int Position,
        [property: JsonPropertyName("is_included")] bool IsIncluded);
}
