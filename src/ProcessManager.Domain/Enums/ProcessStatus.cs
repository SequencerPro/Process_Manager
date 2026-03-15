namespace ProcessManager.Domain.Enums;

/// <summary>
/// Lifecycle state of a Process or StepTemplate design artefact.
/// </summary>
public enum ProcessStatus
{
    /// <summary>Being authored or revised — not available for new Jobs.</summary>
    Draft,

    /// <summary>Submitted for review — locked against further edits.</summary>
    PendingApproval,

    /// <summary>Approved and active — available for new Jobs; immutable until a new revision is created.</summary>
    Released,

    /// <summary>Replaced by a newer Released version — existing Jobs in execution may continue.</summary>
    Superseded,

    /// <summary>Withdrawn from use — no new Jobs permitted.</summary>
    Retired
}
