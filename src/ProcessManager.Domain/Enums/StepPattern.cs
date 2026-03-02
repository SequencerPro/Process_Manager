namespace ProcessManager.Domain.Enums;

/// <summary>
/// Classifies a StepTemplate by its port configuration pattern.
/// </summary>
public enum StepPattern
{
    /// <summary>1 input port, 1 output port.</summary>
    Transform,

    /// <summary>2+ input ports, 1 output port.</summary>
    Assembly,

    /// <summary>1 input port, 2+ output ports.</summary>
    Division,

    /// <summary>Any number of input and output ports.</summary>
    General
}
