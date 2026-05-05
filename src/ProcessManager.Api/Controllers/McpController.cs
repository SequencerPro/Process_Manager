using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Model Context Protocol (MCP) server — JSON-RPC 2.0, protocol version 2024-11-05.
///
/// Exposes Process Manager as a set of tools and resources that any MCP-compatible
/// AI client (Microsoft Copilot, GitHub Copilot agent mode, Claude Desktop, etc.)
/// can discover and call.
///
/// Discovery methods (initialize, tools/list, resources/list) are anonymous.
/// Data tools require a JWT Bearer token — create a service account in the admin
/// panel and configure your AI client with it.
///
/// Quick reference for AI client configuration:
///   Endpoint:  POST /mcp
///   Auth:      Bearer {jwt}  (required for data tools)
///   Discovery: GET  /mcp     (returns server info as JSON)
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("mcp")]
public partial class McpController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly IWebhookEventPublisher? _webhooks;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private const string ProtocolVersion = "2024-11-05";
    private const string ServerName      = "ProcessManager";
    private const string ServerVersion   = "3.3";

    public McpController(ProcessManagerDbContext db, IWebhookEventPublisher? webhooks = null)
    {
        _db = db;
        _webhooks = webhooks;
    }

    // ─── Discovery endpoint ───────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Info() => Ok(new
    {
        name            = ServerName,
        version         = ServerVersion,
        protocolVersion = ProtocolVersion,
        description     = "Process Manager MCP server. POST to /mcp with JSON-RPC 2.0 requests.",
        authRequired    = "Bearer JWT for data tools. Obtain via POST /api/auth/login.",
        contextDocument = "/api/help/context",
    });

    // ─── Main JSON-RPC dispatcher ─────────────────────────────────────────────

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> Handle([FromBody] JsonElement body)
    {
        // Parse the request — return parse error if malformed
        if (!TryParseRequest(body, out var method, out var id, out var @params, out var parseError))
            return Ok(ErrorResponse(default, -32600, parseError!));

        return method switch
        {
            "initialize"      => Ok(OkResponse(id, HandleInitialize())),
            "tools/list"      => Ok(OkResponse(id, HandleToolsList())),
            "resources/list"  => Ok(OkResponse(id, HandleResourcesList())),
            "resources/read"  => Ok(await HandleResourcesRead(id, @params)),
            "tools/call"      => Ok(await HandleToolsCall(id, @params)),
            _                 => Ok(ErrorResponse(id, -32601, $"Method not found: {method}"))
        };
    }

    // ─── initialize ──────────────────────────────────────────────────────────

    private static object HandleInitialize() => new
    {
        protocolVersion = ProtocolVersion,
        capabilities    = new { tools = new { }, resources = new { } },
        serverInfo      = new { name = ServerName, version = ServerVersion },
    };

    // ─── tools/list ──────────────────────────────────────────────────────────

    private static object HandleToolsList() => new
    {
        tools = new[]
        {
            Tool("describe_domain",
                 "Get a comprehensive explanation of Process Manager concepts, terminology, and how-to guides. Does not require authentication.",
                 Schema()),

            Tool("list_processes",
                 "List all active process definitions in the system, including step count. Requires authentication.",
                 Schema()),

            Tool("get_process",
                 "Get detailed information about a specific process, including its ordered steps. Requires authentication.",
                 Schema(("query", "string", "Process code or name (partial match supported)"))),

            Tool("list_step_templates",
                 "List all active step template definitions (reusable work unit designs). Requires authentication.",
                 Schema()),

            Tool("list_active_jobs",
                 "List all jobs currently in Created, InProgress, or OnHold status. Requires authentication.",
                 Schema()),

            Tool("get_job_status",
                 "Get the current status and step-by-step execution progress of a specific job. Requires authentication.",
                 Schema(("code", "string", "Job code (exact match)"))),

            Tool("get_pfmea",
                 "Get a PFMEA (Failure Mode &amp; Effects Analysis) by code or name, including all failure modes with Severity×Occurrence×Detection RPN scores and open actions. Requires authentication.",
                 Schema(("query", "string", "PFMEA code or name (partial match supported)"))),

            Tool("list_high_rpn_failure_modes",
                 "List the highest-risk PFMEA failure modes ranked by RPN (Severity×Occurrence×Detection). Useful for prioritising corrective actions. Requires authentication.",
                 Schema(
                     ("top", "number", "Maximum number of failure modes to return (default 20)"),
                     ("min_rpn", "number", "Only include failure modes with RPN >= this value (default 100)"))),

            Tool("get_ce_matrix",
                 "Get a Cause &amp; Effect (Fishbone / C&amp;E) matrix by name or process step, showing input-to-output correlation scores and computed priority scores. Requires authentication.",
                 Schema(("query", "string", "Matrix name or process step name (partial match)"))),

            Tool("get_control_plan",
                 "Get a Control Plan by code or name, including all characteristic entries grouped by process step with specification, measurement technique, and reaction plan details. Requires authentication.",
                 Schema(("query", "string", "Control plan code or name (partial match supported)"))),

            Tool("list_critical_characteristics",
                 "List all Product-type characteristics across active Control Plans, optionally filtered by process. Useful for identifying critical-to-quality parameters across the process. Requires authentication.",
                 Schema(("process_query", "string", "Optional process name or code to filter by (leave empty for all)"))),

            Tool("list_qms_documents",
                 "List QMS documents (procedures, work instructions, approval processes) with their revision and lifecycle status. Useful for determining document currency, pending approvals, and revision history. Requires authentication.",
                 Schema(
                     ("role", "string", "Optional filter: QmsDocument, WorkInstruction, ApprovalProcess, Training, or leave empty for all"),
                     ("status", "string", "Optional filter: Draft, PendingApproval, Released, Superseded, Retired, or leave empty for all"))),

            Tool("list_recurring_root_causes",
                 "List the most frequently encountered root causes from the organisation's Root Cause Library, ordered by usage count. Useful for identifying systemic problems that recur across analyses. Requires authentication.",
                 Schema(
                     ("category", "string", "Optional filter: Machine, Method, Material, People, Measurement, Environment, Management, or leave empty for all"),
                     ("top", "string", "Number of entries to return (default 20, max 50)"))),

            Tool("get_rca_summary",
                 "Retrieve a summary of Ishikawa Diagrams and 5 Whys Analyses for an entity, showing open/closed status, cause/node counts and confirmed root causes. Useful for understanding the current RCA state for a non-conformance or PFMEA failure mode. Requires authentication.",
                 Schema(
                     ("linked_entity_id", "string", "The UUID of the linked entity (non-conformance, PFMEA failure mode, etc.) to filter by — or leave empty to list all open RCAs"),
                     ("status", "string", "Optional: Open or Closed — leave empty for all"))),

            Tool("get_mrb_summary",
                 "Retrieve a summary of Material Review Board (MRB) reviews, optionally filtered by status and/or SCAR/supplier flags. Useful for understanding nonconforming material awaiting MRB disposition decisions. Requires authentication.",
                 Schema(
                     ("status", "string", "Optional: Draft, UnderReview, Decided, or Closed — leave empty for all"),
                     ("scar_required", "string", "Optional: true or false — filter by SCAR Required flag"),
                     ("supplier_caused", "string", "Optional: true or false — filter by Supplier Caused flag"))),

            Tool("get_management_review_status",
                 "Retrieve a summary of scheduled Quality Management Reviews with their status, type, scheduled date, and linked action item counts. Useful for understanding management review cadence and outcomes. Requires authentication.",
                 Schema(
                     ("status", "string", "Optional: Scheduled, InProgress, or Complete — leave empty for all"),
                     ("include_action_items", "string", "true to include a breakdown of action items per review"))),

            Tool("get_competency_status",
                 "Retrieve the training competency status for all users or a specific user. Returns Current, Expired, and Expiring Soon records. Useful for understanding workforce readiness and training compliance. Requires authentication.",
                 Schema(
                     ("user_id", "string", "Optional: filter to a specific user's competency records — leave empty for all users"),
                     ("status",  "string", "Optional: Current, Expired, or Superseded — leave empty for Current and Expired only"))),

            Tool("get_production_status",
                 "Get a live production dashboard: active jobs, late jobs, equipment currently down, maintenance due/overdue, and top bottleneck steps. Requires authentication.",
                 Schema()),

            Tool("list_equipment_downtime",
                 "List equipment that is currently down (open downtime records) or recently experienced downtime. Requires authentication.",
                 Schema(
                     ("currently_down_only", "string", "true to return only equipment currently down, false/empty for all equipment with any downtime history"))),

            Tool("list_overdue_maintenance",
                 "List maintenance tasks that are Overdue or Due across all equipment, ordered by due date ascending. Requires authentication.",
                 Schema(
                     ("status", "string", "Optional: Overdue, Due, or InProgress — leave empty for Overdue and Due"))),

            Tool("get_inventory_status",
                 "On-hand inventory counts grouped by Kind, with optional location filter and low-stock-only flag",
                 Schema(("location_id", "string", "Optional storage location GUID to filter results"),
                        ("low_stock_only", "string", "If 'true', only return Kinds below their ReorderThreshold"))),

            // ── Write tools ────────────────────────────────────────────────
            Tool("create_nonconformance",
                 "Create a non-conformance record for a step execution. Returns the new NCR details.",
                 Schema(("step_execution_id", "string", "GUID of the step execution"),
                        ("content_block_id", "string", "GUID of the content block that was out of spec"),
                        ("actual_value", "string", "The actual measured/observed value"),
                        ("limit_type", "string", "LSL, USL, or FailResult"))),

            Tool("create_action_item",
                 "Create a corrective or preventive action item. Returns the new action item details.",
                 Schema(("title", "string", "Concise action title"),
                        ("description", "string", "Full detail and context"),
                        ("assigned_to_user_id", "string", "User ID of the assignee"),
                        ("assigned_to_display_name", "string", "Display name of the assignee"),
                        ("due_date", "string", "Due date in ISO 8601 format (e.g. 2026-04-15)"),
                        ("priority", "string", "Low, Medium, High, or Critical"),
                        ("source_type", "string", "Manual, NonConformance, PFMEA, Audit, MRB, or ManagementReview"),
                        ("source_entity_id", "string", "Optional GUID linking to the source entity"))),

            Tool("complete_action_item",
                 "Mark an action item as completed with notes. Returns the updated action item.",
                 Schema(("action_item_id", "string", "GUID of the action item to complete"),
                        ("completion_notes", "string", "Notes describing what was done to address the action"))),

            Tool("create_job",
                 "Create a new job from a released process. Auto-creates step executions and pick list. Returns the new job details.",
                 Schema(("code", "string", "Unique job code (e.g. JOB-2026-001)"),
                        ("name", "string", "Job name"),
                        ("description", "string", "Optional description"),
                        ("process_id", "string", "GUID of the process to execute"),
                        ("priority", "number", "Priority (1=highest, default 5)"),
                        ("due_date", "string", "Optional due date in ISO 8601 format"),
                        ("planned_start_date", "string", "Optional planned start date in ISO 8601 format"))),

            Tool("record_inventory_transaction",
                 "Record an inventory transaction (Receipt, Issue, Transfer, or Adjustment). Returns the transaction details.",
                 Schema(("transaction_type", "string", "Receipt, Issue, Transfer, or Adjustment"),
                        ("item_id", "string", "GUID of the item"),
                        ("from_location_id", "string", "GUID of the source location (required for Issue/Transfer)"),
                        ("to_location_id", "string", "GUID of the destination location (required for Receipt/Transfer/Adjustment)"),
                        ("quantity", "number", "Quantity (positive number)"),
                        ("notes", "string", "Optional notes about this transaction"))),

            Tool("transition_job",
                 "Transition a job to a new status. Valid transitions: start, complete, cancel, hold, resume.",
                 Schema(("job_id", "string", "GUID of the job to transition"),
                        ("transition", "string", "start, complete, cancel, hold, or resume"))),

            // ── Audit log ──────────────────────────────────────────────────
            Tool("list_mcp_audit_log",
                 "View recent MCP tool call audit log. Returns timestamped entries showing who called what, when, and whether it succeeded.",
                 Schema(("tool_name", "string", "Optional filter by tool name"),
                        ("user_id", "string", "Optional filter by user ID"),
                        ("action", "string", "Optional filter: Read, Create, Update, or Delete"),
                        ("top", "number", "Number of entries to return (default 50, max 200)"))),

            // ── Phase 17: Standards Conformance ─────────────────────────
            Tool("get_conformance_status",
                 "Get the standards conformance status: clause coverage summary (Covered/Partial/Gap/OpenMajorFinding counts), list of open Major findings with clause reference, and next planned audit date. Requires authentication.",
                 Schema(
                     ("standard", "string", "Optional: Iso9001_2015 or As9100RevD — leave empty for all standards"),
                     ("clause_number", "string", "Optional: filter to a specific clause number (e.g. 8.5.2)"))),

            // ── Phase 24: SPC & Capability Analysis ─────────────────────
            Tool("get_spc_status",
                 "Get a summary of active SPC control charts with their current capability indices (Cp, Cpk) and out-of-control counts. Useful for identifying processes drifting out of control. Requires authentication.",
                 Schema(
                     ("process_id", "string", "Optional: filter to a specific process by GUID"),
                     ("ooc_only", "string", "If 'true', only return charts with out-of-control points"))),

            Tool("get_process_capability",
                 "Get detailed process capability analysis for a specific SPC chart: X-bar, R-bar, control limits, Cp, Cpk, Pp, Ppk, and Nelson rule violations. Requires authentication.",
                 Schema(
                     ("chart_id", "string", "GUID of the SPC chart to analyse"))),

            // ── Phase 21: Automatic Inventory Tracking ──────────────────
            Tool("get_workstation_status",
                 "Get all active workstations with their fixed locations, API key count, and last scan time. Useful for monitoring scanner health and workstation utilisation. Requires authentication.",
                 Schema(
                     ("active_only", "string", "If 'true' (default), only return active workstations"),
                     ("workstation_code", "string", "Optional: filter to a specific workstation by code"))),

            // ── Phase 25: Supplier Quality Management ────────────────────
            Tool("get_supplier_quality_status",
                 "Get a summary of supplier quality: status breakdown (Approved/Conditional/Suspended), at-risk suppliers, average evaluation scores, and suppliers with open non-conformances. Useful for supplier performance monitoring. Requires authentication.",
                 Schema(
                     ("status", "string", "Optional: filter by supplier status (Pending/Approved/Conditional/Suspended/Inactive)"),
                     ("top", "number", "Number of suppliers to return (default 20, max 50)"))),
        }
    };

    // ─── resources/list ──────────────────────────────────────────────────────

    private static object HandleResourcesList() => new
    {
        resources = new[]
        {
            new
            {
                uri         = "process_manager://docs/context",
                name        = "Process Manager Context Document",
                description = "Full domain model, terminology, how-to guides, and API reference.",
                mimeType    = "text/markdown",
            }
        }
    };

    // ─── resources/read ──────────────────────────────────────────────────────

    private static Task<object> HandleResourcesRead(JsonElement id, JsonElement @params)
    {
        var uri = @params.TryGetProperty("uri", out var u) ? u.GetString() : null;

        if (uri == "process_manager://docs/context")
        {
            return Task.FromResult<object>(OkResponse(id, new
            {
                contents = new[]
                {
                    new
                    {
                        uri      = uri,
                        mimeType = "text/markdown",
                        text     = HelpController.ContextDocumentPublic,
                    }
                }
            }));
        }

        return Task.FromResult<object>(ErrorResponse(id, -32602, $"Unknown resource URI: {uri}"));
    }

    // ─── tools/call ──────────────────────────────────────────────────────────

    private async Task<object> HandleToolsCall(JsonElement id, JsonElement @params)
    {
        var toolName = @params.TryGetProperty("name", out var n) ? n.GetString() : null;
        var args     = @params.TryGetProperty("arguments", out var a) ? a : default;

        if (string.IsNullOrEmpty(toolName))
            return ErrorResponse(id, -32602, "tools/call requires a 'name' field.");

        // Public tool — no auth required, not audited
        if (toolName == "describe_domain")
            return OkResponse(id, TextContent(HelpController.ContextDocumentPublic));

        // All other tools require authentication
        if (User.Identity?.IsAuthenticated != true)
            return ErrorResponse(id, -32001,
                "Authentication required. Include 'Authorization: Bearer {jwt}' in your request. " +
                "Obtain a token via POST /api/auth/login.");

        // ── Extract format preference ────────────────────────────────────
        var format = args.ValueKind == JsonValueKind.Object
            && args.TryGetProperty("format", out var fmt)
            && fmt.GetString() == "json"
                ? "json"
                : "markdown";

        // ── Dispatch with audit logging ────────────────────────────────────
        var sw = Stopwatch.StartNew();
        string? resultText = null;
        string? errorMessage = null;
        var isSuccess = true;

        try
        {
            resultText = toolName switch
            {
                "list_processes"             => await ToolListProcesses(),
                "get_process"                => await ToolGetProcess(args),
                "list_step_templates"        => await ToolListStepTemplates(),
                "list_active_jobs"           => await ToolListActiveJobs(),
                "get_job_status"             => await ToolGetJobStatus(args),
                "get_pfmea"                  => await ToolGetPfmea(args),
                "list_high_rpn_failure_modes" => await ToolListHighRpnFailureModes(args),
                "get_ce_matrix"              => await ToolGetCeMatrix(args),
                "get_control_plan"           => await ToolGetControlPlan(args),
                "list_critical_characteristics" => await ToolListCriticalCharacteristics(args),
                "list_qms_documents"            => await ToolListQmsDocuments(args),
                "list_recurring_root_causes"    => await ToolListRecurringRootCauses(args),
                "get_rca_summary"               => await ToolGetRcaSummary(args),
                "get_mrb_summary"               => await ToolGetMrbSummary(args),
                "get_management_review_status"  => await ToolGetManagementReviewStatus(args),
                "get_competency_status"          => await ToolGetCompetencyStatus(args),
                "get_production_status"          => await ToolGetProductionStatus(),
                "list_equipment_downtime"        => await ToolListEquipmentDowntime(args),
                "list_overdue_maintenance"       => await ToolListOverdueMaintenance(args),
                "get_inventory_status"           => await ToolGetInventoryStatus(args),
                // Write tools
                "create_nonconformance"          => await ToolCreateNonConformance(args),
                "create_action_item"             => await ToolCreateActionItem(args),
                "complete_action_item"           => await ToolCompleteActionItem(args),
                "create_job"                     => await ToolCreateJob(args),
                "record_inventory_transaction"   => await ToolRecordInventoryTransaction(args),
                "transition_job"                 => await ToolTransitionJob(args),
                // Audit log
                "list_mcp_audit_log"             => await ToolListMcpAuditLog(args),
                "get_conformance_status"         => await ToolGetConformanceStatus(args),
                // SPC
                "get_spc_status"                 => await ToolGetSpcStatus(args),
                "get_process_capability"         => await ToolGetProcessCapability(args),
                // Phase 21: Workstations
                "get_workstation_status"          => await ToolGetWorkstationStatus(args),
                // Phase 25: Supplier Quality
                "get_supplier_quality_status"     => await ToolGetSupplierQualityStatus(args),
                _                               => null
            };

            if (resultText is null)
                return ErrorResponse(id, -32602, $"Unknown tool: {toolName}");

            // Check if tool returned an error message
            if (resultText.StartsWith("Error:"))
            {
                isSuccess = false;
                errorMessage = resultText;
            }

            if (format == "json")
            {
                // Return structured JSON content block
                var jsonPayload = JsonSerializer.Serialize(new
                {
                    tool    = toolName,
                    success = isSuccess,
                    content = resultText,
                }, _json);
                return OkResponse(id, new
                {
                    content = new[] { new { type = "text", text = jsonPayload, mimeType = "application/json" } },
                    isError = !isSuccess,
                });
            }

            return OkResponse(id, TextContent(resultText));
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            return ErrorResponse(id, -32603, $"Tool execution failed: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            await WriteAuditLogAsync(toolName!, args, resultText, isSuccess, errorMessage, sw.ElapsedMilliseconds);
        }
    }

    // ─── Audit log writer ─────────────────────────────────────────────────────

    private async Task WriteAuditLogAsync(
        string toolName, JsonElement args, string? resultText,
        bool isSuccess, string? errorMessage, long durationMs)
    {
        try
        {
            var action = toolName switch
            {
                _ when toolName.StartsWith("create_") => "Create",
                _ when toolName.StartsWith("complete_") => "Update",
                _ when toolName.StartsWith("transition_") => "Update",
                _ when toolName.StartsWith("record_") => "Create",
                _ => "Read"
            };

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;
            var displayName = User.Identity?.IsAuthenticated == true
                ? (User.FindFirstValue("display_name") ?? User.Identity?.Name)
                : null;

            var log = new McpAuditLog
            {
                ToolName = toolName,
                Action = action,
                UserId = userId,
                UserDisplayName = displayName,
                RequestPayload = args.ValueKind != JsonValueKind.Undefined
                    ? args.GetRawText()
                    : null,
                ResponseSummary = resultText?.Length > 500
                    ? resultText[..500]
                    : resultText,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage?.Length > 2000
                    ? errorMessage[..2000]
                    : errorMessage,
                DurationMs = durationMs,
                PerformedAt = DateTime.UtcNow
            };

            _db.McpAuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Audit logging must never fail the actual tool call
        }
    }

    // ─── Tool implementations ─────────────────────────────────────────────────

    private async Task<string> ToolListProcesses()
    {
        var processes = await _db.Processes
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.ProcessSteps)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!processes.Any()) return "No active processes defined in this system.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Active Processes ({processes.Count})\n");
        foreach (var p in processes)
            sb.AppendLine($"- **{p.Name}** (`{p.Code}`) — {p.ProcessSteps.Count} step(s)" +
                          (string.IsNullOrEmpty(p.Description) ? "" : $"\n  {p.Description}"));
        return sb.ToString();
    }

    private async Task<string> ToolGetProcess(JsonElement args)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(query))
            return "Please provide a 'query' argument with the process code or name.";

        var process = await _db.Processes
            .AsNoTracking()
            .Include(p => p.ProcessSteps)
                .ThenInclude(s => s.StepTemplate)
            .FirstOrDefaultAsync(p =>
                p.Code.ToLower().Contains(query.ToLower()) ||
                p.Name.ToLower().Contains(query.ToLower()));

        if (process is null)
            return $"No process found matching '{query}'. Use list_processes to see all available processes.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Process: {process.Name}\n");
        sb.AppendLine($"- **Code:** `{process.Code}`");
        sb.AppendLine($"- **Status:** {(process.IsActive ? "Active" : "Inactive")}");
        if (!string.IsNullOrEmpty(process.Description))
            sb.AppendLine($"- **Description:** {process.Description}");
        sb.AppendLine();
        sb.AppendLine($"### Steps ({process.ProcessSteps.Count})\n");
        foreach (var step in process.ProcessSteps.OrderBy(s => s.Sequence))
        {
            var name = step.NameOverride ?? step.StepTemplate.Name;
            sb.AppendLine($"{step.Sequence}. **{name}** — template: `{step.StepTemplate.Code}` " +
                          $"(pattern: {step.StepTemplate.Pattern})");
            if (!string.IsNullOrEmpty(step.StepTemplate.Description))
                sb.AppendLine($"   {step.StepTemplate.Description}");
        }
        return sb.ToString();
    }

    private async Task<string> ToolListStepTemplates()
    {
        var templates = await _db.StepTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();

        if (!templates.Any()) return "No active step templates defined in this system.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Active Step Templates ({templates.Count})\n");
        foreach (var t in templates)
            sb.AppendLine($"- **{t.Name}** (`{t.Code}`) — pattern: {t.Pattern}" +
                          (string.IsNullOrEmpty(t.Description) ? "" : $"\n  {t.Description}"));
        return sb.ToString();
    }

    private async Task<string> ToolListActiveJobs()
    {
        var jobs = await _db.Jobs
            .AsNoTracking()
            .Include(j => j.Process)
            .Include(j => j.StepExecutions)
            .Where(j => j.Status == Domain.Enums.JobStatus.Created ||
                        j.Status == Domain.Enums.JobStatus.InProgress ||
                        j.Status == Domain.Enums.JobStatus.OnHold)
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(50)
            .ToListAsync();

        if (!jobs.Any()) return "No active jobs at the moment.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Active Jobs ({jobs.Count})\n");
        foreach (var j in jobs)
        {
            var done  = j.StepExecutions.Count(s =>
                s.Status is Domain.Enums.StepExecutionStatus.Completed or
                            Domain.Enums.StepExecutionStatus.Skipped);
            var total = j.StepExecutions.Count;
            var prog  = total > 0 ? $" — {done}/{total} steps" : "";
            sb.AppendLine($"- **{j.Name}** (`{j.Code}`) — {j.Status}{prog}" +
                          $" | Process: {j.Process.Name}" +
                          (j.Priority > 0 ? $" | Priority: {j.Priority}" : ""));
        }
        return sb.ToString();
    }

    private async Task<string> ToolGetJobStatus(JsonElement args)
    {
        var code = args.TryGetProperty("code", out var c) ? c.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(code))
            return "Please provide a 'code' argument with the job code.";

        var job = await _db.Jobs
            .AsNoTracking()
            .Include(j => j.Process)
            .Include(j => j.StepExecutions.OrderBy(se => se.Sequence))
                .ThenInclude(se => se.ProcessStep)
                    .ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(j => j.Code.ToLower() == code.ToLower());

        if (job is null)
            return $"No job found with code '{code}'. Use list_active_jobs to see current jobs.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Job: {job.Name} (`{job.Code}`)\n");
        sb.AppendLine($"- **Status:** {job.Status}");
        sb.AppendLine($"- **Process:** {job.Process.Name}");
        if (job.Priority > 0)  sb.AppendLine($"- **Priority:** {job.Priority}");
        if (job.StartedAt.HasValue) sb.AppendLine($"- **Started:** {job.StartedAt:g} UTC");
        if (job.CompletedAt.HasValue) sb.AppendLine($"- **Completed:** {job.CompletedAt:g} UTC");
        if (!string.IsNullOrEmpty(job.Description)) sb.AppendLine($"- **Description:** {job.Description}");

        if (job.StepExecutions.Any())
        {
            sb.AppendLine($"\n### Step Progress ({job.StepExecutions.Count} steps)\n");
            foreach (var se in job.StepExecutions)
            {
                var name    = se.ProcessStep?.NameOverride ?? se.ProcessStep?.StepTemplate?.Name ?? $"Step {se.Sequence}";
                var dur     = se.StartedAt.HasValue && se.CompletedAt.HasValue
                    ? $" ({(se.CompletedAt.Value - se.StartedAt.Value).TotalMinutes:F0}m)"
                    : "";
                sb.AppendLine($"{se.Sequence}. [{StatusEmoji(se.Status)}] **{name}** — {se.Status}{dur}");
            }
        }
        return sb.ToString();
    }

    // ─── Phase 7: Quality Engineering Tools ─────────────────────────────────

    private async Task<string> ToolGetPfmea(JsonElement args)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(query))
            return "Please provide a 'query' argument with the PFMEA code or name.";

        var pfmea = await _db.Pfmeas
            .AsNoTracking()
            .Include(p => p.FailureModes)
                .ThenInclude(fm => fm.Actions)
            .FirstOrDefaultAsync(p =>
                p.Code.ToLower().Contains(query.ToLower()) ||
                p.Name.ToLower().Contains(query.ToLower()));

        if (pfmea is null)
            return $"No PFMEA found matching '{query}'.";

        var sb = new StringBuilder();
        sb.AppendLine($"## PFMEA: {pfmea.Name} (`{pfmea.Code}`) — v{pfmea.Version}\n");
        if (!string.IsNullOrEmpty(pfmea.Description))
            sb.AppendLine($"*{pfmea.Description}*\n");

        if (!pfmea.FailureModes.Any())
        {
            sb.AppendLine("No failure modes defined.");
            return sb.ToString();
        }

        sb.AppendLine($"| # | Failure Mode | Effect | SEV | OCC | DET | RPN | Open Actions |");
        sb.AppendLine($"|---|---|---|:---:|:---:|:---:|:---:|:---:|");

        int i = 1;
        foreach (var fm in pfmea.FailureModes.OrderByDescending(f => f.Severity * f.Occurrence * f.Detection))
        {
            var rpn = fm.Severity * fm.Occurrence * fm.Detection;
            var open = fm.Actions.Count(a => a.Status == Domain.Enums.PfmeaActionStatus.Open ||
                                             a.Status == Domain.Enums.PfmeaActionStatus.InProgress);
            sb.AppendLine($"| {i++} | {fm.FailureMode} | {fm.FailureEffect} | {fm.Severity} | {fm.Occurrence} | {fm.Detection} | **{rpn}** | {(open > 0 ? $"{open} open" : "—")} |");
        }

        var highCount = pfmea.FailureModes.Count(f => f.Severity * f.Occurrence * f.Detection >= 200);
        if (highCount > 0)
            sb.AppendLine($"\n⚠️ **{highCount} failure mode(s) with RPN ≥ 200** require immediate attention.");

        return sb.ToString();
    }

    private async Task<string> ToolListHighRpnFailureModes(JsonElement args)
    {
        int top = 20, minRpn = 100;
        if (args.TryGetProperty("top", out var t) && t.TryGetInt32(out var tv)) top = tv;
        if (args.TryGetProperty("min_rpn", out var mr) && mr.TryGetInt32(out var mrv)) minRpn = mrv;

        // Load all and filter in memory (RPN is computed, not stored)
        var allModes = await _db.PfmeaFailureModes
            .AsNoTracking()
            .Include(fm => fm.Pfmea)
            .Include(fm => fm.Actions)
            .ToListAsync();

        var filtered = allModes
            .Select(fm => new { fm, rpn = fm.Severity * fm.Occurrence * fm.Detection })
            .Where(x => x.rpn >= minRpn)
            .OrderByDescending(x => x.rpn)
            .Take(top)
            .ToList();

        if (!filtered.Any())
            return $"No failure modes found with RPN ≥ {minRpn}.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Top Failure Modes by RPN (≥ {minRpn}, showing {filtered.Count})\n");
        sb.AppendLine($"| # | PFMEA | Failure Mode | SEV | OCC | DET | RPN | Open Actions |");
        sb.AppendLine($"|---|---|---|:---:|:---:|:---:|:---:|:---:|");

        int i = 1;
        foreach (var x in filtered)
        {
            var fm = x.fm;
            var open = fm.Actions.Count(a => a.Status == Domain.Enums.PfmeaActionStatus.Open ||
                                             a.Status == Domain.Enums.PfmeaActionStatus.InProgress);
            sb.AppendLine($"| {i++} | {fm.Pfmea?.Name ?? "—"} | {fm.FailureMode} | {fm.Severity} | {fm.Occurrence} | {fm.Detection} | **{x.rpn}** | {(open > 0 ? $"{open} open" : "—")} |");
        }

        return sb.ToString();
    }

    private async Task<string> ToolGetCeMatrix(JsonElement args)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(query))
            return "Please provide a 'query' argument with the matrix name or process step name.";

        var matrix = await _db.CeMatrices
            .AsNoTracking()
            .Include(m => m.Inputs)
            .Include(m => m.Outputs)
            .Include(m => m.Correlations)
            .FirstOrDefaultAsync(m =>
                m.Name.ToLower().Contains(query.ToLower()));

        if (matrix is null)
            return $"No C\u0026E matrix found matching '{query}'.";

        var sb = new StringBuilder();
        sb.AppendLine($"## C\u0026E Matrix: {matrix.Name}\n");
        if (!string.IsNullOrEmpty(matrix.Description))
            sb.AppendLine($"*{matrix.Description}*\n");

        var outputs = matrix.Outputs.OrderBy(o => o.SortOrder).ToList();
        var scoreMap = matrix.Correlations.ToDictionary(c => (c.CeInputId, c.CeOutputId), c => c.Score);

        if (!outputs.Any())
        {
            sb.AppendLine("No outputs defined.");
            return sb.ToString();
        }

        // Header
        sb.Append("| Input | Category |");
        foreach (var o in outputs) sb.Append($" {o.Name} (Imp:{o.Importance}) |");
        sb.AppendLine(" Priority |");

        sb.Append("| --- | --- |");
        foreach (var _ in outputs) sb.Append(" :---: |");
        sb.AppendLine(" :---: |");

        var inputs = matrix.Inputs.OrderBy(i => i.SortOrder).ToList();
        foreach (var inp in inputs)
        {
            var priority = outputs.Sum(o => scoreMap.GetValueOrDefault((inp.Id, o.Id), 0) * o.Importance);
            sb.Append($"| {inp.Name} | {inp.Category} |");
            foreach (var o in outputs)
            {
                var score = scoreMap.GetValueOrDefault((inp.Id, o.Id), 0);
                sb.Append($" {(score == 0 ? "" : score.ToString())} |");
            }
            sb.AppendLine($" **{priority}** |");
        }

        sb.AppendLine($"\n*Importance weights — " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Importance}")) + "*");
        return sb.ToString();
    }

    // ─── Phase 7c: Control Plan Tools ─────────────────────────────────────────

    private async Task<string> ToolGetControlPlan(JsonElement args)
    {
        var query = args.TryGetProperty("query", out var q) ? q.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(query))
            return "Please provide a 'query' argument with the control plan code or name.";

        var cp = await _db.ControlPlans
            .AsNoTracking()
            .Include(cp => cp.Process)
            .Include(cp => cp.Entries)
                .ThenInclude(e => e.ProcessStep)
                    .ThenInclude(ps => ps.StepTemplate)
            .FirstOrDefaultAsync(cp =>
                cp.Code.ToLower().Contains(query.ToLower()) ||
                cp.Name.ToLower().Contains(query.ToLower()));

        if (cp is null)
            return $"No control plan found matching '{query}'.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Control Plan: {cp.Name} (`{cp.Code}`) — v{cp.Version}\n");
        sb.AppendLine($"- **Process:** {cp.Process.Name} (`{cp.Process.Code}`)");
        sb.AppendLine($"- **Status:** {(cp.IsActive ? "Active" : "Inactive")}{(cp.IsStale ? " ⚠️ Stale" : "")}");
        sb.AppendLine();

        if (!cp.Entries.Any())
        {
            sb.AppendLine("No characteristic entries defined.");
            return sb.ToString();
        }

        sb.AppendLine($"| Step | Characteristic | Type | Specification | Measurement | Sample Size | Frequency | Control Method |");
        sb.AppendLine($"|---|---|---|---|---|---|---|---|");

        foreach (var e in cp.Entries.OrderBy(e => e.ProcessStep?.StepTemplate?.Name).ThenBy(e => e.SortOrder))
        {
            var stepName = e.ProcessStep?.NameOverride ?? e.ProcessStep?.StepTemplate?.Name ?? "—";
            sb.AppendLine($"| {stepName} | {e.CharacteristicName} | {e.CharacteristicType} | {e.SpecificationOrTolerance ?? "—"} | {e.MeasurementTechnique ?? "—"} | {e.SampleSize ?? "—"} | {e.SampleFrequency ?? "—"} | {e.ControlMethod ?? "—"} |");
        }

        return sb.ToString();
    }

    private async Task<string> ToolListCriticalCharacteristics(JsonElement args)
    {
        var processQuery = args.TryGetProperty("process_query", out var pq) ? pq.GetString()?.Trim() : null;

        var query = _db.ControlPlans
            .AsNoTracking()
            .Include(cp => cp.Process)
            .Include(cp => cp.Entries)
                .ThenInclude(e => e.ProcessStep)
                    .ThenInclude(ps => ps!.StepTemplate)
            .Where(cp => cp.IsActive);

        if (!string.IsNullOrEmpty(processQuery))
            query = query.Where(cp =>
                cp.Process.Code.ToLower().Contains(processQuery.ToLower()) ||
                cp.Process.Name.ToLower().Contains(processQuery.ToLower()));

        var plans = await query.OrderBy(cp => cp.Process.Name).ThenBy(cp => cp.Name).ToListAsync();

        var productEntries = plans
            .SelectMany(cp => cp.Entries
                .Where(e => e.CharacteristicType == Domain.Enums.CharacteristicType.Product)
                .Select(e => new { cp, e }))
            .OrderBy(x => x.cp.Process.Name)
            .ThenBy(x => x.e.ProcessStep?.StepTemplate?.Name)
            .ThenBy(x => x.e.SortOrder)
            .ToList();

        if (!productEntries.Any())
        {
            var scope = string.IsNullOrEmpty(processQuery) ? "any process" : $"process matching '{processQuery}'";
            return $"No Product-type characteristics found for {scope}.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Critical (Product) Characteristics ({productEntries.Count})\n");
        sb.AppendLine($"| Process | Control Plan | Step | Characteristic | Specification | Measurement |");
        sb.AppendLine($"|---|---|---|---|---|---|");

        foreach (var x in productEntries)
        {
            var stepName = x.e.ProcessStep?.NameOverride ?? x.e.ProcessStep?.StepTemplate?.Name ?? "—";
            sb.AppendLine($"| {x.cp.Process.Name} | {x.cp.Name} | {stepName} | **{x.e.CharacteristicName}** | {x.e.SpecificationOrTolerance ?? "—"} | {x.e.MeasurementTechnique ?? "—"} |");
        }

        return sb.ToString();
    }

    private async Task<string> ToolListRecurringRootCauses(JsonElement args)
    {
        var categoryFilter = args.TryGetProperty("category", out var c) ? c.GetString()?.Trim() : null;
        var topStr         = args.TryGetProperty("top",      out var t) ? t.GetString()?.Trim() : null;
        var top            = int.TryParse(topStr, out var n) ? Math.Clamp(n, 1, 50) : 20;

        var query = _db.RootCauseEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(categoryFilter) &&
            Enum.TryParse<Domain.Enums.RootCauseCategory>(categoryFilter, true, out var catEnum))
            query = query.Where(r => r.Category == catEnum);

        var entries = await query
            .OrderByDescending(r => r.UsageCount)
            .ThenBy(r => r.Title)
            .Take(top)
            .ToListAsync();

        if (!entries.Any())
            return "No root cause entries found matching the criteria.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Root Cause Library — Top {entries.Count} Recurring Causes\n");
        sb.AppendLine("| # | Category | Title | Usage | Tags |");
        sb.AppendLine("|---|---|---|---|---|");

        var rank = 1;
        foreach (var e in entries)
        {
            var tags = string.IsNullOrEmpty(e.Tags) ? "—" : e.Tags;
            sb.AppendLine($"| {rank++} | {e.Category} | {e.Title} | {e.UsageCount} | {tags} |");
        }

        return sb.ToString();
    }

    private async Task<string> ToolGetRcaSummary(JsonElement args)
    {
        var linkedIdStr  = args.TryGetProperty("linked_entity_id", out var li) ? li.GetString()?.Trim() : null;
        var statusFilter = args.TryGetProperty("status",           out var sf) ? sf.GetString()?.Trim() : null;

        Guid? linkedId = Guid.TryParse(linkedIdStr, out var g) ? g : null;
        Domain.Enums.RcaStatus? rcaStatus =
            Enum.TryParse<Domain.Enums.RcaStatus>(statusFilter, true, out var se) ? se : null;

        // ── Ishikawa Diagrams ──
        var iQuery = _db.IshikawaDiagrams
            .AsNoTracking()
            .Include(d => d.Causes)
            .AsQueryable();

        if (linkedId.HasValue)
            iQuery = iQuery.Where(d => d.LinkedEntityId == linkedId);
        if (rcaStatus.HasValue)
            iQuery = iQuery.Where(d => d.Status == rcaStatus.Value);

        var diagrams = await iQuery.OrderByDescending(d => d.CreatedAt).ToListAsync();

        // ── 5 Whys Analyses ──
        var wQuery = _db.FiveWhysAnalyses
            .AsNoTracking()
            .Include(a => a.Nodes)
            .AsQueryable();

        if (linkedId.HasValue)
            wQuery = wQuery.Where(a => a.LinkedEntityId == linkedId);
        if (rcaStatus.HasValue)
            wQuery = wQuery.Where(a => a.Status == rcaStatus.Value);

        var analyses = await wQuery.OrderByDescending(a => a.CreatedAt).ToListAsync();

        if (!diagrams.Any() && !analyses.Any())
            return "No RCA analyses found matching the criteria.";

        var sb = new StringBuilder();
        sb.AppendLine("## RCA Summary\n");

        if (diagrams.Any())
        {
            sb.AppendLine($"### Ishikawa Diagrams ({diagrams.Count})\n");
            sb.AppendLine("| Title | Status | Causes | Root Causes | Trigger |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var d in diagrams)
            {
                var rootCount = d.Causes.Count(c => c.IsSelectedRootCause);
                sb.AppendLine($"| {d.Title} | {d.Status} | {d.Causes.Count} | {rootCount} | {d.LinkedEntityType} |");
            }
            sb.AppendLine();
        }

        if (analyses.Any())
        {
            sb.AppendLine($"### 5 Whys Analyses ({analyses.Count})\n");
            sb.AppendLine("| Title | Status | Nodes | Root Causes | Trigger |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var a in analyses)
            {
                var rootCount = a.Nodes.Count(n => n.IsRootCause);
                sb.AppendLine($"| {a.Title} | {a.Status} | {a.Nodes.Count} | {rootCount} | {a.LinkedEntityType} |");
            }
        }

        return sb.ToString();
    }

    private async Task<string> ToolGetMrbSummary(JsonElement args)
    {
        var statusFilter       = args.TryGetProperty("status",         out var sf) ? sf.GetString()?.Trim() : null;
        var scarStr            = args.TryGetProperty("scar_required",  out var sc) ? sc.GetString()?.Trim() : null;
        var supplierStr        = args.TryGetProperty("supplier_caused", out var su) ? su.GetString()?.Trim() : null;

        var query = _db.MrbReviews
            .AsNoTracking()
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc!.StepExecution)
                    .ThenInclude(se => se!.ProcessStep)
                        .ThenInclude(ps => ps!.StepTemplate)
            .Include(m => m.NonConformance)
                .ThenInclude(nc => nc!.StepExecution)
                    .ThenInclude(se => se!.Job)
            .Include(m => m.Participants)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<Domain.Enums.MrbStatus>(statusFilter, true, out var mrbStatus))
            query = query.Where(m => m.Status == mrbStatus);

        if (bool.TryParse(scarStr, out var scarBool))
            query = query.Where(m => m.ScarRequired == scarBool);

        if (bool.TryParse(supplierStr, out var supplierBool))
            query = query.Where(m => m.SupplierCaused == supplierBool);

        var reviews = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .ToListAsync();

        if (!reviews.Any())
            return "No MRB reviews found matching the criteria.";

        var sb = new StringBuilder();
        sb.AppendLine($"## MRB Reviews ({reviews.Count})\n");
        sb.AppendLine("| # | Job / Step | Item Description | Status | Decision | Flags | Participants |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        int i = 1;
        foreach (var m in reviews)
        {
            var nc      = m.NonConformance;
            var job     = nc?.StepExecution?.Job?.Code ?? "—";
            var step    = nc?.StepExecution?.ProcessStep?.NameOverride
                          ?? nc?.StepExecution?.ProcessStep?.StepTemplate?.Name ?? "—";
            var status  = m.Status.ToString();
            var decision = m.DispositionDecision.HasValue ? m.DispositionDecision.Value.ToString() : "—";
            var flags   = string.Join(", ", new[]
            {
                m.ScarRequired              ? "SCAR" : null,
                m.SupplierCaused            ? "Supplier" : null,
                m.CustomerNotificationRequired ? "Customer" : null,
                m.RequiresRca               ? "RCA" : null,
            }.Where(f => f is not null));
            if (string.IsNullOrEmpty(flags)) flags = "—";
            sb.AppendLine($"| {i++} | {job} / {step} | {m.ItemDescription} | **{status}** | {decision} | {flags} | {m.Participants.Count} |");
        }

        var byStatus = reviews.GroupBy(m => m.Status)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        sb.AppendLine($"\n*Status breakdown: {string.Join(", ", byStatus)}*");

        return sb.ToString();
    }

    private async Task<string> ToolListQmsDocuments(JsonElement args)
    {
        var roleFilter   = args.TryGetProperty("role",   out var r) ? r.GetString()?.Trim() : null;
        var statusFilter = args.TryGetProperty("status", out var s) ? s.GetString()?.Trim() : null;

        var query = _db.Processes.AsNoTracking().AsQueryable();

        // Exclude ManufacturingProcess unless explicitly requested
        if (string.IsNullOrEmpty(roleFilter))
        {
            query = query.Where(p =>
                p.ProcessRole != Domain.Enums.ProcessRole.ManufacturingProcess);
        }
        else if (Enum.TryParse<Domain.Enums.ProcessRole>(roleFilter, true, out var roleEnum))
        {
            query = query.Where(p => p.ProcessRole == roleEnum);
        }

        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<Domain.Enums.ProcessStatus>(statusFilter, true, out var statusEnum))
            query = query.Where(p => p.Status == statusEnum);

        var docs = await query
            .OrderBy(p => p.ProcessRole.ToString())
            .ThenBy(p => p.Name)
            .Take(100)
            .ToListAsync();

        if (!docs.Any())
            return "No QMS documents found matching the criteria.";

        var sb = new StringBuilder();
        sb.AppendLine($"## QMS Documents ({docs.Count})\n");
        sb.AppendLine("| Type | Code | Name | Rev | Version | Status | Effective |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var d in docs)
        {
            var roleLabel = d.ProcessRole switch
            {
                Domain.Enums.ProcessRole.QmsDocument     => "QMS Doc",
                Domain.Enums.ProcessRole.WorkInstruction => "Work Instr.",
                Domain.Enums.ProcessRole.ApprovalProcess => "Approval Template",
                Domain.Enums.ProcessRole.Training        => "Training",
                _                                         => d.ProcessRole.ToString()
            };
            var effective = d.EffectiveDate?.ToString("yyyy-MM-dd") ?? "—";
            sb.AppendLine($"| {roleLabel} | `{d.Code}` | {d.Name} | {d.RevisionCode ?? "—"} | v{d.Version} | **{d.Status}** | {effective} |");
        }

        return sb.ToString();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string StatusEmoji(Domain.Enums.StepExecutionStatus s) => s switch
    {
        Domain.Enums.StepExecutionStatus.Completed  => "✅",
        Domain.Enums.StepExecutionStatus.InProgress => "🔵",
        Domain.Enums.StepExecutionStatus.Failed     => "❌",
        Domain.Enums.StepExecutionStatus.Skipped    => "⏭️",
        _                                            => "⬜"
    };

    private static object TextContent(string text) => new
    {
        content = new[] { new { type = "text", text } },
        isError = false,
    };

    private static object OkResponse(JsonElement id, object result) => new
    {
        jsonrpc = "2.0",
        id      = id.ValueKind == JsonValueKind.Undefined ? (object?)null : (object)id,
        result,
    };

    private static object ErrorResponse(JsonElement id, int code, string message) => new
    {
        jsonrpc = "2.0",
        id      = id.ValueKind == JsonValueKind.Undefined ? (object?)null : (object)id,
        error   = new { code, message },
    };

    private static object Tool(string name, string description, object inputSchema) => new
    {
        name,
        description,
        inputSchema = new
        {
            type       = "object",
            properties = inputSchema,
            required   = Array.Empty<string>(),
        }
    };

    /// <summary>Builds a single-property input schema for a tool argument.
    /// Automatically injects a 'format' parameter that lets callers choose 'markdown' (default) or 'json'.</summary>
    private static object Schema(params (string name, string type, string description)[] props)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (n, t, d) in props)
            dict[n] = new { type = t, description = d };
        dict["format"] = new
        {
            type = "string",
            description = "Response format: 'markdown' (default, human-readable) or 'json' (structured, machine-readable).",
            @enum = new[] { "markdown", "json" },
            @default = "markdown",
        };
        return dict;
    }

    private static bool TryParseRequest(
        JsonElement body,
        out string method,
        out JsonElement id,
        out JsonElement @params,
        out string? error)
    {
        method  = "";
        id      = default;
        @params = default;
        error   = null;

        if (body.ValueKind != JsonValueKind.Object)
        {
            error = "Request must be a JSON object.";
            return false;
        }

        if (!body.TryGetProperty("method", out var m) || m.ValueKind != JsonValueKind.String)
        {
            error = "Missing or invalid 'method' field.";
            return false;
        }

        method = m.GetString()!;
        body.TryGetProperty("id", out id);
        body.TryGetProperty("params", out @params);
        return true;
    }

    private async Task<string> ToolGetCompetencyStatus(JsonElement args)
    {
        var userIdFilter  = args.TryGetProperty("user_id", out var ui) ? ui.GetString()?.Trim() : null;
        var statusFilter  = args.TryGetProperty("status",  out var sf) ? sf.GetString()?.Trim() : null;

        var query = _db.CompetencyRecords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(userIdFilter))
            query = query.Where(r => r.UserId == userIdFilter);

        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<Domain.Enums.CompetencyStatus>(statusFilter, true, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);
        else
            query = query.Where(r => r.Status != Domain.Enums.CompetencyStatus.Superseded);

        var records = await query
            .Include(r => r.TrainingProcess)
            .OrderBy(r => r.UserDisplayName)
            .ThenBy(r => r.Status.ToString())
            .Take(200)
            .ToListAsync();

        if (!records.Any())
            return "No competency records found matching the criteria.";

        var now = DateTime.UtcNow;
        var soon = now.AddDays(30);

        var sb = new StringBuilder();
        sb.AppendLine($"## Competency Records ({records.Count})\n");
        sb.AppendLine("| User | Training Course | Competency | Status | Completed | Expires |");
        sb.AppendLine("|---|---|---|---|---|---|");

        foreach (var r in records)
        {
            var statusLabel = r.Status.ToString();
            if (r.Status == Domain.Enums.CompetencyStatus.Current && r.ExpiresAt.HasValue && r.ExpiresAt.Value <= soon)
                statusLabel = "Current (Expiring Soon)";

            var expiry = r.ExpiresAt.HasValue
                ? r.ExpiresAt.Value.ToString("yyyy-MM-dd")
                : "No expiry";

            var competencyTitle = r.TrainingProcess?.CompetencyTitle ?? r.TrainingProcess?.Name ?? r.TrainingProcessId.ToString();
            sb.AppendLine($"| {r.UserDisplayName} | {r.TrainingProcess?.Name ?? r.TrainingProcessId.ToString()} | {competencyTitle} | **{statusLabel}** | {r.CompletedAt:yyyy-MM-dd} | {expiry} |");
        }

        // Summary
        var total    = records.Count;
        var current  = records.Count(r => r.Status == Domain.Enums.CompetencyStatus.Current);
        var expired  = records.Count(r => r.Status == Domain.Enums.CompetencyStatus.Expired);
        var expiring = records.Count(r => r.Status == Domain.Enums.CompetencyStatus.Current && r.ExpiresAt.HasValue && r.ExpiresAt.Value <= soon);

        sb.AppendLine($"\n*Summary: {current}/{total} current, {expired} expired, {expiring} expiring within 30 days.*");

        return sb.ToString();
    }

    private async Task<string> ToolGetManagementReviewStatus(JsonElement args)
    {
        var statusFilter        = args.TryGetProperty("status",               out var sf) ? sf.GetString()?.Trim() : null;
        var includeActionItems  = args.TryGetProperty("include_action_items", out var ia) ? ia.GetString()?.Trim() : null;
        var showActions         = string.Equals(includeActionItems, "true", StringComparison.OrdinalIgnoreCase);

        var query = _db.ManagementReviews.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<Domain.Enums.ManagementReviewStatus>(statusFilter, true, out var parsed))
            query = query.Where(r => r.Status == parsed);

        var reviews = await query
            .OrderByDescending(r => r.ScheduledDate)
            .Take(50)
            .ToListAsync();

        if (!reviews.Any())
            return "No management reviews found matching the criteria.";

        Dictionary<Guid, int> actionCounts = new();
        if (showActions)
        {
            var reviewIds = reviews.Select(r => r.Id).ToList();
            actionCounts = await _db.ActionItems
                .AsNoTracking()
                .Where(a => a.SourceType == Domain.Enums.ActionItemSourceType.ManagementReview
                         && a.SourceEntityId.HasValue
                         && reviewIds.Contains(a.SourceEntityId.Value))
                .GroupBy(a => a.SourceEntityId!.Value)
                .Select(g => new { ReviewId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ReviewId, x => x.Count);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Management Reviews ({reviews.Count})\n");
        if (showActions)
        {
            sb.AppendLine("| Title | Type | Scheduled | Status | Conducted By | Action Items |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var r in reviews)
            {
                var count = actionCounts.GetValueOrDefault(r.Id, 0);
                sb.AppendLine($"| {r.Title} | {r.ReviewType} | {r.ScheduledDate:yyyy-MM-dd} | {r.Status} | {r.ConductedBy ?? "—"} | {count} |");
            }
        }
        else
        {
            sb.AppendLine("| Title | Type | Scheduled | Status | Conducted By |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var r in reviews)
                sb.AppendLine($"| {r.Title} | {r.ReviewType} | {r.ScheduledDate:yyyy-MM-dd} | {r.Status} | {r.ConductedBy ?? "—"} |");
        }

        return sb.ToString();
    }

    private async Task<string> ToolGetProductionStatus()
    {
        var now = DateTime.UtcNow;

        // Active jobs
        var activeJobs = await _db.Jobs
            .AsNoTracking()
            .Include(j => j.Process)
            .Where(j => j.Status == Domain.Enums.JobStatus.InProgress || j.Status == Domain.Enums.JobStatus.Created)
            .ToListAsync();

        // Equipment currently down
        var downEquipment = await _db.DowntimeRecords
            .AsNoTracking()
            .Include(d => d.Equipment)
            .Where(d => d.EndedAt == null)
            .Select(d => d.Equipment!.Code)
            .Distinct()
            .ToListAsync();

        // Maintenance due/overdue
        var maintenanceDue = await _db.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.Equipment)
            .Where(t => t.Status == Domain.Enums.MaintenanceTaskStatus.Due || t.Status == Domain.Enums.MaintenanceTaskStatus.Overdue)
            .OrderBy(t => t.DueDate)
            .Take(20)
            .ToListAsync();

        var lateJobs = activeJobs.Where(j => j.DueDate.HasValue && j.DueDate.Value < now).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("## Production Status\n");
        sb.AppendLine($"- **Active Jobs:** {activeJobs.Count}");
        sb.AppendLine($"- **Late Jobs:** {lateJobs.Count}");
        sb.AppendLine($"- **Equipment Currently Down:** {downEquipment.Count}" +
                      (downEquipment.Any() ? $" ({string.Join(", ", downEquipment)})" : ""));
        sb.AppendLine($"- **Maintenance Due/Overdue:** {maintenanceDue.Count}");

        if (lateJobs.Any())
        {
            sb.AppendLine("\n### Late Jobs");
            foreach (var j in lateJobs.OrderBy(j => j.DueDate))
            {
                var daysLate = (int)(now - j.DueDate!.Value).TotalDays;
                sb.AppendLine($"- **{j.Code}** — {j.Process?.Name ?? "?"} — {daysLate} day(s) late (due {j.DueDate.Value:MMM d, yyyy})");
            }
        }

        if (maintenanceDue.Any())
        {
            sb.AppendLine("\n### Maintenance Due / Overdue");
            foreach (var t in maintenanceDue)
                sb.AppendLine($"- **{t.Equipment?.Code ?? "?"}** — {t.Title} — {t.Status} (due {t.DueDate:MMM d, yyyy})");
        }

        return sb.ToString();
    }

    private async Task<string> ToolListEquipmentDowntime(JsonElement args)
    {
        var currentlyDownOnly = args.TryGetProperty("currently_down_only", out var c) && c.GetString() == "true";

        IQueryable<Domain.Entities.DowntimeRecord> query = _db.DowntimeRecords
            .AsNoTracking()
            .Include(d => d.Equipment);

        if (currentlyDownOnly)
            query = query.Where(d => d.EndedAt == null);

        var records = await query
            .OrderByDescending(d => d.StartedAt)
            .Take(50)
            .ToListAsync();

        if (!records.Any())
            return currentlyDownOnly ? "No equipment is currently down." : "No downtime records found.";

        var sb = new StringBuilder();
        sb.AppendLine(currentlyDownOnly
            ? $"## Equipment Currently Down ({records.Count})\n"
            : $"## Equipment Downtime History ({records.Count} most recent)\n");

        foreach (var r in records)
        {
            var duration = r.EndedAt.HasValue
                ? $"{(r.EndedAt.Value - r.StartedAt).TotalMinutes:F0} min"
                : "ongoing";
            sb.AppendLine($"- **{r.Equipment?.Code ?? "?"}** — {r.Type} — {r.StartedAt:MMM d HH:mm} → {(r.EndedAt.HasValue ? r.EndedAt.Value.ToString("MMM d HH:mm") : "now")} ({duration})");
            sb.AppendLine($"  Reason: {r.Reason}");
        }

        return sb.ToString();
    }

    private async Task<string> ToolListOverdueMaintenance(JsonElement args)
    {
        var statusFilter = args.TryGetProperty("status", out var s) ? s.GetString() : null;

        var statuses = new List<Domain.Enums.MaintenanceTaskStatus>();
        if (string.IsNullOrEmpty(statusFilter))
        {
            statuses.Add(Domain.Enums.MaintenanceTaskStatus.Overdue);
            statuses.Add(Domain.Enums.MaintenanceTaskStatus.Due);
        }
        else if (Enum.TryParse<Domain.Enums.MaintenanceTaskStatus>(statusFilter, true, out var parsed))
        {
            statuses.Add(parsed);
        }
        else
        {
            return $"Invalid status '{statusFilter}'. Use Overdue, Due, or InProgress.";
        }

        var tasks = await _db.MaintenanceTasks
            .AsNoTracking()
            .Include(t => t.Equipment)
            .Where(t => statuses.Contains(t.Status))
            .OrderBy(t => t.DueDate)
            .Take(50)
            .ToListAsync();

        if (!tasks.Any())
            return "No maintenance tasks with the requested status.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Maintenance Tasks — {string.Join(" / ", statuses)} ({tasks.Count})\n");
        sb.AppendLine("| Equipment | Title | Type | Due Date | Assigned | Status |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var t in tasks)
            sb.AppendLine($"| {t.Equipment?.Code ?? "?"} | {t.Title} | {t.Type} | {t.DueDate:MMM d, yyyy} | {t.AssignedTo ?? "—"} | **{t.Status}** |");

        return sb.ToString();
    }

    private async Task<string> ToolGetInventoryStatus(JsonElement args)
    {
        var locationIdStr = args.TryGetProperty("location_id",    out var li) ? li.GetString()?.Trim() : null;
        var lowStockStr   = args.TryGetProperty("low_stock_only", out var ls) ? ls.GetString()?.Trim() : null;

        Guid? locationId  = Guid.TryParse(locationIdStr, out var locGuid) ? locGuid : null;
        bool  lowStockOnly = string.Equals(lowStockStr, "true", StringComparison.OrdinalIgnoreCase);

        var query = _db.Items
            .AsNoTracking()
            .Include(i => i.Kind)
            .Include(i => i.StorageLocation)
            .Where(i => i.StorageLocationId != null);

        if (locationId.HasValue)
            query = query.Where(i => i.StorageLocationId == locationId.Value);

        var items = await query.ToListAsync();

        var groups = items
            .GroupBy(i => i.KindId)
            .Select(g =>
            {
                var kind     = g.First().Kind;
                var count    = g.Count();
                var location = g.Select(i => i.StorageLocation?.Code).Distinct().OrderBy(c => c).ToList();
                return new
                {
                    KindCode          = kind.Code,
                    KindName          = kind.Name,
                    OnHand            = count,
                    UoM               = kind.UnitOfMeasure ?? "Each",
                    Location          = string.Join(", ", location),
                    ReorderThreshold  = kind.ReorderThreshold,
                };
            })
            .OrderBy(x => x.KindCode)
            .ToList();

        if (lowStockOnly)
            groups = groups.Where(x => x.ReorderThreshold.HasValue && x.OnHand < (int)x.ReorderThreshold.Value).ToList();

        if (!groups.Any())
            return lowStockOnly
                ? "No Kinds are currently below their reorder threshold."
                : "No items with a storage location found.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Inventory Status ({groups.Count} Kind(s))\n");
        sb.AppendLine("| Kind Code | Kind Name | On Hand | UoM | Location | Reorder Threshold |");
        sb.AppendLine("|---|---|---:|---|---|---:|");

        foreach (var g in groups)
        {
            var threshold = g.ReorderThreshold.HasValue ? g.ReorderThreshold.Value.ToString("0") : "—";
            var flag      = g.ReorderThreshold.HasValue && g.OnHand < (int)g.ReorderThreshold.Value ? " ⚠️" : "";
            sb.AppendLine($"| `{g.KindCode}` | {g.KindName} | {g.OnHand}{flag} | {g.UoM} | {g.Location} | {threshold} |");
        }

        var belowThreshold = groups.Count(x => x.ReorderThreshold.HasValue && x.OnHand < (int)x.ReorderThreshold.Value);
        if (belowThreshold > 0)
            sb.AppendLine($"\n⚠️ **{belowThreshold} Kind(s) below reorder threshold.**");

        return sb.ToString();
    }

    // ─── REST endpoint for Blazor audit log page ─────────────────────────────

    [HttpGet("audit")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string? toolName = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.McpAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(toolName))
            query = query.Where(l => l.ToolName.Contains(toolName));
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(l => l.UserId != null && l.UserId.Contains(userId));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);
        if (isSuccess.HasValue)
            query = query.Where(l => l.IsSuccess == isSuccess.Value);
        if (dateFrom.HasValue)
            query = query.Where(l => l.PerformedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(l => l.PerformedAt <= dateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new McpAuditLogDto(
                l.Id, l.ToolName, l.Action, l.EntityType, l.EntityId,
                l.UserId, l.UserDisplayName, l.RequestPayload, l.ResponseSummary,
                l.IsSuccess, l.ErrorMessage, l.DurationMs, l.PerformedAt))
            .ToListAsync();

        return Ok(new PaginatedResponse<McpAuditLogDto>(items, totalCount, page, pageSize));
    }
}
