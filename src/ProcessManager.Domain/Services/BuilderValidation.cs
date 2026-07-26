namespace ProcessManager.Domain.Services;

/// <summary>
/// Shared contract for the Phase 36.2 builder validators (Process, Workflow,
/// StepTemplate). All validators return the same shape so the real-time
/// validation badge in the builder toolbar can render them uniformly.
/// </summary>
public interface IBuilderValidator<in T>
{
    BuilderValidationResult Validate(T target);
}

/// <summary>
/// Outcome of a builder validation pass. Errors block saving / releasing;
/// warnings are advisory.
/// </summary>
public sealed record BuilderValidationResult(
    IReadOnlyList<BuilderValidationIssue> Errors,
    IReadOnlyList<BuilderValidationIssue> Warnings)
{
    public bool IsValid => Errors.Count == 0;

    public int TotalIssues => Errors.Count + Warnings.Count;

    public static readonly BuilderValidationResult Empty = new(
        Array.Empty<BuilderValidationIssue>(),
        Array.Empty<BuilderValidationIssue>());
}

/// <summary>
/// A single validation finding. <see cref="NodeId"/> lets the UI scroll/zoom
/// the diagram to the offending node when the user clicks the issue.
/// </summary>
public sealed record BuilderValidationIssue(
    string RuleId,
    string Message,
    BuilderIssueSeverity Severity,
    Guid? NodeId = null);

public enum BuilderIssueSeverity
{
    Error,
    Warning
}
