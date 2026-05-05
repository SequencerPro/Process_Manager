using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Controllers;

public partial class McpController
{
    // ─── Argument-parsing helpers ──────────────────────────────────────────────

    private static Guid? ParseGuidArg(JsonElement args, string name)
    {
        if (args.ValueKind == JsonValueKind.Undefined) return null;
        if (args.TryGetProperty(name, out var prop))
        {
            var str = prop.GetString()?.Trim();
            if (Guid.TryParse(str, out var guid)) return guid;
        }
        return null;
    }

    private static string? GetStringArg(JsonElement args, string name)
    {
        if (args.ValueKind == JsonValueKind.Undefined) return null;
        return args.TryGetProperty(name, out var prop) ? prop.GetString()?.Trim() : null;
    }

    private static int GetIntArg(JsonElement args, string name, int defaultValue)
    {
        if (args.ValueKind == JsonValueKind.Undefined) return defaultValue;
        if (args.TryGetProperty(name, out var prop) && prop.TryGetInt32(out var v)) return v;
        return defaultValue;
    }

    private static decimal GetDecimalArg(JsonElement args, string name, decimal defaultValue)
    {
        if (args.ValueKind == JsonValueKind.Undefined) return defaultValue;
        if (args.TryGetProperty(name, out var prop) && prop.TryGetDecimal(out var v)) return v;
        // Handle numbers sent as strings
        if (args.TryGetProperty(name, out var sp) && decimal.TryParse(sp.GetString(), out var sv)) return sv;
        return defaultValue;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    private string GetCurrentUserName() =>
        User.FindFirstValue("display_name")
        ?? User.Identity?.Name
        ?? GetCurrentUserId();

    // ─── create_nonconformance ─────────────────────────────────────────────────

    private async Task<string> ToolCreateNonConformance(JsonElement args)
    {
        var stepExecutionId = ParseGuidArg(args, "step_execution_id");
        var contentBlockId = ParseGuidArg(args, "content_block_id");
        var actualValue = GetStringArg(args, "actual_value");
        var limitTypeStr = GetStringArg(args, "limit_type");

        if (!stepExecutionId.HasValue)
            return "Error: 'step_execution_id' is required and must be a valid GUID.";
        if (!contentBlockId.HasValue)
            return "Error: 'content_block_id' is required and must be a valid GUID.";
        if (string.IsNullOrEmpty(actualValue))
            return "Error: 'actual_value' is required.";
        if (!Enum.TryParse<LimitType>(limitTypeStr, true, out var limitType))
            return $"Error: Invalid 'limit_type' '{limitTypeStr}'. Expected: LSL, USL, or FailResult.";

        var stepExecution = await _db.StepExecutions
            .Include(se => se.Job)
            .Include(se => se.ProcessStep)
            .FirstOrDefaultAsync(se => se.Id == stepExecutionId.Value);
        if (stepExecution is null)
            return "Error: StepExecution not found.";

        var contentBlock = await _db.ProcessStepContents.FindAsync(contentBlockId.Value);
        if (contentBlock is null)
            return "Error: ContentBlock not found.";

        var nc = new NonConformance
        {
            StepExecutionId = stepExecutionId.Value,
            ContentBlockId = contentBlockId.Value,
            ActualValue = actualValue,
            LimitType = limitType,
            DispositionStatus = DispositionStatus.Pending
        };

        _db.NonConformances.Add(nc);
        await _db.SaveChangesAsync();

        _webhooks?.Publish("nonconformance.created", new { ncId = nc.Id, jobCode = stepExecution.Job?.Code, limitType, actualValue });

        return $"## Non-Conformance Created\n\n" +
               $"- **ID:** `{nc.Id}`\n" +
               $"- **Job:** {stepExecution.Job?.Code}\n" +
               $"- **Step:** {stepExecution.ProcessStep?.NameOverride ?? "Step " + stepExecution.Sequence}\n" +
               $"- **Actual Value:** {actualValue}\n" +
               $"- **Limit Type:** {limitType}\n" +
               $"- **Status:** Pending\n";
    }

    // ─── create_action_item ────────────────────────────────────────────────────

    private async Task<string> ToolCreateActionItem(JsonElement args)
    {
        var title = GetStringArg(args, "title");
        var description = GetStringArg(args, "description");
        var assignedToUserId = GetStringArg(args, "assigned_to_user_id");
        var assignedToDisplayName = GetStringArg(args, "assigned_to_display_name");
        var dueDateStr = GetStringArg(args, "due_date");
        var priorityStr = GetStringArg(args, "priority");
        var sourceTypeStr = GetStringArg(args, "source_type");
        var sourceEntityId = ParseGuidArg(args, "source_entity_id");

        if (string.IsNullOrEmpty(title))
            return "Error: 'title' is required.";
        if (string.IsNullOrEmpty(assignedToUserId))
            return "Error: 'assigned_to_user_id' is required.";
        if (string.IsNullOrEmpty(assignedToDisplayName))
            return "Error: 'assigned_to_display_name' is required.";

        if (!Enum.TryParse<ActionItemPriority>(priorityStr, true, out var priority))
            return $"Error: Invalid 'priority' '{priorityStr}'. Expected: Low, Medium, High, or Critical.";
        if (!Enum.TryParse<ActionItemSourceType>(sourceTypeStr, true, out var sourceType))
            return $"Error: Invalid 'source_type' '{sourceTypeStr}'. Expected: Manual, NonConformance, PFMEA, Audit, MRB, or ManagementReview.";

        if (string.IsNullOrEmpty(dueDateStr))
            return "Error: 'due_date' is required. Use ISO 8601 format (e.g. 2026-04-15).";
        if (!DateTime.TryParse(dueDateStr, out var dueDate))
            return $"Error: Invalid 'due_date' '{dueDateStr}'. Use ISO 8601 format.";

        var assignerUserId = GetCurrentUserId();
        var assignerName = GetCurrentUserName();

        var item = new ActionItem
        {
            Title = title,
            Description = description,
            AssignedToUserId = assignedToUserId,
            AssignedToDisplayName = assignedToDisplayName,
            AssignedByUserId = assignerUserId,
            AssignedByDisplayName = assignerName,
            DueDate = dueDate,
            Priority = priority,
            SourceType = sourceType,
            SourceEntityId = sourceEntityId,
            Status = ActionItemStatus.Open,
        };

        _db.ActionItems.Add(item);
        await _db.SaveChangesAsync();

        _webhooks?.Publish("action_item.created", new { actionItemId = item.Id, title, assignedToUserId, priority = priority.ToString(), dueDate = dueDate.ToString("O") });

        return $"## Action Item Created\n\n" +
               $"- **ID:** `{item.Id}`\n" +
               $"- **Title:** {title}\n" +
               $"- **Assigned To:** {assignedToDisplayName}\n" +
               $"- **Priority:** {priority}\n" +
               $"- **Due Date:** {dueDate:yyyy-MM-dd}\n" +
               $"- **Source:** {sourceType}\n" +
               $"- **Status:** Open\n";
    }

    // ─── complete_action_item ──────────────────────────────────────────────────

    private async Task<string> ToolCompleteActionItem(JsonElement args)
    {
        var actionItemId = ParseGuidArg(args, "action_item_id");
        var completionNotes = GetStringArg(args, "completion_notes");

        if (!actionItemId.HasValue)
            return "Error: 'action_item_id' is required and must be a valid GUID.";

        var item = await _db.ActionItems.FindAsync(actionItemId.Value);
        if (item is null)
            return "Error: Action item not found.";

        if (item.Status is not (ActionItemStatus.Open or ActionItemStatus.InProgress))
            return $"Error: Cannot complete action item with status '{item.Status}'. Must be Open or InProgress.";

        var userName = GetCurrentUserName();

        item.Status = ActionItemStatus.Complete;
        item.CompletedBy = userName;
        item.CompletedAt = DateTime.UtcNow;
        item.CompletionNotes = completionNotes;

        await _db.SaveChangesAsync();

        _webhooks?.Publish("action_item.completed", new { actionItemId = item.Id, title = item.Title, completedBy = userName });

        return $"## Action Item Completed\n\n" +
               $"- **ID:** `{item.Id}`\n" +
               $"- **Title:** {item.Title}\n" +
               $"- **Completed By:** {userName}\n" +
               $"- **Completed At:** {item.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC\n" +
               $"- **Notes:** {completionNotes ?? "—"}\n" +
               $"- **Status:** Complete (awaiting verification)\n";
    }

    // ─── create_job ────────────────────────────────────────────────────────────

    private async Task<string> ToolCreateJob(JsonElement args)
    {
        var code = GetStringArg(args, "code");
        var name = GetStringArg(args, "name");
        var description = GetStringArg(args, "description");
        var processId = ParseGuidArg(args, "process_id");
        var priority = GetIntArg(args, "priority", 5);
        var dueDateStr = GetStringArg(args, "due_date");
        var plannedStartStr = GetStringArg(args, "planned_start_date");

        if (string.IsNullOrEmpty(code))
            return "Error: 'code' is required.";
        if (string.IsNullOrEmpty(name))
            return "Error: 'name' is required.";
        if (!processId.HasValue)
            return "Error: 'process_id' is required and must be a valid GUID.";

        if (await _db.Jobs.AnyAsync(j => j.Code == code))
            return $"Error: A job with code '{code}' already exists.";

        var process = await _db.Processes
            .Include(p => p.ProcessSteps)
                .ThenInclude(ps => ps.StepTemplate)
                    .ThenInclude(st => st!.Ports)
            .FirstOrDefaultAsync(p => p.Id == processId.Value);

        if (process is null)
            return $"Error: Process '{processId}' not found.";
        if (!process.IsActive)
            return $"Error: Process '{process.Code}' is not active.";
        if (process.Status != ProcessStatus.Released && process.Status != ProcessStatus.Superseded)
            return $"Error: Process '{process.Code}' is not Released (current status: {process.Status}).";

        DateTime? dueDate = null, plannedStartDate = null;
        if (!string.IsNullOrEmpty(dueDateStr) && DateTime.TryParse(dueDateStr, out var dd)) dueDate = dd;
        if (!string.IsNullOrEmpty(plannedStartStr) && DateTime.TryParse(plannedStartStr, out var ps2)) plannedStartDate = ps2;

        var currentUserId = GetCurrentUserId();

        var job = new Job
        {
            Code = code,
            Name = name,
            Description = description,
            ProcessId = processId.Value,
            Priority = priority,
            ProcessVersion = process.Version,
            Status = JobStatus.Created,
            DueDate = dueDate,
            PlannedStartDate = plannedStartDate
        };

        _db.Jobs.Add(job);

        // Auto-create StepExecutions
        foreach (var ps in process.ProcessSteps.OrderBy(ps => ps.Sequence))
        {
            _db.StepExecutions.Add(new StepExecution
            {
                JobId = job.Id,
                ProcessStepId = ps.Id,
                Sequence = ps.Sequence,
                Status = StepExecutionStatus.Pending
            });
        }

        // Auto-generate PickList from input material ports
        var inputMaterialPorts = process.ProcessSteps
            .OrderBy(ps => ps.Sequence)
            .SelectMany(ps => (ps.StepTemplate?.Ports ?? Enumerable.Empty<Port>())
                .Where(p => p.Direction == PortDirection.Input
                         && p.PortType == PortType.Material
                         && p.KindId.HasValue))
            .ToList();

        if (inputMaterialPorts.Count > 0)
        {
            var pickList = new PickList
            {
                JobId = job.Id,
                Status = PickListStatus.Open,
                GeneratedAt = DateTime.UtcNow,
                GeneratedByUserId = currentUserId
            };
            _db.PickLists.Add(pickList);

            foreach (var port in inputMaterialPorts)
            {
                var requiredQty = port.QtyRuleMode switch
                {
                    QuantityRuleMode.Exactly => port.QtyRuleN ?? 1,
                    QuantityRuleMode.ZeroOrN => port.QtyRuleN ?? 1,
                    QuantityRuleMode.Range => port.QtyRuleMin ?? 1,
                    QuantityRuleMode.Unbounded => port.QtyRuleMin ?? 1,
                    _ => 1
                };

                var suggestedLocationId = await _db.Items
                    .Where(i => i.KindId == port.KindId!.Value
                             && i.StorageLocationId != null
                             && i.Status == ItemStatus.Available)
                    .GroupBy(i => i.StorageLocationId)
                    .Select(g => new { LocationId = g.Key, Count = g.Count() })
                    .Where(g => g.Count >= requiredQty)
                    .OrderBy(g => g.Count)
                    .Select(g => g.LocationId)
                    .FirstOrDefaultAsync();

                _db.PickListLines.Add(new PickListLine
                {
                    PickListId = pickList.Id,
                    KindId = port.KindId!.Value,
                    SourceLocationId = suggestedLocationId,
                    RequiredQuantity = requiredQty,
                    Status = PickListLineStatus.Pending
                });
            }

            job.PickListId = pickList.Id;
        }

        await _db.SaveChangesAsync();

        _webhooks?.Publish("job.created", new { jobId = job.Id, code, name, processCode = process.Code, status = job.Status.ToString() });

        var stepCount = process.ProcessSteps.Count;
        var pickListMsg = inputMaterialPorts.Count > 0
            ? $"PickList generated with {inputMaterialPorts.Count} line(s)"
            : "No material requirements";

        return $"## Job Created\n\n" +
               $"- **ID:** `{job.Id}`\n" +
               $"- **Code:** {code}\n" +
               $"- **Name:** {name}\n" +
               $"- **Process:** {process.Code} — {process.Name} (v{process.Version})\n" +
               $"- **Steps:** {stepCount}\n" +
               $"- **Materials:** {pickListMsg}\n" +
               $"- **Priority:** {priority}\n" +
               $"- **Status:** Created\n";
    }

    // ─── record_inventory_transaction ──────────────────────────────────────────

    private async Task<string> ToolRecordInventoryTransaction(JsonElement args)
    {
        var transactionTypeStr = GetStringArg(args, "transaction_type");
        var itemId = ParseGuidArg(args, "item_id");
        var fromLocationId = ParseGuidArg(args, "from_location_id");
        var toLocationId = ParseGuidArg(args, "to_location_id");
        var quantity = GetDecimalArg(args, "quantity", 0);
        var notes = GetStringArg(args, "notes");

        if (!Enum.TryParse<InventoryTransactionType>(transactionTypeStr, true, out var txnType))
            return $"Error: Invalid 'transaction_type' '{transactionTypeStr}'. Expected: Receipt, Issue, Transfer, or Adjustment.";
        if (txnType == InventoryTransactionType.PicklistConsumption)
            return "Error: PicklistConsumption transactions are created automatically via the picklist consume workflow.";
        if (!itemId.HasValue)
            return "Error: 'item_id' is required and must be a valid GUID.";
        if (quantity <= 0)
            return "Error: 'quantity' must be a positive number.";

        var item = await _db.Items
            .Include(i => i.Kind)
            .Include(i => i.StorageLocation)
            .FirstOrDefaultAsync(i => i.Id == itemId.Value);
        if (item is null)
            return "Error: Item not found.";

        StorageLocation? fromLoc = null, toLoc = null;

        switch (txnType)
        {
            case InventoryTransactionType.Receipt:
                if (fromLocationId.HasValue) return "Error: Receipt transactions must not have a from_location_id.";
                if (!toLocationId.HasValue) return "Error: Receipt transactions require a to_location_id.";
                toLoc = await _db.StorageLocations.FindAsync(toLocationId.Value);
                if (toLoc is null || !toLoc.IsActive) return "Error: Destination location not found or inactive.";
                item.StorageLocationId = toLoc.Id;
                break;

            case InventoryTransactionType.Issue:
                if (!fromLocationId.HasValue) return "Error: Issue transactions require a from_location_id.";
                if (toLocationId.HasValue) return "Error: Issue transactions must not have a to_location_id.";
                fromLoc = await _db.StorageLocations.FindAsync(fromLocationId.Value);
                if (fromLoc is null) return "Error: Source location not found.";
                if (item.StorageLocationId != fromLoc.Id) return "Error: Item is not in the specified source location.";
                item.StorageLocationId = null;
                break;

            case InventoryTransactionType.Transfer:
                if (!fromLocationId.HasValue || !toLocationId.HasValue)
                    return "Error: Transfer transactions require both from_location_id and to_location_id.";
                if (fromLocationId.Value == toLocationId.Value)
                    return "Error: Cannot transfer to the same location.";
                fromLoc = await _db.StorageLocations.FindAsync(fromLocationId.Value);
                if (fromLoc is null) return "Error: Source location not found.";
                if (item.StorageLocationId != fromLoc.Id) return "Error: Item is not in the specified source location.";
                toLoc = await _db.StorageLocations.FindAsync(toLocationId.Value);
                if (toLoc is null || !toLoc.IsActive) return "Error: Destination location not found or inactive.";
                item.StorageLocationId = toLoc.Id;
                break;

            case InventoryTransactionType.Adjustment:
                if (!toLocationId.HasValue) return "Error: Adjustment transactions require a to_location_id.";
                toLoc = await _db.StorageLocations.FindAsync(toLocationId.Value);
                if (toLoc is null || !toLoc.IsActive) return "Error: Destination location not found or inactive.";
                if (fromLocationId.HasValue)
                {
                    fromLoc = await _db.StorageLocations.FindAsync(fromLocationId.Value);
                    if (fromLoc is null) return "Error: Source location not found.";
                }
                item.StorageLocationId = toLoc.Id;
                break;

            default:
                return "Error: Manual transactions of this type are not allowed.";
        }

        var userId = GetCurrentUserId();

        var txn = new InventoryTransaction
        {
            TransactionType = txnType,
            ItemId = item.Id,
            FromLocationId = fromLoc?.Id,
            ToLocationId = toLoc?.Id,
            Quantity = quantity,
            Notes = notes,
            TransactedAt = DateTime.UtcNow,
            TransactedByUserId = userId
        };

        _db.InventoryTransactions.Add(txn);
        await _db.SaveChangesAsync();

        _webhooks?.Publish($"inventory.{txnType.ToString().ToLowerInvariant()}", new { transactionId = txn.Id, itemId = item.Id, transactionType = txnType.ToString(), quantity });

        return $"## Inventory Transaction Recorded\n\n" +
               $"- **ID:** `{txn.Id}`\n" +
               $"- **Type:** {txnType}\n" +
               $"- **Item:** {item.Kind?.Code} — {item.SerialNumber ?? item.Id.ToString()[..8]}\n" +
               $"- **From:** {fromLoc?.Code ?? "—"}\n" +
               $"- **To:** {toLoc?.Code ?? "—"}\n" +
               $"- **Quantity:** {quantity}\n" +
               $"- **Notes:** {notes ?? "—"}\n";
    }

    // ─── transition_job ────────────────────────────────────────────────────────

    private async Task<string> ToolTransitionJob(JsonElement args)
    {
        var jobId = ParseGuidArg(args, "job_id");
        var transition = GetStringArg(args, "transition")?.ToLowerInvariant();

        if (!jobId.HasValue)
            return "Error: 'job_id' is required and must be a valid GUID.";
        if (string.IsNullOrEmpty(transition))
            return "Error: 'transition' is required. Expected: start, complete, cancel, hold, or resume.";

        var job = await _db.Jobs
            .Include(j => j.Process)
            .Include(j => j.StepExecutions)
            .FirstOrDefaultAsync(j => j.Id == jobId.Value);

        if (job is null)
            return "Error: Job not found.";

        switch (transition)
        {
            case "start":
                if (job.Status != JobStatus.Created && job.Status != JobStatus.OnHold)
                    return $"Error: Cannot start a job with status '{job.Status}'. Must be Created or OnHold.";
                job.Status = JobStatus.InProgress;
                job.StartedAt ??= DateTime.UtcNow;
                break;

            case "complete":
                if (job.Status != JobStatus.InProgress)
                    return $"Error: Cannot complete a job with status '{job.Status}'. Must be InProgress.";
                var incomplete = job.StepExecutions
                    .Where(se => se.Status != StepExecutionStatus.Completed
                              && se.Status != StepExecutionStatus.Skipped)
                    .ToList();
                if (incomplete.Any())
                    return $"Error: Cannot complete job — {incomplete.Count} step(s) are not finished: " +
                           string.Join(", ", incomplete.Select(se => $"Step {se.Sequence} ({se.Status})"));
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                break;

            case "cancel":
                if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
                    return $"Error: Cannot cancel a {job.Status} job.";
                job.Status = JobStatus.Cancelled;
                job.CompletedAt = DateTime.UtcNow;
                break;

            case "hold":
                if (job.Status != JobStatus.InProgress)
                    return $"Error: Cannot hold a job with status '{job.Status}'. Must be InProgress.";
                job.Status = JobStatus.OnHold;
                break;

            case "resume":
                if (job.Status != JobStatus.OnHold)
                    return $"Error: Cannot resume a job with status '{job.Status}'. Must be OnHold.";
                job.Status = JobStatus.InProgress;
                break;

            default:
                return $"Error: Unknown transition '{transition}'. Expected: start, complete, cancel, hold, or resume.";
        }

        await _db.SaveChangesAsync();

        var eventType = transition switch
        {
            "start" => "job.started",
            "complete" => "job.completed",
            "cancel" => "job.cancelled",
            "hold" => "job.held",
            "resume" => "job.resumed",
            _ => $"job.{transition}"
        };
        _webhooks?.Publish(eventType, new { jobId = job.Id, code = job.Code, transition, newStatus = job.Status.ToString() });

        return $"## Job Transitioned\n\n" +
               $"- **ID:** `{job.Id}`\n" +
               $"- **Code:** {job.Code}\n" +
               $"- **Transition:** {transition}\n" +
               $"- **New Status:** {job.Status}\n";
    }

    // ─── list_mcp_audit_log ────────────────────────────────────────────────────

    private async Task<string> ToolListMcpAuditLog(JsonElement args)
    {
        var toolNameFilter = GetStringArg(args, "tool_name");
        var userIdFilter = GetStringArg(args, "user_id");
        var actionFilter = GetStringArg(args, "action");
        var top = Math.Clamp(GetIntArg(args, "top", 50), 1, 200);

        var query = _db.McpAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(toolNameFilter))
            query = query.Where(a => a.ToolName == toolNameFilter);
        if (!string.IsNullOrEmpty(userIdFilter))
            query = query.Where(a => a.UserId == userIdFilter);
        if (!string.IsNullOrEmpty(actionFilter))
            query = query.Where(a => a.Action == actionFilter);

        var entries = await query
            .OrderByDescending(a => a.PerformedAt)
            .Take(top)
            .ToListAsync();

        if (!entries.Any())
            return "No audit log entries found matching the criteria.";

        var sb = new StringBuilder();
        sb.AppendLine($"## MCP Audit Log ({entries.Count} entries)\n");
        sb.AppendLine("| Time (UTC) | Tool | Action | User | Success | Duration |");
        sb.AppendLine("|------------|------|--------|------|---------|----------|");

        foreach (var e in entries)
        {
            var success = e.IsSuccess ? "✅" : "❌";
            sb.AppendLine($"| {e.PerformedAt:yyyy-MM-dd HH:mm:ss} | {e.ToolName} | {e.Action} | {e.UserDisplayName ?? e.UserId ?? "anon"} | {success} | {e.DurationMs}ms |");
        }

        return sb.ToString();
    }

