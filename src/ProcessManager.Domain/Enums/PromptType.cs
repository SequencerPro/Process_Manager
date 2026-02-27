namespace ProcessManager.Domain.Enums;

/// <summary>
/// Discriminates how an operator is expected to respond to a StepPrompt.
/// </summary>
public enum PromptType
{
    /// <summary>Operator enters a numeric value (decimal). Supports optional unit label and soft min/max bounds.</summary>
    NumericEntry,

    /// <summary>Binary outcome — Pass or Fail.</summary>
    PassFail,

    /// <summary>Engineer supplies a fixed list of options; operator picks exactly one.</summary>
    MultipleChoice,

    /// <summary>Free-form text entry by the operator.</summary>
    TextEntry,

    /// <summary>Single boolean tick-box (e.g., "Coolant reservoir filled").</summary>
    Checkbox,

    /// <summary>Barcode / serial number scan or manual typed entry.</summary>
    Scan
}
