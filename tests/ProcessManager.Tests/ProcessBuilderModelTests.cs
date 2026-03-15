using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Enums;
using ProcessManager.Web.Components.Pages.Processes;

namespace ProcessManager.Tests;

public class ProcessBuilderModelTests
{
    // ──────────── HELPERS ────────────

    private static PortResponseDto MakePort(PortDirection direction, int sortOrder = 0)
    {
        return new PortResponseDto(
            Id: Guid.NewGuid(),
            StepTemplateId: Guid.NewGuid(),
            Name: direction == PortDirection.Input ? "Part In" : "Part Out",
            Direction: direction,
            PortType: PortType.Material,
            KindId: null, KindCode: null, KindName: null,
            GradeId: null, GradeCode: null, GradeName: null,
            QtyRuleMode: null, QtyRuleN: null, QtyRuleMin: null, QtyRuleMax: null,
            DataType: null, Units: null, NominalValue: null,
            LowerTolerance: null, UpperTolerance: null,
            SortOrder: sortOrder,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);
    }

    private static StepTemplateResponseDto MakeTemplate(params PortResponseDto[] ports)
    {
        return new StepTemplateResponseDto(
            Id: Guid.NewGuid(),
            Code: "TST-001",
            Name: "Test Step",
            Description: null,
            Pattern: StepPattern.Transform,
            Version: 1,
            Status: "Active",
            IsActive: true,
            IsShared: false,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Ports: ports.ToList(),
            Images: new List<StepTemplateImageResponseDto>());
    }

    private static BuilderStep MakeBuilderStep(int sequence = 1, StepTemplateResponseDto? template = null)
    {
        return new BuilderStep
        {
            Sequence = sequence,
            StepTemplateId = template?.Id ?? Guid.NewGuid(),
            Template = template
        };
    }

    // ──────────── SequenceLinkModel ────────────

    [Fact]
    public void SequenceLinkModel_HasShapeIntersectionAnchors()
    {
        var source = new NodeModel(new Point(0, 0));
        var target = new NodeModel(new Point(300, 0));

        var link = new SequenceLinkModel(source, target);

        Assert.IsType<ShapeIntersectionAnchor>(link.Source);
        Assert.IsType<ShapeIntersectionAnchor>(link.Target);
        Assert.False(link.Segmentable);
    }

    // ──────────── BuilderStep ────────────

    [Fact]
    public void BuilderStep_Defaults()
    {
        var step = new BuilderStep();

        Assert.NotEqual(Guid.Empty, step.LocalId);
        Assert.Null(step.ServerId);
        Assert.Equal(0, step.Sequence);
        Assert.Equal(Guid.Empty, step.StepTemplateId);
        Assert.Null(step.Template);
        Assert.NotNull(step.PortOverrides);
        Assert.Empty(step.PortOverrides);
    }

    // ──────────── ProcessStepNodeModel ────────────

    [Fact]
    public void ProcessStepNodeModel_NullTemplate_NoPorts()
    {
        var step = MakeBuilderStep();
        var node = new ProcessStepNodeModel(step, Point.Zero);

        Assert.Empty(node.Ports);
        Assert.Empty(node.InputPorts);
        Assert.Empty(node.OutputPorts);
    }

    [Fact]
    public void ProcessStepNodeModel_WithTemplate_BuildsInputAndOutputPorts()
    {
        var template = MakeTemplate(
            MakePort(PortDirection.Input, sortOrder: 1),
            MakePort(PortDirection.Output, sortOrder: 1));
        var step = MakeBuilderStep(template: template);

        var node = new ProcessStepNodeModel(step, Point.Zero);

        Assert.Equal(2, node.Ports.Count);
        Assert.Single(node.InputPorts);
        Assert.Single(node.OutputPorts);

        var inputPort = node.InputPorts.First();
        var outputPort = node.OutputPorts.First();
        Assert.IsType<ProcessStepPortModel>(inputPort);
        Assert.IsType<ProcessStepPortModel>(outputPort);
        Assert.Equal(PortAlignment.Left, inputPort.Alignment);
        Assert.Equal(PortAlignment.Right, outputPort.Alignment);
    }

    // ──────────── ProcessStepPortModel ────────────

    [Fact]
    public void ProcessStepPortModel_ExposesPortData()
    {
        var portData = MakePort(PortDirection.Input);
        var parent = new NodeModel(Point.Zero);

        var port = new ProcessStepPortModel(parent, PortAlignment.Left, portData);

        Assert.Same(portData, port.PortData);
    }

    // ──────────── Sequence link generation logic ────────────

    [Fact]
    public void SequenceLinks_NSteps_CreatesNMinus1Links()
    {
        const int stepCount = 4;
        const double nodeWidth = 220;
        const double nodeGap = 80;

        var diagram = CreateDiagram();
        var nodes = new List<ProcessStepNodeModel>();

        for (int i = 0; i < stepCount; i++)
        {
            var step = MakeBuilderStep(sequence: i + 1);
            var node = new ProcessStepNodeModel(step, new Point(i * (nodeWidth + nodeGap), 80));
            diagram.Nodes.Add(node);
            nodes.Add(node);
        }

        // Mimic PopulateDiagram's sequence link creation
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var seq = new SequenceLinkModel(nodes[i], nodes[i + 1]);
            diagram.Links.Add(seq);
        }

        Assert.Equal(stepCount - 1, diagram.Links.Count);
        Assert.All(diagram.Links, link => Assert.IsType<SequenceLinkModel>(link));
    }

    [Fact]
    public void SequenceLinks_ZeroSteps_NoLinks()
    {
        var diagram = CreateDiagram();
        var nodes = diagram.Nodes.OfType<ProcessStepNodeModel>().ToList();

        for (int i = 0; i < nodes.Count - 1; i++)
            diagram.Links.Add(new SequenceLinkModel(nodes[i], nodes[i + 1]));

        Assert.Empty(diagram.Links);
    }

    [Fact]
    public void SequenceLinks_OneStep_NoLinks()
    {
        var diagram = CreateDiagram();
        var step = MakeBuilderStep(sequence: 1);
        diagram.Nodes.Add(new ProcessStepNodeModel(step, Point.Zero));

        var nodes = diagram.Nodes.OfType<ProcessStepNodeModel>().ToList();
        for (int i = 0; i < nodes.Count - 1; i++)
            diagram.Links.Add(new SequenceLinkModel(nodes[i], nodes[i + 1]));

        Assert.Empty(diagram.Links);
    }

    private static BlazorDiagram CreateDiagram()
    {
        var options = new BlazorDiagramOptions();
        options.Links.DefaultRouter = new NormalRouter();
        options.Links.DefaultPathGenerator = new StraightPathGenerator();
        return new BlazorDiagram(options);
    }
}
