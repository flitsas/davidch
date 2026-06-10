using System.Security.Cryptography;
using Flit.Identity.Auth;
using Flit.Identity.Infrastructure.Audit;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Notifications;
using Flit.Identity.Shared.Auth;
using Flit.Identity.Shared.Domain;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flit.Identity.Users;

public sealed class ForceResetHandler(
    IdentityDbContext db,
    IEmailSender email,
    IConfiguration config,
    SessionRevocationService revocation,
    AuditService audit)
{
    public async Task<IResult> HandleAsync(Guid userId, CurrentUser admin, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);
        }

        if (!admin.IsSuperAdmin && user.TenantId != admin.TenantId)
        {
            return Results.Json(new { code = ApiErrorCodes.Forbidden }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (user.Status != UserStatus.Active)
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "User must be active." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        await revocation.RevokeAllSessionsAsync(user.Id, ct);

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(raw),
            Type = ResetTokenType.Forced,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        });
        await db.SaveChangesAsync(ct);

        var link = $"{config["App:PublicBaseUrl"]}/reset-password?token={Uri.EscapeDataString(raw)}";
        await email.SendPasswordResetAsync(user.Email, link, ct);
        await audit.LogPrivilegeChangeAsync(
            admin.Id,
            AuditActions.UserForceReset,
            "user",
            user.Id,
            ct: ct);

        return Results.Ok(new { user_id = user.Id, email = user.Email });
    }
}
