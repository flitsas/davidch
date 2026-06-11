using Flit.Companies.Admin.Config;
using Flit.Companies.Admin.Endpoints;
using Flit.Companies.Admin.Crud;
using Flit.Companies.Admin.Index;
using Flit.OT.Admin.Config;
using Flit.OT.Admin.Crud;
using Flit.OT.Admin.Endpoints;
using Flit.OT.Admin.Index;
using Flit.OT.Admin.Settings;
using Flit.OT.Shared;
using Flit.Companies.Runt;
using Flit.Companies.Runt.Endpoints;
using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Seed;
using Flit.OT.Infrastructure.Persistence;
using Flit.OT.Infrastructure.Persistence.Seed;
using Flit.Identity.Api.Middleware;
using Flit.Identity.Auth;
using Flit.Identity.Infrastructure.Audit;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Seed;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Infrastructure.Tenancy;
using Flit.Identity.Notifications;
using Flit.Identity.Rbac;
using Flit.Identity.Users;
using Flit.Identity.Users.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddDbContext<CompaniesDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddDbContext<OtDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddSingleton<RsaJwtService>();
builder.Services.AddScoped<PermissionResolver>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<MeHandler>();
builder.Services.AddScoped<ActivateHandler>();
builder.Services.AddScoped<ForgotPasswordHandler>();
builder.Services.AddScoped<ResetPasswordHandler>();
builder.Services.AddScoped<SessionRevocationService>();
builder.Services.AddSingleton<AuthRateLimiter>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<InviteUserHandler>();
builder.Services.AddScoped<ForceResetHandler>();
builder.Services.AddScoped<SetUserRolesHandler>();
builder.Services.AddScoped<BlockUserHandler>();
builder.Services.AddScoped<CompanyIndexHandler>();
builder.Services.AddScoped<CompanyCrudHandler>();
builder.Services.AddScoped<CompanyConfigHandler>();
builder.Services.AddScoped<CompanyExceptionsHandler>();
builder.Services.AddScoped<CompanyTrafficAuthoritiesHandler>();
builder.Services.AddScoped<OtIndexHandler>();
builder.Services.AddScoped<OtCrudHandler>();
builder.Services.AddScoped<OtIntegrationHandler>();
builder.Services.AddScoped<OtSettingsHandler>();
builder.Services.AddScoped<IOtIntegrationModeService, OtIntegrationModeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<VerifikAdapter>();
builder.Services.AddSingleton<IntempoStubAdapter>();
builder.Services.AddScoped<RuntProxy>();
builder.Services.AddScoped<RuntQueryHandler>();
builder.Services.AddSingleton<AuthorizationService>();
builder.Services.AddSingleton<RoleConflictAnalyzer>();
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await IdentityDbSeeder.SeedAsync(db, app.Configuration, hasher);
    if (app.Environment.IsDevelopment())
    {
        await DevTenantSeeder.SeedAsync(db, hasher);
    }

    var companiesDb = scope.ServiceProvider.GetRequiredService<CompaniesDbContext>();
    await companiesDb.Database.MigrateAsync();
    await CompaniesDbSeeder.SeedAsync(companiesDb);

    var otDb = scope.ServiceProvider.GetRequiredService<OtDbContext>();
    await otDb.Database.MigrateAsync();
    await OtDbSeeder.SeedAsync(otDb);
    if (app.Environment.IsDevelopment())
    {
        await DevOtSeeder.SeedAsync(otDb, db);
    }
}

app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<JwtCookieAuthenticationMiddleware>();
app.UseMiddleware<TokenVersionValidationMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapRbacEndpoints();
app.MapUsersEndpoints();
app.MapTenantsEndpoints();
app.MapCompaniesAdminEndpoints();
app.MapOtAdminEndpoints();
app.MapOtSettingsEndpoints();
app.MapRuntEndpoints();

app.Run();

public partial class Program;
