using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Pure-domain unit tests for <see cref="ProcessStructuralValidator"/>
/// (Phase 36.2). No DB, no HTTP.
/// </summary>
public class ProcessStructuralValidatorTests
{
    // ─────────────── Helpers ───────────────

    private static Port MakePort(PortDirection dir, PortType type = PortType.Material,
        Guid? kindId = null, Guid? gradeId = null, DataValueType? dataType = null,
        string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? (dir == PortDirection.Input ? "in" : "out"),
            Direction = dir,
            PortType = type,
            KindId = kindId ?? (type == PortType.Material ? Guid.NewGuid() : (Guid?)null),
            GradeId = gradeId ?? (type == PortType.Material ? Guid.NewGuid() : (Guid?)null),
            QtyRuleMode = type == PortType.Material ? QuantityRuleMode.Exactly : (QuantityRuleMode?)null,
            DataType = dataType,
        };

    private static (ProcessStep step, Port input, Port output) MakeTransformStep(int sequence)
    {
        var input = MakePort(PortDirection.Input);
        var output = MakePort(PortDirection.Output);
        var tmpl = new StepTemplate
        {
            Id = Guid.NewGuid(),
            Pattern = StepPattern.Transform,
            Ports = new List<Port> { input, output },
        };
        var step = new ProcessStep
        {
            Id = Guid.NewGuid(),
            Sequence = sequence,
            StepTemplate = tmpl,
            StepTemplateId = tmpl.Id,
        };
        return (step, input, output);
    }

    // ─────────────── Tests ───────────────

    [Fact]
    public void Empty_process_reports_no_steps_error()
    {
        var p = new Process { Id = Guid.NewGuid() };
        var result = ProcessStructuralValidator.Validate(p);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == "R-P01");
    }

    [Fact]
    public void Single_step_passes_validation()
    {
        var (step, _, _) = MakeTransformStep(1);
        var p = new Process { ProcessSteps = new List<ProcessStep> { step } };

        var result = ProcessStructuralValidator.Validate(p);

        Assert.True(result.IsValid, "errors: " + string.Join("; ", result.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Noncontiguous_sequence_is_error()
    {
        var (s1, _, _) = MakeTransformStep(1);
        var (s3, _, _) = MakeTransformStep(3); // gap
        var p = new Process { ProcessSteps = new List<ProcessStep> { s1, s3 } };

        var result = ProcessStructuralValidator.Validate(p);

        Assert.Contains(result.Errors, e => e.RuleId == "R-P02");
    }

    [Fact]
    public void Flow_referencing_unknown_step_is_error()
    {
        var (s1, _, out1) = MakeTransformStep(1);
        var (s2, in2, _) = MakeTransformStep(2);
        var p = new Process
        {
            ProcessSteps = new List<ProcessStep> { s1, s2 },
            Flows = new List<Flow>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceProcessStepId = Guid.NewGuid(), // not a real step
                    SourcePortId = out1.Id,
                    TargetProcessStepId = s2.Id,
                    TargetPortId = in2.Id,
                }
            },
        };

        var result = ProcessStructuralValidator.Validate(p);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == "R-P03");
    }

    [Fact]
    public void Flow_with_wrong_direction_port_is_error()
    {
        var (s1, in1, out1) = MakeTransformStep(1);
        var (s2, in2, _) = MakeTransformStep(2);
        var p = new Process
        {
            ProcessSteps = new List<ProcessStep> { s1, s2 },
            Flows = new List<Flow>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceProcessStepId = s1.Id,
                    SourcePortId = in1.Id, // input used as source — invalid
                    TargetProcessStepId = s2.Id,
                    TargetPortId = in2.Id,
                }
            },
        };

        var result = ProcessStructuralValidator.Validate(p);

        Assert.Contains(result.Errors, e => e.RuleId == "R-P03");
    }

    [Fact]
    public void Material_flow_with_mismatched_kinds_is_warning_not_error()
    {
        var (s1, _, out1) = MakeTransformStep(1);
        var (s2, in2, _) = MakeTransformStep(2);
        // Force a different kind on the target input port.
        in2.KindId = Guid.NewGuid();

        var p = new Process
        {
            ProcessSteps = new List<ProcessStep> { s1, s2 },
            Flows = new List<Flow>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SourceProcessStepId = s1.Id,
                    SourcePortId = out1.Id,
                    TargetProcessStepId = s2.Id,
                    TargetPortId = in2.Id,
                }
            },
        };

        var result = ProcessStructuralValidator.Validate(p);

        Assert.True(result.IsValid, "kind mismatch should be a warning, not an error");
        Assert.Contains(result.Warnings, w => w.RuleId == "R-P04");
    }

    [Fact]
    public void Step_with_no_incoming_flow_yields_warning()
    {
        var (s1, _, _) = MakeTransformStep(1);
        var (s2, _, _) = MakeTransformStep(2); // no incoming flow
        var p = new Process { ProcessSteps = new List<ProcessStep> { s1, s2 } };

        var result = ProcessStructuralValidator.Validate(p);

        Assert.Contains(result.Warnings, w => w.RuleId == "R-P05");
    }

    [Fact]
    public void Null_process_throws()
    {
        Assert.Throws<ArgumentNullException>(() => ProcessStructuralValidator.Validate(null!));
    }
}
