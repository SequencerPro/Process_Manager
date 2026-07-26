using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;
using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Pure-domain unit tests for <see cref="WorkflowStructuralValidator"/> — no DB,
/// no HTTP, no DI. Verifies the rules added in Phase 36.1.
/// </summary>
public class WorkflowStructuralValidatorTests
{
    // ─────────────── Builders ───────────────

    private static WorkflowProcess MakeWp(
        string? name = null,
        bool entry = false,
        bool terminal = false)
    {
        var wp = new WorkflowProcess
        {
            Id = Guid.NewGuid(),
            IsEntryPoint = entry,
            IsTerminalNode = terminal,
        };
        if (!terminal)
        {
            wp.Process = new Process { Id = Guid.NewGuid(), Name = name ?? "P" };
            wp.ProcessId = wp.Process.Id;
        }
        return wp;
    }

    private static WorkflowLink MakeLink(
        WorkflowProcess src, WorkflowProcess tgt,
        RoutingType routing = RoutingType.Always,
        IEnumerable<WorkflowLinkCondition>? conditions = null)
    {
        return new WorkflowLink
        {
            Id = Guid.NewGuid(),
            SourceWorkflowProcessId = src.Id,
            TargetWorkflowProcessId = tgt.Id,
            SourceWorkflowProcess = src,
            TargetWorkflowProcess = tgt,
            RoutingType = routing,
            Conditions = (conditions ?? Enumerable.Empty<WorkflowLinkCondition>()).ToList(),
        };
    }

    // ─────────────── Tests ───────────────

    [Fact]
    public void Empty_workflow_reports_missing_entry_point()
    {
        var wf = new Workflow { Id = Guid.NewGuid(), Code = "WF", Name = "X" };

        var result = WorkflowStructuralValidator.Validate(wf);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("entry point", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valid_two_node_workflow_with_entry_passes()
    {
        var a = MakeWp("A", entry: true);
        var b = MakeWp("B");
        var wf = new Workflow
        {
            Id = Guid.NewGuid(),
            WorkflowProcesses = new List<WorkflowProcess> { a, b },
            WorkflowLinks = new List<WorkflowLink> { MakeLink(a, b) },
        };

        var result = WorkflowStructuralValidator.Validate(wf);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GradeBased_link_without_conditions_is_error()
    {
        var a = MakeWp("A", entry: true);
        var b = MakeWp("B");
        var link = MakeLink(a, b, RoutingType.GradeBased); // no conditions
        var wf = new Workflow
        {
            WorkflowProcesses = new List<WorkflowProcess> { a, b },
            WorkflowLinks = new List<WorkflowLink> { link },
        };

        var result = WorkflowStructuralValidator.Validate(wf);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("GradeBased"));
        Assert.Contains(result.Errors, e => e.Contains("A") && e.Contains("B"));
    }

    [Fact]
    public void GradeBased_link_with_conditions_passes()
    {
        var a = MakeWp("A", entry: true);
        var b = MakeWp("B");
        var link = MakeLink(a, b, RoutingType.GradeBased,
            new[] { new WorkflowLinkCondition { Id = Guid.NewGuid(), GradeId = Guid.NewGuid() } });

        var wf = new Workflow
        {
            WorkflowProcesses = new List<WorkflowProcess> { a, b },
            WorkflowLinks = new List<WorkflowLink> { link },
        };

        var result = WorkflowStructuralValidator.Validate(wf);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Unreachable_node_produces_warning_not_error()
    {
        var a = MakeWp("A", entry: true);
        var b = MakeWp("B");
        var c = MakeWp("Orphan"); // not entry, no incoming
        var wf = new Workflow
        {
            WorkflowProcesses = new List<WorkflowProcess> { a, b, c },
            WorkflowLinks = new List<WorkflowLink> { MakeLink(a, b) },
        };

        var result = WorkflowStructuralValidator.Validate(wf);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("Orphan") && w.Contains("unreachable"));
    }

    [Fact]
    public void Terminal_node_without_incoming_link_is_warning()
    {
        var a = MakeWp("A", entry: true);
        var end = MakeWp(terminal: true); // terminal, no incoming
        var wf = new Workflow
        {
            WorkflowProcesses = new List<WorkflowProcess> { a, end },
            WorkflowLinks = new List<WorkflowLink>(), // none
        };

        var result = WorkflowStructuralValidator.Validate(wf);

        // No links → A is the only node, terminal sits with no incoming.
        // Missing-entry-point is already satisfied by A.
        Assert.Contains(result.Warnings, w => w.Contains("End"));
    }

    [Fact]
    public void Null_workflow_throws()
    {
        Assert.Throws<ArgumentNullException>(() => WorkflowStructuralValidator.Validate(null!));
    }

    [Fact]
    public void Result_IsValid_reflects_error_count()
    {
        var r1 = new WorkflowStructuralValidationResult(new List<string>(), new List<string> { "w" });
        Assert.True(r1.IsValid);

        var r2 = new WorkflowStructuralValidationResult(new List<string> { "e" }, new List<string>());
        Assert.False(r2.IsValid);
    }
}
