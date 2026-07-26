namespace ProcessManager.Domain.Entities;

/// <summary>
/// Tracks a tenant's progress through the first-run onboarding wizard (M2).
/// Exactly one row per tenant. Persisted so a user who drops out mid-wizard
/// can resume where they left off on next login.
/// </summary>
public class TenantOnboardingState : BaseEntity
{
    /// <summary>Industry selected at signup. Drives vocabulary and sample process generation.</summary>
    public OnboardingIndustry Industry { get; set; } = OnboardingIndustry.General;

    /// <summary>Current wizard step (0 = welcome, 1 = first kind, 2 = first step, 3 = first process, 4 = first job).</summary>
    public int CurrentStep { get; set; }

    /// <summary>When the wizard was completed (null while in progress).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>When the user skipped onboarding (null if not skipped). Sample content still seeded on skip.</summary>
    public DateTime? SkippedAt { get; set; }

    /// <summary>Id of the first Kind created in the wizard (or the sample Kind when skipped).</summary>
    public Guid? FirstKindId { get; set; }

    /// <summary>Id of the first StepTemplate created in the wizard.</summary>
    public Guid? FirstStepTemplateId { get; set; }

    /// <summary>Id of the first Process created in the wizard.</summary>
    public Guid? FirstProcessId { get; set; }

    /// <summary>Id of the first Job launched from the wizard.</summary>
    public Guid? FirstJobId { get; set; }

    /// <summary>Millisecond timestamp captured when the tenant signs up. Used to compute time-to-first-job telemetry.</summary>
    public DateTime? SignupAt { get; set; }

    /// <summary>Timestamp of the first Job completion — for the "signup → first completed job" funnel.</summary>
    public DateTime? FirstJobCompletedAt { get; set; }
}

public enum OnboardingIndustry
{
    General,
    CNC,
    PCBA,
    Medical
}
