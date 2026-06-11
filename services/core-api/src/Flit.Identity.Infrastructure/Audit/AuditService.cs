using System.Text.Json;
using Flit.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flit.Identity.Infrastructure.Audit;

public sealed class AuditService(IdentityDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public Task LogLoginFailureAsync(string email, string? ip, CancellationToken ct = default) =>
        LogAsync(null, AuditActions.LoginFailure, "auth", null, new { email, ip }, ct);

    public Task LogSuperAdminBypassAsync(
        Guid actorUserId,
        string permissionKey,
        string? path,
        CancellationToken ct = default) =>
        LogAsync(actorUserId, AuditActions.SuperAdminBypass, "permission", null, new { permission_key = permissionKey, path }, ct);

    public Task LogPrivilegeChangeAsync(
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        object? metadata = null,
        CancellationToken ct = default) =>
        LogAsync(actorUserId, action, targetType, targetId, metadata, ct);

    private async Task LogAsync(
        Guid? actorUserId,
        string action,
        string targetType,
        Guid? targetId,
        object? metadata,
        CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
