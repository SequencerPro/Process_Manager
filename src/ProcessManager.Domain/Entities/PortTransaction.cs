namespace ProcessManager.Domain.Entities;

/// <summary>
/// Records an item or batch flowing through a specific port during a step execution.
/// This is the core traceability record.
/// </summary>
public class PortTransaction : BaseEntity
{
    /// <summary>The step execution this occurred during.</summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>Which port the item/batch flowed through.</summary>
    public Guid PortId { get; set; }

    /// <summary>The specific item (for serialized tracking).</summary>
    public Guid? ItemId { get; set; }

    /// <summary>The batch (for batch tracking).</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Count (for untracked items or batch quantity).</summary>
    public int Quantity { get; set; } = 1;

    // Navigation properties
    public StepExecution StepExecution { get; set; } = null!;
    public Port Port { get; set; } = null!;
    public Item? Item { get; set; }
    public Batch? Batch { get; set; }
}
