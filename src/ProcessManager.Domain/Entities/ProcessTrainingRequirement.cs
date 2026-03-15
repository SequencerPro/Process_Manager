using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Declares that a Process or StepTemplate requires the assigned operator to hold a Current
/// CompetencyRecord in the specified training process before a job can be started.
/// </summary>
public class ProcessTrainingRequirement : BaseEntity
{
    /// <summary>Whether this requirement applies to a Process or a StepTemplate.</summary>
    public TrainingRequirementSubjectType SubjectType { get; set; } = TrainingRequirementSubjectType.Process;

    /// <summary>The ID of the Process or StepTemplate that enforces this requirement.</summary>
    public Guid SubjectEntityId { get; set; }

    /// <summary>FK to the Training-role Process that must be completed.</summary>
    public Guid RequiredTrainingProcessId { get; set; }

    /// <summary>When true, job creation is blocked if the operator lacks this competency.
    /// When false, a warning is shown but the job can still be created.</summary>
    public bool IsEnforced { get; set; } = true;

    // Navigation
    public Process? RequiredTrainingProcess { get; set; }
}
