using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A linear sequence of steps. Has exactly one entry and one exit point.
/// </summary>
public class Process : BaseEntity
{
    /// <summary>Short identifier (e.g., "WDG-MACH-01").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name (e.g., "Widget Machining").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Purpose and scope of this process.</summary>
    public string? Description { get; set; }

    /// <summary>Version number for change tracking.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Whether this process is available for use.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Formal lifecycle state — controls availability for new Jobs and edit permissions.</summary>
    public ProcessStatus Status { get; set; } = ProcessStatus.Draft;

    /// <summary>Document classification — controls which UI surfaces this process appears in and how it is governed.</summary>
    public ProcessRole ProcessRole { get; set; } = ProcessRole.ManufacturingProcess;

    /// <summary>FK to the ApprovalProcess-role Process that defines approval routing for this document type.</summary>
    public Guid? ApprovalProcessId { get; set; }

    /// <summary>Human-readable revision label alongside the integer Version (e.g. "A", "B", "1.0", "Rev 2").</summary>
    public string? RevisionCode { get; set; }

    /// <summary>Summary of what changed in this revision. Required before submitting for approval.</summary>
    public string? ChangeDescription { get; set; }

    /// <summary>When the released revision becomes effective. Defaults to approval timestamp if not set.</summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>The Process this revision was branched from (null for originals).</summary>
    public Guid? ParentProcessId { get; set; }

    // ── Training & Competency (Phase 16) ────────────────────────────────────

    /// <summary>Short label shown on competency records (e.g. "Press Brake Operation"). Defaults to Name.</summary>
    public string? CompetencyTitle { get; set; }

    /// <summary>Days before a competency record expires and re-training is required. Null = never expires.</summary>
    public int? CompetencyExpiryDays { get; set; }

    // Navigation properties
    public Process? ParentProcess { get; set; }
    public Process? ApprovalProcess { get; set; }
    public ICollection<ProcessStep> ProcessSteps { get; set; } = new List<ProcessStep>();
    public ICollection<Flow> Flows { get; set; } = new List<Flow>();
    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
    public ICollection<DocumentApprovalRequest> DocumentApprovalRequests { get; set; } = new List<DocumentApprovalRequest>();
}
