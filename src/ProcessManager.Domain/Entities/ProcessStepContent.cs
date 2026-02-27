using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An ordered content block (text paragraph or image) attached to a ProcessStep,
/// providing work instructions for the operator who executes this step.
/// </summary>
public class ProcessStepContent : BaseEntity
{
    public Guid ProcessStepId { get; set; }

    /// <summary>Whether this block is a text paragraph or an image.</summary>
    public StepContentType ContentType { get; set; }

    /// <summary>Display order among all content blocks of this step (0-based, gaps allowed).</summary>
    public int SortOrder { get; set; }

    // ── Text block ──

    /// <summary>Paragraph text (used when ContentType = Text).</summary>
    public string? Body { get; set; }

    // ── Image block ──

    /// <summary>GUID-based file name on disk (used when ContentType = Image).</summary>
    public string? FileName { get; set; }

    /// <summary>Original name provided by the uploader.</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>MIME type (e.g., "image/jpeg").</summary>
    public string? MimeType { get; set; }

    // Navigation
    public ProcessStep ProcessStep { get; set; } = null!;
}
