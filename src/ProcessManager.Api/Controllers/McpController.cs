using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;

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
public class McpController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private const string ProtocolVersion = "2024-11-05";
    private const string ServerName      = "ProcessManager";
    private const string ServerVersion   = "1.2";

    public McpController(ProcessManagerDbContext db) => _db = db;

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
                 new { }),

            Tool("list_processes",
                 "List all active process definitions in the system, including step count. Requires authentication.",
                 new { }),

            Tool("get_process",
                 "Get detailed information about a specific process, including its ordered steps. Requires authentication.",
                 Schema(("query", "string", "Process code or name (partial match supported)"))),

            Tool("list_step_templates",
                 "List all active step template definitions (reusable work unit designs). Requires authentication.",
                 new { }),

            Tool("list_active_jobs",
                 "List all jobs currently in Created, InProgress, or OnHold status. Requires authentication.",
                 new { }),

            Tool("get_job_status",
                 "Get the current status and step-by-step execution progress of a specific job. Requires authentication.",
                 Schema(("code", "string", "Job code (exact match)"))),
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

        // Public tool — no auth required
        if (toolName == "describe_domain")
            return OkResponse(id, TextContent(HelpController.ContextDocumentPublic));

        // All other tools require authentication
        if (User.Identity?.IsAuthenticated != true)
            return ErrorResponse(id, -32001,
                "Authentication required. Include 'Authorization: Bearer {jwt}' in your request. " +
                "Obtain a token via POST /api/auth/login.");

        return toolName switch
        {
            "list_processes"      => OkResponse(id, TextContent(await ToolListProcesses())),
            "get_process"         => OkResponse(id, TextContent(await ToolGetProcess(args))),
            "list_step_templates" => OkResponse(id, TextContent(await ToolListStepTemplates())),
            "list_active_jobs"    => OkResponse(id, TextContent(await ToolListActiveJobs())),
            "get_job_status"      => OkResponse(id, TextContent(await ToolGetJobStatus(args))),
            _                     => ErrorResponse(id, -32602, $"Unknown tool: {toolName}")
        };
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

    /// <summary>Builds a single-property input schema for a tool argument.</summary>
    private static object Schema(params (string name, string type, string description)[] props)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (n, t, d) in props)
            dict[n] = new { type = t, description = d };
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
}
