namespace ProcessManager.Domain.Entities;

/// <summary>
/// Records an operator's answer to a StepPrompt (a Prompt-type content block)
/// during a specific StepExecution.
/// </summary>
public class PromptResponse : BaseEntity
{
    /// <summary>The StepExecution in which this response was captured.</summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Set when the prompt was defined on a ProcessStep's content blocks.
    /// Null when the prompt came from the StepTemplate.
    /// </summary>
    public Guid? ProcessStepContentId { get; set; }

    /// <summary>
    /// Set when the prompt was defined on a StepTemplate's content blocks.
    /// Null when the prompt came from the ProcessStep.
    /// </summary>
    public Guid? StepTemplateContentId { get; set; }

    /// <summary>
    /// String-normalised response value:
    ///   NumericEntry  → "12.43"
    ///   PassFail      → "Pass" | "Fail"
    ///   MultipleChoice→ the chosen option string
    ///   TextEntry     → free-form text
    ///   Checkbox      → "true" | "false"
    ///   Scan          → the scanned / typed code
    /// </summary>
    public string ResponseValue { get; set; } = string.Empty;

    /// <summary>True when a NumericEntry value violates the prompt's MinValue/MaxValue bounds.</summary>
    public bool IsOutOfRange { get; set; }

    /// <summary>Operator-provided reason for proceeding despite an out-of-range value.</summary>
    public string? OverrideNote { get; set; }

    /// <summary>When the response was submitted.</summary>
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public StepExecution StepExecution { get; set; } = null!;
    public ProcessStepContent? ProcessStepContent { get; set; }
    public StepTemplateContent? StepTemplateContent { get; set; }
}
