using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Pure-domain unit tests for <see cref="StepTemplateStructuralValidator"/>
/// (Phase 36.2). Structural shape only — content/maturity is covered by
/// MaturityScoringService and its tests.
/// </summary>
public class StepTemplateStructuralValidatorTests
{
    private static Port Material(PortDirection dir, string? name = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name ?? (dir == PortDirection.Input ? "in" : "out"),
        Direction = dir,
        PortType = PortType.Material,
        KindId = Guid.NewGuid(),
        GradeId = Guid.NewGuid(),
        QtyRuleMode = QuantityRuleMode.Exactly,
    };

    [Fact]
    public void Empty_template_reports_required_fields_and_pattern_mismatch()
    {
        var t = new StepTemplate { Pattern = StepPattern.Transform }; // no code/name/ports

        var result = StepTemplateStructuralValidator.Validate(t);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == "R-S01");
        Assert.Contains(result.Errors, e => e.RuleId == "R-S02");
    }

    [Fact]
    public void Transform_with_1in_1out_is_valid()
    {
        var t = new StepTemplate
        {
            Code = "T-1", Name = "Transform",
            Pattern = StepPattern.Transform,
            Ports = new List<Port> { Material(PortDirection.Input), Material(PortDirection.Output) },
        };

        var result = StepTemplateStructuralValidator.Validate(t);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void Assembly_requires_two_or_more_inputs()
    {
        var t = new StepTemplate
        {
            Code = "A-1", Name = "Assembly",
            Pattern = StepPattern.Assembly,
            Ports = new List<Port> { Material(PortDirection.Input), Material(PortDirection.Output) },
        };

        var result = StepTemplateStructuralValidator.Validate(t);
        Assert.Contains(result.Errors, e => e.RuleId == "R-S02");
    }

    [Fact]
    public void Division_requires_two_or_more_outputs()
    {
        var t = new StepTemplate
        {
            Code = "D-1", Name = "Division",
            Pattern = StepPattern.Division,
            Ports = new List<Port> { Material(PortDirection.Input), Material(PortDirection.Output) },
        };

        var result = StepTemplateStructuralValidator.Validate(t);
        Assert.Contains(result.Errors, e => e.RuleId == "R-S02");
    }

    [Fact]
    public void Material_port_missing_kind_is_error()
    {
        var bad = Material(PortDirection.Input);
        bad.KindId = null; // strip required field

        var t = new StepTemplate
        {
            Code = "T-2", Name = "Bad",
            Pattern = StepPattern.Transform,
            Ports = new List<Port> { bad, Material(PortDirection.Output) },
        };

        var result = StepTemplateStructuralValidator.Validate(t);
        Assert.Contains(result.Errors, e => e.RuleId == "R-S03");
    }

    [Fact]
    public void Parameter_port_without_datatype_is_error()
    {
        var t = new StepTemplate
        {
            Code = "P-1", Name = "Params",
            Pattern = StepPattern.General,
            Ports = new List<Port>
            {
                new() {
                    Id = Guid.NewGuid(), Name = "rpm", Direction = PortDirection.Input,
                    PortType = PortType.Parameter, DataType = null
                }
            },
        };

        var result = StepTemplateStructuralValidator.Validate(t);
        Assert.Contains(result.Errors, e => e.RuleId == "R-S04");
    }

    [Fact]
    public void Duplicate_port_names_on_same_side_is_warning()
    {
        var t = new StepTemplate
        {
            Code = "T-3", Name = "Dups",
            Pattern = StepPattern.Transform,
            Ports = new List<Port>
            {
                Material(PortDirection.Input, "Part"),
                Material(PortDirection.Output, "Part"), // different direction — OK
                Material(PortDirection.Output, "Part"), // duplicate output name — warning
            },
        };

        // R-S02 will also fire (3 outputs ≠ 1 expected for Transform). Filter to R-S05.
        var result = StepTemplateStructuralValidator.Validate(t);
        Assert.Contains(result.Warnings, w => w.RuleId == "R-S05");
    }

    [Fact]
    public void Null_template_throws()
    {
        Assert.Throws<ArgumentNullException>(() => StepTemplateStructuralValidator.Validate(null!));
    }
}
