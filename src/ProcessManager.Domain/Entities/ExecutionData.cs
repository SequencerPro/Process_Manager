using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Entities;

/// <summary>
/// Key-value data captured during execution.
/// Associated at one of three levels: Step Execution, Batch, or Item.
/// </summary>
public class ExecutionData : BaseEntity
{
    /// <summary>Data field name (e.g., "Temperature").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Data value stored as string.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>How to interpret the value.</summary>
    public DataValueType DataType { get; set; } = DataValueType.String;

    /// <summary>Unit (e.g., "mm", "°C", "psi").</summary>
    public string? UnitOfMeasure { get; set; }

    // --- Association level (exactly one must be non-null) ---

    /// <summary>Association level 1: step-wide data.</summary>
    public Guid? StepExecutionId { get; set; }

    /// <summary>Association level 2: batch-level data.</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Association level 3: item-level data.</summary>
    public Guid? ItemId { get; set; }

    // Navigation properties
    public StepExecution? StepExecution { get; set; }
    public Batch? Batch { get; set; }
    public Item? Item { get; set; }
}
