using Flit.Companies.Infrastructure.Persistence;
using Flit.Identity.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Admin.Config;

public sealed class CompanyConfigHandler(CompaniesDbContext db)
{
    public Task<IResult> GetMatriculaAsync(Guid companyId, CancellationToken ct) =>
        GetConfigAsync(
            companyId,
            db.CompanyMatriculaConfigs,
            c => new MatriculaConfigDto
            {
                AllowNewVehicleFiling = c.AllowNewVehicleFiling,
                AllowMiscProcedures = c.AllowMiscProcedures
            },
            ct);

    public Task<IResult> PutMatriculaAsync(Guid companyId, MatriculaConfigDto dto, CancellationToken ct) =>
        PutConfigAsync(
            companyId,
            db.CompanyMatriculaConfigs,
            (c, now) =>
            {
                c.AllowNewVehicleFiling = dto.AllowNewVehicleFiling;
                c.AllowMiscProcedures = dto.AllowMiscProcedures;
                c.UpdatedAt = now;
            },
            ct);

    public Task<IResult> GetTraspasosAsync(Guid companyId, CancellationToken ct) =>
        GetConfigAsync(
            companyId,
            db.CompanyTraspasoConfigs,
            c => new TraspasoConfigDto { OnlyOwnVehicles = c.OnlyOwnVehicles },
            ct);

    public Task<IResult> PutTraspasosAsync(Guid companyId, TraspasoConfigDto dto, CancellationToken ct) =>
        PutConfigAsync(
            companyId,
            db.CompanyTraspasoConfigs,
            (c, now) =>
            {
                c.OnlyOwnVehicles = dto.OnlyOwnVehicles;
                c.UpdatedAt = now;
            },
            ct);

    public Task<IResult> GetSignaturesAsync(Guid companyId, CancellationToken ct) =>
        GetConfigAsync(
            companyId,
            db.CompanySignatureConfigs,
            c => new SignatureConfigDto
            {
                SellerSignatureType = c.SellerSignatureType,
                BuyerSignatureType = c.BuyerSignatureType,
                VaultEnabled = c.VaultEnabled,
                VaultSettings = c.VaultSettings
            },
            ct);

    public Task<IResult> PutSignaturesAsync(Guid companyId, SignatureConfigDto dto, CancellationToken ct) =>
        PutConfigAsync(
            companyId,
            db.CompanySignatureConfigs,
            (c, now) =>
            {
                c.SellerSignatureType = dto.SellerSignatureType;
                c.BuyerSignatureType = dto.BuyerSignatureType;
                c.VaultEnabled = dto.VaultEnabled;
                c.VaultSettings = dto.VaultSettings;
                c.UpdatedAt = now;
            },
            ct);

    public Task<IResult> GetNotificationsAsync(Guid companyId, CancellationToken ct) =>
        GetConfigAsync(
            companyId,
            db.CompanyNotificationConfigs,
            c => new NotificationConfigDto
            {
                Channel = c.Channel,
                NotificationTarget = c.NotificationTarget,
                ClientApiSettings = c.ClientApiSettings
            },
            ct);

    public Task<IResult> PutNotificationsAsync(Guid companyId, NotificationConfigDto dto, CancellationToken ct) =>
        PutConfigAsync(
            companyId,
            db.CompanyNotificationConfigs,
            (c, now) =>
            {
                c.Channel = dto.Channel;
                c.NotificationTarget = dto.NotificationTarget;
                c.ClientApiSettings = dto.ClientApiSettings;
                c.UpdatedAt = now;
            },
            ct);

    public Task<IResult> GetPaymentsAsync(Guid companyId, CancellationToken ct) =>
        GetConfigAsync(
            companyId,
            db.CompanyPaymentConfigs,
            c => new PaymentConfigDto
            {
                AllowFlitGateway = c.AllowFlitGateway,
                AllowOt = c.AllowOt,
                AllowOther = c.AllowOther
            },
            ct);

    public Task<IResult> PutPaymentsAsync(Guid companyId, PaymentConfigDto dto, CancellationToken ct) =>
        PutConfigAsync(
            companyId,
            db.CompanyPaymentConfigs,
            (c, now) =>
            {
                c.AllowFlitGateway = dto.AllowFlitGateway;
                c.AllowOt = dto.AllowOt;
                c.AllowOther = dto.AllowOther;
                c.UpdatedAt = now;
            },
            ct);

    public Task<IResult> GetRuntAsync(Guid companyId, CancellationToken ct) =>
        GetConfigAsync(
            companyId,
            db.CompanyRuntConfigs,
            c => new RuntConfigDto
            {
                PrimaryProvider = c.PrimaryProvider,
                SecondaryProvider = c.SecondaryProvider,
                FailoverTimeoutMs = c.FailoverTimeoutMs,
                ProviderCredentials = c.ProviderCredentials
            },
            ct);

    public Task<IResult> PutRuntAsync(Guid companyId, RuntConfigDto dto, CancellationToken ct) =>
        PutConfigAsync(
            companyId,
            db.CompanyRuntConfigs,
            (c, now) =>
            {
                c.PrimaryProvider = dto.PrimaryProvider;
                c.SecondaryProvider = dto.SecondaryProvider;
                c.FailoverTimeoutMs = dto.FailoverTimeoutMs;
                c.ProviderCredentials = dto.ProviderCredentials;
                c.UpdatedAt = now;
            },
            ct);

    private async Task<IResult> GetConfigAsync<TEntity, TDto>(
        Guid companyId,
        DbSet<TEntity> set,
        Func<TEntity, TDto> map,
        CancellationToken ct)
        where TEntity : class
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId, ct))
        {
            return NotFound();
        }

        var config = await set.SingleOrDefaultAsync(e => EF.Property<Guid>(e, "CompanyId") == companyId, ct);
        if (config is null)
        {
            return NotFound();
        }

        return Results.Ok(map(config));
    }

    private async Task<IResult> PutConfigAsync<TEntity>(
        Guid companyId,
        DbSet<TEntity> set,
        Action<TEntity, DateTimeOffset> apply,
        CancellationToken ct)
        where TEntity : class
    {
        if (!await db.Companies.AnyAsync(c => c.Id == companyId, ct))
        {
            return NotFound();
        }

        var config = await set.SingleOrDefaultAsync(e => EF.Property<Guid>(e, "CompanyId") == companyId, ct);
        if (config is null)
        {
            return NotFound();
        }

        apply(config, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static IResult NotFound() =>
        Results.Json(new { code = ApiErrorCodes.NotFound }, statusCode: StatusCodes.Status404NotFound);
}