    // ── Phase 17: Standards Conformance ──────────────────────────────────

    private async Task<string> ToolGetConformanceStatus(JsonElement args)
    {
        var standardFilter = GetStringArg(args, "standard");
        var clauseFilter   = GetStringArg(args, "clause_number");

        var query = _db.StandardsClauses
            .Include(c => c.EvidenceLinks)
            .Include(c => c.Findings)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(standardFilter) &&
            Enum.TryParse<ConformanceStandard>(standardFilter, true, out var std))
            query = query.Where(c => c.Standard == std);

        if (!string.IsNullOrEmpty(clauseFilter))
            query = query.Where(c => c.ClauseNumber == clauseFilter);

        var clauses = await query
            .OrderBy(c => c.Standard)
            .ThenBy(c => c.ClauseNumber)
            .ToListAsync();

        if (!clauses.Any())
            return "No standards clauses found matching the criteria.";

        var covered = 0; var partial = 0; var gap = 0; var majorFinding = 0;
        var openMajors = new List<(string Clause, string Title, string FindingDesc)>();

        foreach (var c in clauses)
        {
            var hasOpenMajor = c.Findings.Any(f =>
                f.FindingType == FindingType.MajorNonconformance && f.Status != FindingStatus.Closed);

            if (hasOpenMajor)
            {
                majorFinding++;
                foreach (var f in c.Findings.Where(f =>
                    f.FindingType == FindingType.MajorNonconformance && f.Status != FindingStatus.Closed))
                    openMajors.Add((c.ClauseNumber, c.Title, f.Description));
            }
            else if (!c.EvidenceLinks.Any())
                gap++;
            else if (c.Findings.Any(f =>
                (f.FindingType == FindingType.MinorNonconformance || f.FindingType == FindingType.Observation)
                && f.Status != FindingStatus.Closed))
                partial++;
            else
                covered++;
        }

