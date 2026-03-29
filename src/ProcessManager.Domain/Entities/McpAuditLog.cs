namespace ProcessManager.Domain.Entities;

/// <summary>
/// Append-only log of every MCP tool invocation.
/// Does NOT extend BaseEntity — manages its own Id and timestamps
/// to avoid the SetAuditFields override in DbContext.
/// </summary>
public class McpAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>MCP tool name, e.g. "create_nonconformance", "list_processes".</summary>
    public string ToolName { get; set; } = "";

    /// <summary>Classified action: Read, Create, Update, Delete.</summary>
    public string Action { get; set; } = "Read";

    /// <summary>Entity type affected, e.g. "NonConformance", "Job". Null for read-only tools.</summary>
    public string? EntityType { get; set; }

    /// <summary>Primary key of the affected entity. Null for read-only or failed calls.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>JWT user ID of the caller. Null for anonymous tools (describe_domain).</summary>
    public string? UserId { get; set; }

    /// <summary>Display name of the caller.</summary>
    public string? UserDisplayName { get; set; }

    /// <summary>Full JSON-RPC arguments (raw JSON). Stored for reconstruction/debugging.</summary>
    public string? RequestPayload { get; set; }

    /// <summary>Truncated response text (first 500 chars) for quick review.</summary>
    public string? ResponseSummary { get; set; }

    /// <summary>Whether the tool call succeeded.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Error message if the call failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Wall-clock duration of the tool call in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>UTC timestamp when the tool was invoked.</summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
