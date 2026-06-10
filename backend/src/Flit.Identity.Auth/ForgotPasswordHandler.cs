using System.Security.Cryptography;
using Flit.Identity.Infrastructure.Persistence;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Security;
using Flit.Identity.Notifications;
using Flit.Identity.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flit.Identity.Auth;

public record ForgotPasswordRequest(string Email);

public sealed class ForgotPasswordHandler(
    IdentityDbContext db,
    IEmailSender email,
    IConfiguration config)
{
    public async Task<IResult> HandleAsync(ForgotPasswordRequest req, HttpContext http, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
        {
            return Results.Ok(new { message = "If an account exists, a reset link has been sent." });
        }

        var normalizedEmail = req.Email.ToLowerInvariant();

        var user = await db.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail && u.Status == UserStatus.Active, ct);

        if (user is not null)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = TokenHasher.Sha256(raw),
                Type = ResetTokenType.SelfService,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            });
            await db.SaveChangesAsync(ct);

            var link = $"{config["App:PublicBaseUrl"]}/reset-password?token={Uri.EscapeDataString(raw)}";
            await email.SendPasswordResetAsync(user.Email, link, ct);
        }

        return Results.Ok(new { message = "If an account exists, a reset link has been sent." });
    }
}
