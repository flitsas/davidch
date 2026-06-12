using System.Net.Http.Json;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Persistence.Seed;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Seed;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Seed;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Notifications;
using Flit.Identity.Shared.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Flit.Identity.IntegrationTests;

public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private bool _dockerAvailable;
    private readonly CapturingEmailSender _emailSender = new();
    private bool _tenantSeeded;

    public bool IsDockerAvailable => _dockerAvailable;

    public Guid TenantId { get; private set; }
    public Guid TenantOperatorRoleId { get; private set; }
    public Guid TenantAdminRoleId { get; private set; }
    public Guid TenantAdminUserId { get; private set; }

    public string TenantAdminEmail => DevTenantSeeder.TenantAdminEmail;
    public string TenantAdminPassword => DevTenantSeeder.TenantAdminPassword;

    public HttpClient AnonymousClient => CreateClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (!_dockerAvailable || _postgres is null)
        {
            return;
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = _postgres.GetConnectionString(),
                ["Identity:BootstrapEmail"] = "super@flit.local",
                ["Identity:BootstrapPassword"] = "ChangeMe!123",
                ["Identity:EnableRateLimiting"] = "true",
                ["App:PublicBaseUrl"] = "http://localhost:5000"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(_emailSender);
        });
    }

    public async Task EnsureTenantSeededAsync()
    {
        if (_tenantSeeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await DevTenantSeeder.SeedAsync(db, hasher);

        var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();
        await DevCompanySeeder.SeedAsync(companiesDb, db);

        var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
        await DevOtSeeder.SeedAsync(otDb, db);

        var tenant = await db.Tenants.SingleAsync(t => t.Slug == DevTenantSeeder.TenantSlug);
        TenantId = tenant.Id;

        var adminRole = await db.Roles.SingleAsync(r =>
            r.TenantId == tenant.Id && r.Name == "TenantA-Admin");
        TenantAdminRoleId = adminRole.Id;

        var operatorRole = await db.Roles.SingleAsync(r =>
            r.TenantId == tenant.Id && r.Name == "TenantA-Operator");
        TenantOperatorRoleId = operatorRole.Id;

        var adminUser = await db.Users.SingleAsync(u => u.Email == DevTenantSeeder.TenantAdminEmail);
        TenantAdminUserId = adminUser.Id;

        _tenantSeeded = true;
    }

    public async Task<HttpClient> LoginAsTenantAdminAsync()
    {
        await EnsureTenantSeededAsync();

        var client = CreateClient(new() { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TenantAdminEmail, password = TenantAdminPassword });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public async Task<HttpClient> LoginAsTenantOperatorAsync()
    {
        await EnsureTenantSeededAsync();

        var client = CreateClient(new() { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = DevTenantSeeder.TenantOperatorEmail, password = DevTenantSeeder.TenantOperatorPassword });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public async Task<HttpClient> LoginAsSuperAdminAsync()
    {
        var client = CreateClient(new() { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "super@flit.local", password = "ChangeMe!123" });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public string? GetLatestInvitationToken(string email) => _emailSender.ExtractToken(email);

    public string? GetLatestResetToken(string email) => _emailSender.ExtractResetToken(email);

    public async Task<ActiveUserInfo> CreateActiveUserAsync(string? email = null, string password = "SecurePass!123")
    {
        await EnsureTenantSeededAsync();
        email ??= $"active.{Guid.NewGuid():N}@tenant-a.com";

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = hasher.Hash(password),
            TenantId = TenantId,
            Status = UserStatus.Active,
            TokenVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new ActiveUserInfo(user.Id, user.Email, password);
    }

    public async Task<IReadOnlyDictionary<string, string>> LoginAndGetCookiesAsync(
        string email,
        string password = "SecurePass!123")
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        return ParseCookies(login.Headers.GetValues("Set-Cookie"));
    }

    public async Task<HttpResponseMessage> SendWithCookiesAsync(
        string url,
        IReadOnlyDictionary<string, string> cookies,
        HttpMethod? method = null)
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);
        request.Headers.Add("Cookie", FormatCookieHeader(cookies));
        return await client.SendAsync(request);
    }

    public async Task<string> CreateSelfServiceResetTokenAsync(string email)
    {
        var response = await AnonymousClient.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        response.EnsureSuccessStatusCode();
        var token = GetLatestResetToken(email);
        return token ?? throw new InvalidOperationException($"No reset token captured for {email}.");
    }

    private static IReadOnlyDictionary<string, string> ParseCookies(IEnumerable<string> setCookieHeaders)
    {
        var cookies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var header in setCookieHeaders)
        {
            var pair = header.Split(';')[0].Split('=', 2);
            if (pair.Length == 2)
            {
                cookies[pair[0]] = pair[1];
            }
        }

        return cookies;
    }

    private static string FormatCookieHeader(IReadOnlyDictionary<string, string> cookies) =>
        string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));

    public sealed record ActiveUserInfo(Guid Id, string Email, string Password);

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine")
                .Build();

            await _postgres.StartAsync();
            _dockerAvailable = true;
        }
        catch
        {
            _dockerAvailable = false;
        }
    }

    public new async Task DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
