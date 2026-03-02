using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A tracked, homogeneous group of items.
/// The batch carries a Grade that all member items inherit.
/// </summary>
public class Batch : BaseEntity
{
    /// <summary>Batch identifier (e.g., "LOT-2026-042").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>All items in this batch are this Kind.</summary>
    public Guid KindId { get; set; }

    /// <summary>Batch-level grade (items inherit).</summary>
    public Guid GradeId { get; set; }

    /// <summary>The Job this batch belongs to.</summary>
    public Guid JobId { get; set; }

    /// <summary>Count of items (for non-serialized Kinds).</summary>
    public int Quantity { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public BatchStatus Status { get; set; } = BatchStatus.Open;

    // Navigation properties
    public Kind Kind { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<PortTransaction> PortTransactions { get; set; } = new List<PortTransaction>();
    public ICollection<ExecutionData> ExecutionData { get; set; } = new List<ExecutionData>();
}
