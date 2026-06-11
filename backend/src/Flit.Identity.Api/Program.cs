using Flit.Companies.Infrastructure.Persistence;
using Flit.Companies.Infrastructure.Persistence.Seed;
using Flit.Companies.Admin.Endpoints;
using Flit.Companies.Admin.Index;
using Flit.Companies.Admin.Crud;
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

builder.Services.AddDbContext<CompaniesDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Identity")));
builder.Services.AddDbContext<IdentityDbContext>(o =>
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

app.Run();

public partial class Program;
