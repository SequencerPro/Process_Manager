using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Services;

/// <summary>
/// Pure-domain structural validator for a <see cref="Process"/>.
/// Callers hydrate ProcessSteps (with their StepTemplate and StepTemplate.Ports)
/// and Flows before invoking. No DB access.
///
/// Mirrors <see cref="WorkflowStructuralValidator"/> — provides the same
/// rule set to the Process Builder's real-time validation badge and to any
/// future server-side release gate.
/// </summary>
public static class ProcessStructuralValidator
{
    public static BuilderValidationResult Validate(Process process)
    {
        if (process is null) throw new ArgumentNullException(nameof(process));

        var errors = new List<BuilderValidationIssue>();
        var warnings = new List<BuilderValidationIssue>();

        var steps = (process.ProcessSteps ?? new List<ProcessStep>())
            .OrderBy(s => s.Sequence)
            .ToList();
        var flows = process.Flows ?? new List<Flow>();

        // R-P01 (Error) — A process must have at least one step.
        if (steps.Count == 0)
        {
            errors.Add(new BuilderValidationIssue(
                "R-P01",
                "Process must have at least one step.",
                BuilderIssueSeverity.Error));
        }

        // R-P02 (Error) — Sequence numbers must be contiguous starting at 1.
        for (var i = 0; i < steps.Count; i++)
        {
            var expected = i + 1;
            if (steps[i].Sequence != expected)
            {
                errors.Add(new BuilderValidationIssue(
                    "R-P02",
                    $"Step sequence is not contiguous — expected {expected}, found {steps[i].Sequence}.",
                    BuilderIssueSeverity.Error,
                    NodeId: steps[i].Id));
                break; // one is enough to flag; user fixes and re-validates
            }
        }

        // R-P03 (Error) — Every flow endpoint must reference an existing step
        // and a port that belongs to that step's template.
        var stepIds = steps.Select(s => s.Id).ToHashSet();
        var portsByStepId = steps.ToDictionary(
            s => s.Id,
            s => (s.StepTemplate?.Ports ?? new List<Port>()).ToList());

        foreach (var flow in flows)
        {
            if (!stepIds.Contains(flow.SourceProcessStepId))
            {
                errors.Add(new BuilderValidationIssue(
                    "R-P03",
                    "Flow source step is not part of this process.",
                    BuilderIssueSeverity.Error));
                continue;
            }
            if (!stepIds.Contains(flow.TargetProcessStepId))
            {
                errors.Add(new BuilderValidationIssue(
                    "R-P03",
                    "Flow target step is not part of this process.",
                    BuilderIssueSeverity.Error));
                continue;
            }

            var srcPorts = portsByStepId[flow.SourceProcessStepId];
            var tgtPorts = portsByStepId[flow.TargetProcessStepId];

            var srcPort = srcPorts.FirstOrDefault(p => p.Id == flow.SourcePortId);
            var tgtPort = tgtPorts.FirstOrDefault(p => p.Id == flow.TargetPortId);

            if (srcPort is null || srcPort.Direction != PortDirection.Output)
            {
                errors.Add(new BuilderValidationIssue(
                    "R-P03",
                    "Flow source port must be an Output port on the source step.",
                    BuilderIssueSeverity.Error,
                    NodeId: flow.SourceProcessStepId));
            }
            if (tgtPort is null || tgtPort.Direction != PortDirection.Input)
            {
                errors.Add(new BuilderValidationIssue(
                    "R-P03",
                    "Flow target port must be an Input port on the target step.",
                    BuilderIssueSeverity.Error,
                    NodeId: flow.TargetProcessStepId));
            }

            // R-P04 (Warning) — Kind/Grade mismatch on Material→Material connections.
            if (srcPort?.PortType == PortType.Material &&
                tgtPort?.PortType == PortType.Material &&
                srcPort.KindId.HasValue && tgtPort.KindId.HasValue &&
                srcPort.KindId != tgtPort.KindId)
            {
                warnings.Add(new BuilderValidationIssue(
                    "R-P04",
                    "Flow connects ports with different Kinds.",
                    BuilderIssueSeverity.Warning,
                    NodeId: flow.TargetProcessStepId));
            }
        }

        // R-P05 (Warning) — Step (other than the first) has no incoming flow.
        if (steps.Count > 1)
        {
            var stepsWithIncoming = flows.Select(f => f.TargetProcessStepId).ToHashSet();
            foreach (var step in steps.Skip(1))
            {
                if (!stepsWithIncoming.Contains(step.Id))
                {
                    warnings.Add(new BuilderValidationIssue(
                        "R-P05",
                        $"Step #{step.Sequence} has no incoming flow.",
                        BuilderIssueSeverity.Warning,
                        NodeId: step.Id));
                }
            }
        }

        return new BuilderValidationResult(errors, warnings);
    }
}
