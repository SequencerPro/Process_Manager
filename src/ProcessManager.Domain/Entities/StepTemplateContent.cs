using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// An ordered content block (text paragraph or image) attached to a StepTemplate,
/// providing default work instructions that operators see whenever this template is used.
/// </summary>
public class StepTemplateContent : BaseEntity
{
    public Guid StepTemplateId { get; set; }

    /// <summary>Whether this block is a text paragraph or an image.</summary>
    public StepContentType ContentType { get; set; }

    /// <summary>Display order among all content blocks of this template (0-based, gaps allowed).</summary>
    public int SortOrder { get; set; }

    // ── Text block ──

    /// <summary>Paragraph text (used when ContentType = Text).</summary>
    public string? Body { get; set; }

    // ── Image block ──

    /// <summary>GUID-based file name on disk (used when ContentType = Image).</summary>
    public string? FileName { get; set; }

    /// <summary>Original name provided by the uploader.</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>MIME type of the uploaded image.</summary>
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

    // ── Phase 8a fields ──

    /// <summary>
    /// The StepTemplate version in which this block was first added or last substantively modified.
    /// Used by the ExecutionWizard to highlight content that changed since the previous release.
    /// </summary>
    public int IntroducedInVersion { get; set; } = 1;

    /// <summary>
    /// Classifies the purpose of this content block (Setup/Safety/Inspection/Reference/Note).
    /// Drives wizard phase assignment and maturity scoring.
    /// </summary>
    public ContentCategory? ContentCategory { get; set; }

    /// <summary>
    /// When true the operator must explicitly acknowledge this block before the wizard advances.
    /// Automatically true for Safety blocks; can be set manually on any block type.
    /// </summary>
    public bool AcknowledgmentRequired { get; set; }

    /// <summary>Target (nominal) value for NumericEntry prompts. Shown alongside LSL/USL in the wizard.</summary>
    public decimal? NominalValue { get; set; }

    /// <summary>
    /// When true, an out-of-spec NumericEntry response or a PassFail=Fail response blocks step
    /// sign-off and opens the non-conformance disposition modal.
    /// </summary>
    public bool IsHardLimit { get; set; }

    // Navigation
    public StepTemplate StepTemplate { get; set; } = null!;
}
