namespace ProcessManager.Domain.Entities;

/// <summary>
/// A file attachment (drawing, spec, form, etc.) associated with a Kind.
/// </summary>
public class KindDocument : BaseEntity
{
    public Guid KindId { get; set; }

    /// <summary>GUID-based filename stored on disk.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Original filename as uploaded by the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME type (e.g. application/pdf, image/png).</summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Optional display title for the document.</summary>
    public string? Title { get; set; }

    /// <summary>Display ordering.</summary>
    public int SortOrder { get; set; }

    // Navigation
    public Kind Kind { get; set; } = null!;
}
