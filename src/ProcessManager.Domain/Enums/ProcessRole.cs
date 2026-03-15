namespace ProcessManager.Domain.Enums;

/// <summary>
/// Document classification for a Process — controls which UI surfaces the process appears in
/// and how it is governed (approval routing, version control, execution queues).
/// This is orthogonal to execution capability: a process can be executable AND version-controlled.
/// </summary>
public enum ProcessRole
{
    /// <summary>Standard manufacturing or operational procedure. Appears in the Create Job UI.</summary>
    ManufacturingProcess,

    /// <summary>
    /// Defines an approval routing template. Contains parallel approval step templates.
    /// Does NOT appear in the Create Job UI — triggered only by the Submit for Approval flow.
    /// </summary>
    ApprovalProcess,

    /// <summary>ISO 9001 QMS document (procedure, policy). Subject to formal approval routing and revision control.</summary>
    QmsDocument,

    /// <summary>Work instruction. Subject to formal revision control. May reference manufacturing steps.</summary>
    WorkInstruction,

    /// <summary>Training process. Executed via the ExecutionWizard for competency delivery and assessment.</summary>
    Training,
}
