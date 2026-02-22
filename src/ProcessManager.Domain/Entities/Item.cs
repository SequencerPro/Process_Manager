using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// A specific instance of a Kind that flows through the process.
/// Serialized items have unique serial numbers.
/// </summary>
public class Item : BaseEntity
{
    /// <summary>Unique serial number for serialized items.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>What this item is.</summary>
    public Guid KindId { get; set; }

    /// <summary>Current condition/qualification.</summary>
    public Guid GradeId { get; set; }

    /// <summary>The Job this item belongs to.</summary>
    public Guid JobId { get; set; }

    /// <summary>Optional batch membership.</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public ItemStatus Status { get; set; } = ItemStatus.Available;

    // Navigation properties
    public Kind Kind { get; set; } = null!;
    public Grade Grade { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Batch? Batch { get; set; }
    public ICollection<PortTransaction> PortTransactions { get; set; } = new List<PortTransaction>();
    public ICollection<ExecutionData> ExecutionData { get; set; } = new List<ExecutionData>();
}
