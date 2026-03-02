namespace ProcessManager.Domain.Entities;

/// <summary>
/// A reference image attached to a ProcessStep instance (e.g. process-specific work photo).
/// The actual file is stored on the API filesystem; this entity holds the metadata.
/// </summary>
public class ProcessStepImage : BaseEntity
{
    public Guid ProcessStepId { get; set; }

    /// <summary>Stored filename on disk ({Guid}.{ext}).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Original filename as uploaded by the user.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME type (e.g. image/jpeg).</summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Display order within this process step's image gallery.</summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public ProcessStep ProcessStep { get; set; } = null!;
}