        var nextAudit = await _db.Audits
            .Where(a => a.Status == AuditStatus.Planned && a.PlannedDate >= DateTime.UtcNow)
            .OrderBy(a => a.PlannedDate)
            .Select(a => (DateTime?)a.PlannedDate)
            .FirstOrDefaultAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"## Conformance Status ({clauses.Count} clauses)\n");
        sb.AppendLine($"- **Covered:** {covered}");
        sb.AppendLine($"- **Partial Coverage:** {partial}");
        sb.AppendLine($"- **Gap (no evidence):** {gap}");
        sb.AppendLine($"- **Open Major Findings:** {majorFinding}");
        sb.AppendLine($"- **Next Planned Audit:** {(nextAudit.HasValue ? nextAudit.Value.ToString("yyyy-MM-dd") : "None scheduled")}");

        if (openMajors.Any())
        {
            sb.AppendLine("\n### Open Major Findings\n");
            sb.AppendLine("| Clause | Title | Finding |");
            sb.AppendLine("|--------|-------|---------|");
            foreach (var (clause, title, desc) in openMajors)
                sb.AppendLine($"| {clause} | {title} | {desc[..Math.Min(80, desc.Length)]} |");
        }

        return sb.ToString();
    }

    // ── Phase 24: SPC & Capability Analysis ─────────────────────────────

    private async Task<string> ToolGetSpcStatus(JsonElement args)
    {
        var processIdStr = GetStringArg(args, "process_id");
        var oocOnly = GetStringArg(args, "ooc_only");

        var query = _db.SpcCharts
            .Include(c => c.Process)
            .Include(c => c.DataPoints)
            .Where(c => c.IsActive)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(processIdStr) && Guid.TryParse(processIdStr, out var pid))
            query = query.Where(c => c.ProcessId == pid);

        var charts = await query.OrderBy(c => c.Process.Name).ThenBy(c => c.Name).ToListAsync();

        if (!charts.Any())
            return "No active SPC charts found.";

        var sb = new StringBuilder();
        sb.AppendLine($"## SPC Status ({charts.Count} active charts)\n");
        sb.AppendLine("| Chart | Process | Type | Points | Cpk | OOC |");
        sb.AppendLine("|-------|---------|------|--------|-----|-----|");

        var spcService = new Services.SpcCalculationService();
        var oocTotal = 0;

        foreach (var c in charts)
        {
            var values = c.DataPoints.OrderBy(d => d.SubgroupIndex).Select(d => d.Value).ToList();
            string cpkStr = "—", oocStr = "0";

            if (values.Count >= c.SubgroupSize)
            {
                var result = spcService.Calculate(values, c.SubgroupSize, c.LSL, c.USL);
                cpkStr = result.Cpk.HasValue ? result.Cpk.Value.ToString("F2") : "—";
                oocStr = result.OutOfControlPoints.Count.ToString();
                oocTotal += result.OutOfControlPoints.Count;
            }

            var skipRow = string.Equals(oocOnly, "true", StringComparison.OrdinalIgnoreCase)
                          && oocStr == "0";
            if (skipRow) continue;

            sb.AppendLine($"| {c.Name} | {c.Process.Name} | {c.ChartType} | {values.Count} | {cpkStr} | {oocStr} |");
        }

        sb.AppendLine($"\n**Total charts with OOC points:** {charts.Count(c => c.DataPoints.Count > 0)}");

        return sb.ToString();
    }

    private async Task<string> ToolGetProcessCapability(JsonElement args)
    {
        var chartIdStr = GetStringArg(args, "chart_id");
        if (string.IsNullOrEmpty(chartIdStr) || !Guid.TryParse(chartIdStr, out var chartId))
            return "Error: chart_id is required (GUID).";

        var chart = await _db.SpcCharts
            .Include(c => c.Process)
            .Include(c => c.DataPoints)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chartId);

        if (chart is null)
            return "Error: SPC chart not found.";

        var values = chart.DataPoints.OrderBy(d => d.SubgroupIndex).Select(d => d.Value).ToList();
        if (values.Count < chart.SubgroupSize)
            return $"Error: Need at least {chart.SubgroupSize} data points (have {values.Count}).";

        var spcService = new Services.SpcCalculationService();
        var result = spcService.Calculate(values, chart.SubgroupSize, chart.LSL, chart.USL);

        var sb = new StringBuilder();
        sb.AppendLine($"## Process Capability: {chart.Name}\n");
        sb.AppendLine($"- **Process:** {chart.Process.Name}");
        sb.AppendLine($"- **Chart Type:** {chart.ChartType}");
        sb.AppendLine($"- **Subgroup Size:** {chart.SubgroupSize}");
        sb.AppendLine($"- **Total Points:** {result.TotalPoints}");
        sb.AppendLine($"- **Subgroup Count:** {result.SubgroupCount}");

        sb.AppendLine($"\n### Central Tendency & Dispersion\n");
        sb.AppendLine($"- **X-bar (Grand Mean):** {result.XBar}");
        sb.AppendLine($"- **R-bar (Average Range):** {result.RBar}");
        sb.AppendLine($"- **Estimated σ (within):** {result.StdDev}");

        sb.AppendLine($"\n### Control Limits\n");
        sb.AppendLine($"| Metric | UCL | CL | LCL |");
        sb.AppendLine($"|--------|-----|----|----|");
        sb.AppendLine($"| X-bar | {result.UCL} | {result.CL} | {result.LCL} |");
        sb.AppendLine($"| Range | {result.RangeUCL} | {result.RangeCL} | {result.RangeLCL} |");

        if (chart.LSL.HasValue || chart.USL.HasValue)
        {
            sb.AppendLine($"\n### Specification Limits\n");
            if (chart.LSL.HasValue) sb.AppendLine($"- **LSL:** {chart.LSL}");
            if (chart.USL.HasValue) sb.AppendLine($"- **USL:** {chart.USL}");
        }

        sb.AppendLine($"\n### Capability Indices\n");
        sb.AppendLine($"| Index | Value | Assessment |");
        sb.AppendLine($"|-------|-------|------------|");
        if (result.Cp.HasValue)
            sb.AppendLine($"| Cp | {result.Cp.Value:F4} | {CapabilityAssessment(result.Cp.Value)} |");
        if (result.Cpk.HasValue)
            sb.AppendLine($"| Cpk | {result.Cpk.Value:F4} | {CapabilityAssessment(result.Cpk.Value)} |");
        if (result.Pp.HasValue)
            sb.AppendLine($"| Pp | {result.Pp.Value:F4} | {CapabilityAssessment(result.Pp.Value)} |");
        if (result.Ppk.HasValue)
            sb.AppendLine($"| Ppk | {result.Ppk.Value:F4} | {CapabilityAssessment(result.Ppk.Value)} |");

        if (!result.Cp.HasValue && !result.Cpk.HasValue)
            sb.AppendLine("*Specification limits not set — capability indices unavailable.*");

        if (result.OutOfControlPoints.Any())
        {
            sb.AppendLine($"\n### Out-of-Control Points ({result.OutOfControlPoints.Count})\n");
            sb.AppendLine("| Subgroup | Value | Rule |");
            sb.AppendLine("|----------|-------|------|");
            foreach (var ooc in result.OutOfControlPoints.Take(20))
                sb.AppendLine($"| {ooc.SubgroupIndex} | {ooc.Value:F4} | {ooc.Rule} |");
        }
        else
        {
            sb.AppendLine("\n**Process is in statistical control — no Nelson rule violations.**");
        }

        return sb.ToString();
    }

    private static string CapabilityAssessment(decimal value) => value switch
    {
        >= 2.0m => "Excellent (Six Sigma)",
        >= 1.67m => "Very Good",
        >= 1.33m => "Good (minimum for new processes)",
        >= 1.0m => "Marginal (minimum for existing processes)",
        _ => "Poor — process improvement needed"
    };

    // ── Phase 21: Automatic Inventory Tracking ────────────────────────────

    private async Task<string> ToolGetWorkstationStatus(JsonElement args)
    {
        var activeOnly = GetStringArg(args, "active_only");
        var wsCode = GetStringArg(args, "workstation_code");

        var query = _db.Workstations
            .Include(w => w.FixedLocation)
            .Include(w => w.ApiKeys)
            .AsNoTracking()
            .AsQueryable();

        if (string.IsNullOrEmpty(activeOnly) || string.Equals(activeOnly, "true", StringComparison.OrdinalIgnoreCase))
            query = query.Where(w => w.IsActive);

        if (!string.IsNullOrEmpty(wsCode))
            query = query.Where(w => w.Code.ToLower() == wsCode.Trim().ToLower());

        var workstations = await query.OrderBy(w => w.Code).ToListAsync();

        if (!workstations.Any())
            return "No workstations found matching the criteria.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Workstation Status ({workstations.Count} workstations)\n");
        sb.AppendLine("| Code | Name | Location | API Keys | Active | Last Scan |");
        sb.AppendLine("|------|------|----------|----------|--------|-----------|");

        foreach (var ws in workstations)
        {
            var lastScan = await _db.ScanEvents
                .Where(s => s.WorkstationId == ws.Id)
                .OrderByDescending(s => s.ScannedAt)
                .Select(s => (DateTime?)s.ScannedAt)
                .FirstOrDefaultAsync();

            var lastScanStr = lastScan.HasValue ? lastScan.Value.ToString("yyyy-MM-dd HH:mm") : "Never";
            sb.AppendLine($"| {ws.Code} | {ws.Name} | {ws.FixedLocation.Code} | {ws.ApiKeys.Count} | {(ws.IsActive ? "Yes" : "No")} | {lastScanStr} |");
        }

        return sb.ToString();
    }

    private async Task<string> ToolGetSupplierQualityStatus(JsonElement args)
    {
        var statusFilter = GetStringArg(args, "status");
        var topStr = args.TryGetProperty("top", out var tp) ? tp.GetString()?.Trim() : null;
        var top = int.TryParse(topStr, out var t) && t > 0 ? Math.Min(t, 50) : 20;

        var query = _db.Suppliers
            .Include(s => s.Evaluations)
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<SupplierStatus>(statusFilter, true, out var st))
            query = query.Where(s => s.Status == st);

        var suppliers = await query.OrderBy(s => s.Code).Take(top).ToListAsync();

        if (!suppliers.Any())
            return "No suppliers found matching the criteria.";

        var allActive = await _db.Suppliers.AsNoTracking().Where(s => s.IsActive).ToListAsync();
        var approved = allActive.Count(s => s.Status == SupplierStatus.Approved);
        var conditional = allActive.Count(s => s.Status == SupplierStatus.Conditional);
        var suspended = allActive.Count(s => s.Status == SupplierStatus.Suspended);
        var pending = allActive.Count(s => s.Status == SupplierStatus.Pending);

        var sb = new StringBuilder();
        sb.AppendLine($"## Supplier Quality Status\n");
        sb.AppendLine($"**Total Active:** {allActive.Count} | **Approved:** {approved} | **Conditional:** {conditional} | **Suspended:** {suspended} | **Pending:** {pending}\n");

        sb.AppendLine($"### Suppliers ({suppliers.Count})\n");
        sb.AppendLine("| Code | Name | Status | Evaluations | Latest Score |");
        sb.AppendLine("|------|------|--------|-------------|--------------|");

        foreach (var s in suppliers)
        {
            var latest = s.Evaluations.OrderByDescending(e => e.EvaluationDate).FirstOrDefault();
            var scoreStr = latest != null ? latest.OverallScore.ToString() : "—";
            sb.AppendLine($"| `{s.Code}` | {s.Name} | **{s.Status}** | {s.Evaluations.Count} | {scoreStr} |");
        }

        var atRisk = allActive
            .Where(s => s.Status == SupplierStatus.Conditional || s.Status == SupplierStatus.Suspended)
            .ToList();

        if (atRisk.Any())
        {
            sb.AppendLine($"\n### At-Risk Suppliers ({atRisk.Count})\n");
            foreach (var s in atRisk.Take(10))
                sb.AppendLine($"- **{s.Code}** ({s.Name}) — Status: {s.Status}");
        }

        return sb.ToString();
    }

    private async Task<string> ToolGetCapaStatus(JsonElement args)
    {
        var statusFilter = GetStringArg(args, "status");
        var typeFilter = GetStringArg(args, "type");

        var query = _db.CapaRecords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<CapaStatus>(statusFilter, true, out var st))
            query = query.Where(c => c.Status == st);

        if (!string.IsNullOrEmpty(typeFilter) && Enum.TryParse<CapaType>(typeFilter, true, out var tp))
            query = query.Where(c => c.Type == tp);

        var all = await query.ToListAsync();

        if (!all.Any())
            return "No CAPA records found matching the criteria.";

        var open = all.Where(c => c.Status != CapaStatus.Closed).ToList();
        var closed = all.Where(c => c.Status == CapaStatus.Closed).ToList();
        var overdue = open.Where(c => c.VerificationDueDate.HasValue && c.VerificationDueDate.Value < DateTime.UtcNow).ToList();

        var avgDaysToClose = closed.Any()
            ? closed.Average(c => (c.ClosedAt!.Value - c.CreatedAt).TotalDays)
            : 0;

        var effectivenessVerified = closed.Count(c => c.EffectivenessVerifiedAt.HasValue);
        var effectivenessRate = closed.Any() ? (double)effectivenessVerified / closed.Count * 100 : 0;

        var sb = new StringBuilder();
        sb.AppendLine("## CAPA Status Summary\n");
        sb.AppendLine($"**Total:** {all.Count} | **Open:** {open.Count} | **Overdue:** {overdue.Count} | **Closed:** {closed.Count}");
        sb.AppendLine($"**Avg Days to Close:** {avgDaysToClose:F1} | **Effectiveness Rate:** {effectivenessRate:F1}%\n");

        var byStatus = all.GroupBy(c => c.Status.ToString()).OrderBy(g => g.Key);
        sb.AppendLine("### By Status\n");
        sb.AppendLine("| Status | Count |");
        sb.AppendLine("|--------|-------|");
        foreach (var g in byStatus)
            sb.AppendLine($"| {g.Key} | {g.Count()} |");

        var bySource = all.GroupBy(c => c.SourceType.ToString()).OrderByDescending(g => g.Count());
        sb.AppendLine("\n### By Source Type\n");
        sb.AppendLine("| Source | Count |");
        sb.AppendLine("|--------|-------|");
        foreach (var g in bySource)
            sb.AppendLine($"| {g.Key} | {g.Count()} |");

        if (overdue.Any())
        {
            sb.AppendLine($"\n### Overdue CAPAs ({overdue.Count})\n");
            sb.AppendLine("| Code | Type | Problem | Due Date | Owner |");
            sb.AppendLine("|------|------|---------|----------|-------|");
            foreach (var c in overdue.OrderBy(c => c.VerificationDueDate).Take(10))
            {
                var problem = c.ProblemStatement.Length > 40
                    ? c.ProblemStatement[..40] + "..."
                    : c.ProblemStatement;
                sb.AppendLine($"| `{c.Code}` | {c.Type} | {problem} | {c.VerificationDueDate:yyyy-MM-dd} | {c.OwnerDisplayName} |");
            }
        }

        return sb.ToString();
    }
}
