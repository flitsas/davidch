using Flit.Procedures.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Infrastructure.Persistence;

public class ProceduresDbContext(DbContextOptions<ProceduresDbContext> options) : DbContext(options)
{
    public DbSet<ProcedureType> ProcedureTypes => Set<ProcedureType>();
    public DbSet<ProcedureTypeActor> ProcedureTypeActors => Set<ProcedureTypeActor>();
    public DbSet<ProcedureTypeDocument> ProcedureTypeDocuments => Set<ProcedureTypeDocument>();
    public DbSet<ProcedureInstance> ProcedureInstances => Set<ProcedureInstance>();
    public DbSet<ProcedureInstanceActor> ProcedureInstanceActors => Set<ProcedureInstanceActor>();
    public DbSet<ProcedureInstanceDocument> ProcedureInstanceDocuments => Set<ProcedureInstanceDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcedureType>(e =>
        {
            e.ToTable("procedure_types");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.UpdatedAt);
        });

        modelBuilder.Entity<ProcedureTypeActor>(e =>
        {
            e.ToTable("procedure_type_actors");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProcedureTypeId, x.SortOrder });
            e.HasOne(x => x.ProcedureType)
                .WithMany(p => p.Actors)
                .HasForeignKey(x => x.ProcedureTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcedureTypeDocument>(e =>
        {
            e.ToTable("procedure_type_documents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProcedureTypeId, x.SortOrder });
            e.HasOne(x => x.ProcedureType)
                .WithMany(p => p.Documents)
                .HasForeignKey(x => x.ProcedureTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcedureInstance>(e =>
        {
            e.ToTable("procedure_instances");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.CreatedAt });
            e.HasOne(x => x.ProcedureType)
                .WithMany()
                .HasForeignKey(x => x.ProcedureTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcedureInstanceActor>(e =>
        {
            e.ToTable("procedure_instance_actors");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProcedureInstanceId, x.SortOrder });
            e.HasOne(x => x.ProcedureInstance)
                .WithMany(i => i.Actors)
                .HasForeignKey(x => x.ProcedureInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ParentActor)
                .WithMany()
                .HasForeignKey(x => x.ParentActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcedureInstanceDocument>(e =>
        {
            e.ToTable("procedure_instance_documents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProcedureInstanceId, x.Label }).IsUnique();
            e.HasOne(x => x.ProcedureInstance)
                .WithMany(i => i.Documents)
                .HasForeignKey(x => x.ProcedureInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
