using Flit.Procedures.Shared.Domain;

namespace Flit.Procedures.Infrastructure.Persistence.Entities;

public class ProcedureInstanceDocument
{
    public Guid Id { get; set; }
    public Guid ProcedureInstanceId { get; set; }
    public string Label { get; set; } = "";
    public DocumentKind Kind { get; set; }
    public string StoragePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public ProcedureInstance ProcedureInstance { get; set; } = null!;
}
