using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Companies.Shared.Domain;

namespace Flit.Companies.Infrastructure.Persistence;

public static class CompanyDefaultConfigFactory
{
    public static void SeedDefaults(
        Company company,
        IReadOnlyList<string> authorityCodes,
        DateTimeOffset now)
    {
        company.MatriculaConfig = new CompanyMatriculaConfig
        {
            CompanyId = company.Id,
            AllowNewVehicleFiling = false,
            AllowMiscProcedures = false,
            UpdatedAt = now
        };

        company.TraspasoConfig = new CompanyTraspasoConfig
        {
            CompanyId = company.Id,
            OnlyOwnVehicles = false,
            UpdatedAt = now
        };

        company.SignatureConfig = new CompanySignatureConfig
        {
            CompanyId = company.Id,
            SellerSignatureType = SignatureType.OnScreen,
            BuyerSignatureType = SignatureType.OnScreen,
            VaultEnabled = false,
            UpdatedAt = now
        };

        company.NotificationConfig = new CompanyNotificationConfig
        {
            CompanyId = company.Id,
            Channel = NotificationChannel.FlitSmtp,
            NotificationTarget = NotificationTarget.Filer,
            UpdatedAt = now
        };

        company.PaymentConfig = new CompanyPaymentConfig
        {
            CompanyId = company.Id,
            AllowFlitGateway = false,
            AllowOt = false,
            AllowOther = false,
            UpdatedAt = now
        };

        company.RuntConfig = new CompanyRuntConfig
        {
            CompanyId = company.Id,
            PrimaryProvider = RuntProvider.Verifik,
            SecondaryProvider = RuntProvider.Intempo,
            FailoverTimeoutMs = 4000,
            ProviderCredentials = "{}",
            UpdatedAt = now
        };

        company.TrafficAuthorityMatrix = authorityCodes
            .Select(code => new CompanyTrafficAuthorityMatrix
            {
                CompanyId = company.Id,
                AuthorityCode = code,
                IsEnabled = false
            })
            .ToList();
    }
}
