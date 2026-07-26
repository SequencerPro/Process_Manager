using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Domain.Services;

/// <summary>
/// Pure-domain structural validator for a <see cref="Workflow"/> graph.
/// No DB access — callers are responsible for hydrating navigation properties
/// (WorkflowProcesses, WorkflowLinks with their Conditions, and the linked
/// Process.ProcessSteps.StepTemplate.Ports if compatibility checking is desired).
///
/// Phase 36.1 extracted from <c>WorkflowsController.Validate</c> so the same
/// rules can run during /validate, before release/state transitions, and from
/// unit tests without the API factory.
/// </summary>
public static class WorkflowStructuralValidator
{
    /// <summary>
    /// Validate a workflow. Returns a <see cref="WorkflowStructuralValidationResult"/>
    /// with two disjoint lists: hard errors (block release/approval) and
    /// soft warnings (advisory, do not block).
    /// </summary>
    public static WorkflowStructuralValidationResult Validate(Workflow workflow)
    {
        if (workflow is null) throw new ArgumentNullException(nameof(workflow));

        var errors = new List<string>();
        var warnings = new List<string>();

        var processes = workflow.WorkflowProcesses ?? new List<WorkflowProcess>();
        var links = workflow.WorkflowLinks ?? new List<WorkflowLink>();

        // R-W01 (Error) — At least one entry point.
        if (!processes.Any(wp => wp.IsEntryPoint))
            errors.Add("Workflow must have at least one entry point.");

        // R-W02 (Error) — GradeBased links must carry at least one condition.
        foreach (var link in links.Where(l => l.RoutingType == RoutingType.GradeBased))
        {
            var conditions = link.Conditions ?? new List<WorkflowLinkCondition>();
            if (conditions.Count == 0)
            {
                var srcName = NameOf(link.SourceWorkflowProcess) ?? link.SourceWorkflowProcessId.ToString();
                var tgtName = NameOf(link.TargetWorkflowProcess) ?? link.TargetWorkflowProcessId.ToString();
                errors.Add($"GradeBased link '{srcName}' → '{tgtName}' has no conditions.");
            }
        }

        // R-W03 (Warning) — Port-kind compatibility between source.last and target.first.
        // Skip terminal nodes (they have no Process attached).
        foreach (var link in links)
        {
            if (link.SourceWorkflowProcess?.IsTerminalNode == true ||
                link.TargetWorkflowProcess?.IsTerminalNode == true)
                continue;

            var srcProcess = link.SourceWorkflowProcess?.Process;
            var tgtProcess = link.TargetWorkflowProcess?.Process;
            if (srcProcess?.ProcessSteps is null || tgtProcess?.ProcessSteps is null)
                continue;

            var lastStep = srcProcess.ProcessSteps.OrderByDescending(ps => ps.Sequence).FirstOrDefault();
            var firstStep = tgtProcess.ProcessSteps.OrderBy(ps => ps.Sequence).FirstOrDefault();
            if (lastStep?.StepTemplate?.Ports is null || firstStep?.StepTemplate?.Ports is null)
                continue;

            var outputKinds = lastStep.StepTemplate.Ports
                .Where(p => p.Direction == PortDirection.Output)
                .Select(p => p.KindId)
                .ToHashSet();

            var inputKinds = firstStep.StepTemplate.Ports
                .Where(p => p.Direction == PortDirection.Input)
                .Select(p => p.KindId)
                .ToHashSet();

            if (outputKinds.Count > 0 && inputKinds.Count > 0 && !outputKinds.Intersect(inputKinds).Any())
            {
                warnings.Add($"Link '{srcProcess.Name}' → '{tgtProcess.Name}': " +
                    "source output Kinds do not match target input Kinds.");
            }
        }

        // R-W04 (Warning) — Unreachable nodes: non-entry, non-terminal, no incoming.
        var nodesWithIncoming = links
            .Select(l => l.TargetWorkflowProcessId)
            .ToHashSet();

        foreach (var wp in processes)
        {
            if (!wp.IsEntryPoint && !wp.IsTerminalNode && !nodesWithIncoming.Contains(wp.Id))
            {
                var name = NameOf(wp) ?? wp.ProcessId?.ToString() ?? "Unknown";
                warnings.Add($"Process '{name}' is not an entry point and has no incoming links (unreachable).");
            }
        }

        // R-W05 (Warning) — Terminal nodes with no incoming links are dead-ends.
        foreach (var wp in processes.Where(wp => wp.IsTerminalNode))
        {
            if (!nodesWithIncoming.Contains(wp.Id))
                warnings.Add("Terminal 'End' node has no incoming links.");
        }

        return new WorkflowStructuralValidationResult(errors, warnings);
    }

    private static string? NameOf(WorkflowProcess? wp) =>
        wp?.Process?.Name ?? (wp is not null && wp.IsTerminalNode ? "End" : null);
}

/// <summary>
/// Outcome of <see cref="WorkflowStructuralValidator.Validate"/>.
/// </summary>
public sealed record WorkflowStructuralValidationResult(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}
