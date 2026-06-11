using System.Security.Cryptography;
using System.Text.Json.Serialization;
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

public record InviteRequest(
    string Email,
    [property: JsonPropertyName("role_ids")] Guid[] RoleIds,
    [property: JsonPropertyName("tenant_id")] Guid? TenantId);

public sealed class InviteUserHandler(
    IdentityDbContext db,
    IEmailSender email,
    IConfiguration config)
{
    public async Task<IResult> HandleAsync(InviteRequest req, CurrentUser inviter, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
        {
            return Results.Json(
                new { code = ApiErrorCodes.ValidationError, message = "Email is required." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var tenantId = inviter.IsSuperAdmin ? req.TenantId : inviter.TenantId;
        if (tenantId is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tenant_id"] = ["Required for SuperAdmin invites."]
            });
        }

        var normalizedEmail = req.Email.ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            return Results.Json(new { code = "EMAIL_EXISTS" }, statusCode: StatusCodes.Status409Conflict);
        }

        if (req.RoleIds.Length > 0)
        {
            var validRoleCount = await db.Roles
                .CountAsync(r => req.RoleIds.Contains(r.Id) && r.TenantId == tenantId, ct);
            if (validRoleCount != req.RoleIds.Length)
            {
                return Results.Json(
                    new { code = ApiErrorCodes.ValidationError, message = "One or more role IDs are invalid for this tenant." },
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = normalizedEmail,
            Status = UserStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        foreach (var roleId in req.RoleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.InvitationTokens.Add(new InvitationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(raw),
            InvitedBy = inviter.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(72)
        });
        await db.SaveChangesAsync(ct);

        var link = $"{config["App:PublicBaseUrl"]}/activate?token={Uri.EscapeDataString(raw)}";
        await email.SendInvitationAsync(user.Email, link, ct);

        return Results.Created($"/api/users/{user.Id}",
            new { user_id = user.Id, email = user.Email, status = "Pending" });
    }
}
