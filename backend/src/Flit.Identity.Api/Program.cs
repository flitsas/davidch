using Flit.Identity.Api.Middleware;
using Flit.Identity.Auth;
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
builder.Services.AddSingleton<ForgotPasswordRateLimiter>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<InviteUserHandler>();
builder.Services.AddScoped<ForceResetHandler>();
builder.Services.AddScoped<SetUserRolesHandler>();
builder.Services.AddScoped<BlockUserHandler>();
builder.Services.AddSingleton<AuthorizationService>();
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();
    await IdentityDbSeeder.SeedAsync(
        db,
        app.Configuration,
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

app.UseMiddleware<JwtCookieAuthenticationMiddleware>();
app.UseMiddleware<TokenVersionValidationMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapRbacEndpoints();
app.MapUsersEndpoints();

app.Run();

public partial class Program;
