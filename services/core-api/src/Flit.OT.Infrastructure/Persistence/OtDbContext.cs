using Flit.Identity.Infrastructure.Persistence.Entities;
using Flit.OT.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.OT.Infrastructure.Persistence;

public class OtDbContext(DbContextOptions<OtDbContext> options) : DbContext(options)
{
    public DbSet<OtProfile> OtProfiles => Set<OtProfile>();
    public DbSet<ProcedureTypeCatalog> ProcedureTypeCatalog => Set<ProcedureTypeCatalog>();
    public DbSet<DocumentTypeCatalog> DocumentTypeCatalog => Set<DocumentTypeCatalog>();
    public DbSet<ProcedureDocumentDefault> ProcedureDocumentDefaults => Set<ProcedureDocumentDefault>();
    public DbSet<OtDocumentOrderItem> OtDocumentOrderItems => Set<OtDocumentOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OtProfile>(e =>
        {
            e.ToTable("ot_profiles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TenantId).IsUnique();
            e.HasIndex(x => x.DivipolCode).IsUnique();
            e.HasIndex(x => x.UpdatedAt);
            e.HasOne(x => x.Tenant)
                .WithOne()
                .HasForeignKey<OtProfile>(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcedureTypeCatalog>(e =>
        {
            e.ToTable("procedure_type_catalog");
            e.HasKey(x => x.Code);
        });

        modelBuilder.Entity<DocumentTypeCatalog>(e =>
        {
            e.ToTable("document_type_catalog");
            e.HasKey(x => x.Code);
        });

        modelBuilder.Entity<ProcedureDocumentDefault>(e =>
        {
            e.ToTable("procedure_document_defaults");
            e.HasKey(x => new { x.ProcedureTypeCode, x.DocumentTypeCode });
            e.HasOne(x => x.ProcedureType)
                .WithMany(p => p.DocumentDefaults)
                .HasForeignKey(x => x.ProcedureTypeCode)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DocumentType)
                .WithMany(d => d.ProcedureDefaults)
                .HasForeignKey(x => x.DocumentTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OtDocumentOrderItem>(e =>
        {
            e.ToTable("ot_document_order_items");
            e.HasKey(x => new { x.TenantId, x.ProcedureTypeCode, x.DocumentTypeCode });
            e.HasIndex(x => new { x.TenantId, x.ProcedureTypeCode, x.Position });
            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ProcedureType)
                .WithMany(p => p.DocumentOrderItems)
                .HasForeignKey(x => x.ProcedureTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DocumentType)
                .WithMany(d => d.DocumentOrderItems)
                .HasForeignKey(x => x.DocumentTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Roles);
            e.Ignore(x => x.Users);
        });
    }
}
