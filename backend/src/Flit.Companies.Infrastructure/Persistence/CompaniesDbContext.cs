using Flit.Companies.Infrastructure.Persistence.Entities;
using Flit.Identity.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.Companies.Infrastructure.Persistence;

public class CompaniesDbContext(DbContextOptions<CompaniesDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMatriculaConfig> CompanyMatriculaConfigs => Set<CompanyMatriculaConfig>();
    public DbSet<CompanyTraspasoConfig> CompanyTraspasoConfigs => Set<CompanyTraspasoConfig>();
    public DbSet<TenantUserException> TenantUserExceptions => Set<TenantUserException>();
    public DbSet<CompanySignatureConfig> CompanySignatureConfigs => Set<CompanySignatureConfig>();
    public DbSet<CompanyNotificationConfig> CompanyNotificationConfigs => Set<CompanyNotificationConfig>();
    public DbSet<CompanyPaymentConfig> CompanyPaymentConfigs => Set<CompanyPaymentConfig>();
    public DbSet<CompanyRuntConfig> CompanyRuntConfigs => Set<CompanyRuntConfig>();
    public DbSet<TrafficAuthority> TrafficAuthorities => Set<TrafficAuthority>();
    public DbSet<CompanyTrafficAuthorityMatrix> CompanyTrafficAuthorityMatrix => Set<CompanyTrafficAuthorityMatrix>();
    public DbSet<RuntProviderCatalog> RuntProviderCatalog => Set<RuntProviderCatalog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("companies");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TenantId).IsUnique();
            e.HasIndex(x => x.Nit).IsUnique();
            e.HasIndex(x => x.LegalName);
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Tenant)
                .WithOne()
                .HasForeignKey<Company>(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanyMatriculaConfig>(e =>
        {
            e.ToTable("company_matricula_config");
            e.HasKey(x => x.CompanyId);
            e.HasOne(x => x.Company).WithOne(c => c.MatriculaConfig).HasForeignKey<CompanyMatriculaConfig>(x => x.CompanyId);
        });

        modelBuilder.Entity<CompanyTraspasoConfig>(e =>
        {
            e.ToTable("company_traspaso_config");
            e.HasKey(x => x.CompanyId);
            e.HasOne(x => x.Company).WithOne(c => c.TraspasoConfig).HasForeignKey<CompanyTraspasoConfig>(x => x.CompanyId);
        });

        modelBuilder.Entity<TenantUserException>(e =>
        {
            e.ToTable("tenant_user_exceptions");
            e.HasKey(x => new { x.TenantId, x.UserId });
            e.HasIndex(x => x.TenantId);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanySignatureConfig>(e =>
        {
            e.ToTable("company_signature_config");
            e.HasKey(x => x.CompanyId);
            e.Property(x => x.VaultSettings).HasColumnType("jsonb");
            e.HasOne(x => x.Company).WithOne(c => c.SignatureConfig).HasForeignKey<CompanySignatureConfig>(x => x.CompanyId);
        });

        modelBuilder.Entity<CompanyNotificationConfig>(e =>
        {
            e.ToTable("company_notification_config");
            e.HasKey(x => x.CompanyId);
            e.Property(x => x.ClientApiSettings).HasColumnType("jsonb");
            e.HasOne(x => x.Company).WithOne(c => c.NotificationConfig).HasForeignKey<CompanyNotificationConfig>(x => x.CompanyId);
        });

        modelBuilder.Entity<CompanyPaymentConfig>(e =>
        {
            e.ToTable("company_payment_config");
            e.HasKey(x => x.CompanyId);
            e.HasOne(x => x.Company).WithOne(c => c.PaymentConfig).HasForeignKey<CompanyPaymentConfig>(x => x.CompanyId);
        });

        modelBuilder.Entity<CompanyRuntConfig>(e =>
        {
            e.ToTable("company_runt_config");
            e.HasKey(x => x.CompanyId);
            e.Property(x => x.ProviderCredentials).HasColumnType("jsonb");
            e.HasOne(x => x.Company).WithOne(c => c.RuntConfig).HasForeignKey<CompanyRuntConfig>(x => x.CompanyId);
        });

        modelBuilder.Entity<TrafficAuthority>(e =>
        {
            e.ToTable("traffic_authorities");
            e.HasKey(x => x.Code);
        });

        modelBuilder.Entity<CompanyTrafficAuthorityMatrix>(e =>
        {
            e.ToTable("company_traffic_authority_matrix");
            e.HasKey(x => new { x.CompanyId, x.AuthorityCode });
            e.HasIndex(x => new { x.CompanyId, x.IsEnabled });
            e.HasOne(x => x.Company).WithMany(c => c.TrafficAuthorityMatrix).HasForeignKey(x => x.CompanyId);
            e.HasOne(x => x.Authority).WithMany(a => a.CompanyMatrix).HasForeignKey(x => x.AuthorityCode);
        });

        modelBuilder.Entity<RuntProviderCatalog>(e =>
        {
            e.ToTable("runt_provider_catalog");
            e.HasKey(x => x.Id);
        });

        // Identity tables referenced by FK — exclude from Companies migrations.
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Roles);
            e.Ignore(x => x.Users);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Tenant);
            e.Ignore(x => x.UserRoles);
        });
    }
}
