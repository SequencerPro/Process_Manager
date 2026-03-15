namespace ProcessManager.Domain.Enums;

/// <summary>Lifecycle state of a Document Approval Request.</summary>
public enum DocumentApprovalStatus
{
    /// <summary>All approvers have been notified; waiting for decisions.</summary>
    Pending,

    /// <summary>All approvers Approved the document; Process has been Released.</summary>
    Approved,

    /// <summary>At least one approver Rejected; Process reverted to Draft.</summary>
    Rejected,

    /// <summary>The submitting author withdrew the request before all decisions were made.</summary>
    Withdrawn,
}
