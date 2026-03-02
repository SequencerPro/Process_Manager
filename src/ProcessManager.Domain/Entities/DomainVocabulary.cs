namespace ProcessManager.Domain.Entities;

/// <summary>
/// Maps system terms to domain-specific labels.
/// </summary>
public class DomainVocabulary : BaseEntity
{
    /// <summary>Vocabulary name (e.g., "Semiconductor", "General Manufacturing").</summary>
    public string Name { get; set; } = string.Empty;

    public string TermKind { get; set; } = "Kind";
    public string TermKindCode { get; set; } = "Kind Code";
    public string TermGrade { get; set; } = "Grade";
    public string TermItem { get; set; } = "Item";
    public string TermItemId { get; set; } = "Item ID";
    public string TermBatch { get; set; } = "Batch";
    public string TermBatchId { get; set; } = "Batch ID";
    public string TermJob { get; set; } = "Job";
    public string TermWorkflow { get; set; } = "Workflow";
    public string TermProcess { get; set; } = "Process";
    public string TermStep { get; set; } = "Step";
}
