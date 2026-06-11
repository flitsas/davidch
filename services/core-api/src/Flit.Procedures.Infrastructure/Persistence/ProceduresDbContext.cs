using Flit.Procedures.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flit.Procedures.Infrastructure.Persistence;

public class ProceduresDbContext(DbContextOptions<ProceduresDbContext> options) : DbContext(options)
{
    public DbSet<ProcedureType> ProcedureTypes => Set<ProcedureType>();
    public DbSet<ProcedureTypeActor> ProcedureTypeActors => Set<ProcedureTypeActor>();
    public DbSet<ProcedureTypeDocument> ProcedureTypeDocuments => Set<ProcedureTypeDocument>();

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
    }
}
