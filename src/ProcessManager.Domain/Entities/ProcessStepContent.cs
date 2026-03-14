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

    // ── Prompt block ──

    /// <summary>Prompt type (used when ContentType = Prompt).</summary>
    public PromptType? PromptType { get; set; }

    /// <summary>The question or instruction label shown to the operator.</summary>
    public string? Label { get; set; }

    /// <summary>Whether the operator must answer this prompt before the step can be completed.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Unit label displayed alongside numeric prompts (e.g., "mm", "rpm").</summary>
    public string? Units { get; set; }

    /// <summary>Soft lower bound for NumericEntry prompts.</summary>
    public decimal? MinValue { get; set; }

    /// <summary>Soft upper bound for NumericEntry prompts.</summary>
    public decimal? MaxValue { get; set; }

    /// <summary>JSON array of option strings for MultipleChoice prompts.</summary>
    public string? Choices { get; set; }

    // Navigation
    public ProcessStep ProcessStep { get; set; } = null!;
}
