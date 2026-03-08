using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Web.Components.Pages.Processes;

/// <summary>
/// A diagram link representing the implicit sequence order between two adjacent steps.
/// Rendered as a dashed grey arrow; not saved as a Flow and not user-selectable.
/// </summary>
public sealed class SequenceLinkModel : LinkModel
{
    public SequenceLinkModel(NodeModel source, NodeModel target)
        : base(new ShapeIntersectionAnchor(source), new ShapeIntersectionAnchor(target))
    {
        Segmentable = false;
    }
}

public sealed class BuilderStep
{
    public Guid LocalId { get; } = Guid.NewGuid();
    public Guid? ServerId { get; set; }
    public int Sequence { get; set; }
    public Guid StepTemplateId { get; set; }
    public StepTemplateResponseDto? Template { get; set; }
    public string? NameOverride { get; set; }
    public string? DescriptionOverride { get; set; }
    public StepPattern? PatternOverride { get; set; }
    public List<ProcessStepPortOverrideDto> PortOverrides { get; set; } = new();
}

/// <summary>
/// A diagram port linked to a specific port on a step template.
/// </summary>
public sealed class ProcessStepPortModel : PortModel
{
    public ProcessStepPortModel(NodeModel parent, PortAlignment alignment, PortResponseDto portData)
        : base(parent, alignment)
    {
        PortData = portData;
    }

    public PortResponseDto PortData { get; }
}

/// <summary>
/// A diagram node representing one step in the process being designed.
/// Ports are automatically created from the step's template port definitions.
/// </summary>
public sealed class ProcessStepNodeModel : NodeModel
{
    public ProcessStepNodeModel(BuilderStep step, Point position) : base(position)
    {
        Step = step;
        BuildPorts();
    }

    public BuilderStep Step { get; }

    public IEnumerable<ProcessStepPortModel> InputPorts =>
        Ports.OfType<ProcessStepPortModel>()
             .Where(p => p.PortData.Direction == PortDirection.Input)
             .OrderBy(p => p.PortData.SortOrder);

    public IEnumerable<ProcessStepPortModel> OutputPorts =>
        Ports.OfType<ProcessStepPortModel>()
             .Where(p => p.PortData.Direction == PortDirection.Output)
             .OrderBy(p => p.PortData.SortOrder);

    private void BuildPorts()
    {
        if (Step.Template is null) return;

        foreach (var p in Step.Template.Ports
                     .Where(p => p.Direction == PortDirection.Input)
                     .OrderBy(p => p.SortOrder))
            AddPort(new ProcessStepPortModel(this, PortAlignment.Left, p));

        foreach (var p in Step.Template.Ports
                     .Where(p => p.Direction == PortDirection.Output)
                     .OrderBy(p => p.SortOrder))
            AddPort(new ProcessStepPortModel(this, PortAlignment.Right, p));
    }
}
