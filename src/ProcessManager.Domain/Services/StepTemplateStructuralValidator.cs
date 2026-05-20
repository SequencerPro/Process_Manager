using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Services;

/// <summary>
/// Structural rules for a <see cref="StepTemplate"/>. These are pure shape
/// checks (does this template have the right number/kind of ports for its
/// declared pattern?), kept separate from content/maturity rules which live
/// in MaturityScoringService.
///
/// Used by the Step Template Builder for real-time validation and (future)
/// server-side gate on submit-for-approval.
/// </summary>
public static class StepTemplateStructuralValidator
{
    public static BuilderValidationResult Validate(StepTemplate template)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));

        var errors = new List<BuilderValidationIssue>();
        var warnings = new List<BuilderValidationIssue>();

        var ports = (template.Ports ?? new List<Port>()).ToList();
        var inputs = ports.Where(p => p.Direction == PortDirection.Input).ToList();
        var outputs = ports.Where(p => p.Direction == PortDirection.Output).ToList();

        // R-S01 (Error) — Code and Name must be non-empty.
        if (string.IsNullOrWhiteSpace(template.Code))
            errors.Add(new BuilderValidationIssue("R-S01", "Code is required.", BuilderIssueSeverity.Error));
        if (string.IsNullOrWhiteSpace(template.Name))
            errors.Add(new BuilderValidationIssue("R-S01", "Name is required.", BuilderIssueSeverity.Error));

        // R-S02 (Error) — Port counts must match declared pattern.
        switch (template.Pattern)
        {
            case StepPattern.Transform:
                if (inputs.Count != 1 || outputs.Count != 1)
                    errors.Add(new BuilderValidationIssue("R-S02",
                        $"Transform pattern requires exactly 1 input and 1 output (found {inputs.Count} in / {outputs.Count} out).",
                        BuilderIssueSeverity.Error));
                break;
            case StepPattern.Assembly:
                if (inputs.Count < 2 || outputs.Count != 1)
                    errors.Add(new BuilderValidationIssue("R-S02",
                        $"Assembly pattern requires 2+ inputs and exactly 1 output (found {inputs.Count} in / {outputs.Count} out).",
                        BuilderIssueSeverity.Error));
                break;
            case StepPattern.Division:
                if (inputs.Count != 1 || outputs.Count < 2)
                    errors.Add(new BuilderValidationIssue("R-S02",
                        $"Division pattern requires exactly 1 input and 2+ outputs (found {inputs.Count} in / {outputs.Count} out).",
                        BuilderIssueSeverity.Error));
                break;
            case StepPattern.General:
                if (ports.Count == 0)
                    warnings.Add(new BuilderValidationIssue("R-S02",
                        "General pattern has no ports — consider adding inputs/outputs or switching pattern.",
                        BuilderIssueSeverity.Warning));
                break;
        }

        // R-S03 (Error) — Material ports require Kind + Grade + QtyRuleMode.
        foreach (var p in ports.Where(p => p.PortType == PortType.Material))
        {
            if (!p.KindId.HasValue || !p.GradeId.HasValue || !p.QtyRuleMode.HasValue)
            {
                errors.Add(new BuilderValidationIssue("R-S03",
                    $"Material port '{p.Name}' is missing Kind, Grade, or quantity rule.",
                    BuilderIssueSeverity.Error,
                    NodeId: p.Id));
            }
        }

        // R-S04 (Error) — Parameter/Characteristic ports require DataType.
        foreach (var p in ports.Where(p =>
            p.PortType == PortType.Parameter || p.PortType == PortType.Characteristic))
        {
            if (!p.DataType.HasValue)
            {
                errors.Add(new BuilderValidationIssue("R-S04",
                    $"{p.PortType} port '{p.Name}' is missing DataType.",
                    BuilderIssueSeverity.Error,
                    NodeId: p.Id));
            }
        }

        // R-S05 (Warning) — Duplicate port names within the same direction.
        foreach (var grp in ports.GroupBy(p => (p.Direction, p.Name?.Trim().ToLowerInvariant() ?? "")))
        {
            if (grp.Count() > 1 && !string.IsNullOrEmpty(grp.Key.Item2))
            {
                warnings.Add(new BuilderValidationIssue("R-S05",
                    $"Duplicate port name '{grp.First().Name}' on {grp.Key.Direction} side.",
                    BuilderIssueSeverity.Warning));
            }
        }

        return new BuilderValidationResult(errors, warnings);
    }
}
