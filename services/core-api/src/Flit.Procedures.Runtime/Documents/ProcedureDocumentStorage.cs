namespace Flit.Procedures.Runtime.Documents;

public sealed class ProcedureDocumentStorage
{
    private readonly string _basePath;

    public ProcedureDocumentStorage(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Document storage base path is required.", nameof(basePath));
        }

        _basePath = Path.GetFullPath(basePath);
    }

    public async Task<StoredDocumentResult> SaveAsync(
        Guid tenantId,
        Guid instanceId,
        string label,
        Stream content,
        string fileName,
        CancellationToken ct)
    {
        var safeLabel = SanitizeLabel(label);
        var directory = Path.Combine(_basePath, tenantId.ToString("N"), instanceId.ToString("N"));
        Directory.CreateDirectory(directory);

        var storagePath = Path.Combine(directory, $"{safeLabel}.pdf");
        await using var fileStream = new FileStream(
            storagePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(fileStream, ct);
        var fileSize = fileStream.Length;

        return new StoredDocumentResult(storagePath, fileSize);
    }

    private static string SanitizeLabel(string label)
    {
        var chars = label.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var sanitized = new string(chars);
        return string.IsNullOrEmpty(sanitized) ? "document" : sanitized;
    }
}

public sealed record StoredDocumentResult(string StoragePath, long FileSizeBytes);
