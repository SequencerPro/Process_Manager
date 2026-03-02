# Process Manager — Project Plan

## Version History

| Version | Date       | Notes                          |
|---------|------------|--------------------------------|
| 0.1     | 2026-02-16 | Initial draft                  |
| 0.2     | 2026-02-17 | All phases 1-5 implemented, cross-cutting improvements added |
| 0.3     | 2026-02-21 | API fixes (IsActive toggle, Workflow versioning), Blazor detail pages for Items/Batches/StepExecutions |
| 0.4     | 2026-02-21 | CRUD modals on all detail pages, 17 Blazor pages complete |
| 0.5     | 2026-02-22 | Full UI polish: cascading port dropdowns, workflow validation UI, port transaction forms, delete confirmations on all pages, empty-state messages, display name fixes (JobName/BatchCode), StepExecution job filter, WorkflowDetail link condition management |
| 0.6     | 2026-03-02 | Audit trail wired up (CreatedBy/UpdatedBy via IHttpContextAccessor); multi-tenancy architecture decision documented |
| 0.7     | 2026-03-10 | RunChartWidget component — per-step-template run charts on StepTemplateDetail |
| 0.8     | 2026-03-10 | Ad-hoc analytics chart builder (AnalyticsController, TimeSeriesChart.razor, Analytics page) |
| 0.9     | 2026-03-10 | Dashboard page — KPI cards, job status breakdown, 30-day throughput, step performance, recent completions |
| 1.0     | 2026-03-10 | Out-of-range alerting — AlertsController, Alerts page, NavMenu bell badge with live count |
| 1.1     | 2026-03-10 | Execution Gantt timeline on JobDetail; CSV export endpoints (step executions, alerts); integration tests for Analytics and Alerts |
| 1.2     | 2026-03-02 | AI integration: `/api/help/context` public context document; `/mcp` MCP server with live-data tools for company AI assistants |
| 1.3     | 2026-03-02 | Project plan updated with Phase 7 design intent: PFMEA builder and C&E matrix builder |

---

## Vision

A system that treats manufacturing (and other business) process designs as the central organizing structure of an enterprise. The process model is the schema from which planning, accounting, sales, EHS, and other functions derive their data. By investing in rigorous process definition, a company maximizes its ability to understand, track, and improve its operations.

---

## Design Principles

1. **Process model is the core.** Everything else is a consumer of or contributor to the process model.
2. **Design together, build incrementally.** The data model for Phases 1–3 is designed as a unit before any code is written, then built phase by phase.
3. **Each phase delivers standalone value.** A manufacturing engineer can benefit from the system before it is "complete."
4. **Domain-neutral internals, domain-specific labels.** The system uses generic terms (Kind, Grade, Item) internally and maps them to user-facing vocabulary (Part, Disposition, Serial Number) via configuration.
5. **Type safety prevents errors.** Ports enforce Item Types (Kind + Grade) so that wrong items cannot flow to wrong places.

---

## Phased Build Sequence

### Phase 1 — Type System (Kind, Grade, Tracking Levels) ✅

**Goal:** Define *what things are.*

**Status:** Implemented — full CRUD API and Blazor UI (KindList, KindDetail with inline grade management)

**Delivers:**
- Ability to catalog all Kinds (parts, materials, documents, etc.)
- Define Grades per Kind (Raw, Passed, Failed-Dimensional, etc.)
- Set tracking flags per Kind (Serialized, Batchable)
- Configure domain vocabulary mapping

**Standalone value:** A formal, searchable parts/materials catalog with classification — replaces spreadsheets and tribal knowledge.

**Key entities:**
- Kind
- Grade
- Domain Vocabulary Config

---

### Phase 2 — Step Design (Steps with Ports) ✅

**Goal:** Define *what work looks like.*

**Status:** Implemented — full CRUD API and Blazor UI (StepTemplateList, StepTemplateDetail with port management)

**Delivers:**
- Design individual Steps with named Input and Output Ports
- Each Port declares exactly one Item Type (Kind + Grade) and a Quantity Rule
- Steps are classified by pattern (Transform, Assembly, Division, General)
- Steps are reusable — designed once, used in multiple Processes

**Standalone value:** Documented operations with formal input/output definitions. Already more rigorous than most shops have.

**Key entities:**
- Step (template/definition)
- Port (Input / Output)
- Quantity Rule

---

### Phase 3 — Process Composition ✅

**Goal:** Arrange Steps into linear sequences.

**Status:** Implemented — full CRUD API and Blazor UI (ProcessList, ProcessDetail with step/flow management, cascading port dropdowns for flow creation, step override editing, process validation endpoint)

**Delivers:**
- Create Processes as ordered sequences of Steps
- Validate port compatibility between consecutive Steps (output ports of step N connect to input ports of step N+1)
- Define Flows (the connections between ports of adjacent steps)
- Version and manage Process definitions

**Standalone value:** Complete process plans (routings) — replaces paper travelers and undocumented tribal knowledge.

**Key entities:**
- Process
- Process Step (a Step placed at a position in a Process)
- Flow (port-to-port connection between adjacent Process Steps)

---

### Phase 4 — Workflow Composition ✅

**Goal:** Connect Processes into directed graphs with routing decisions.

**Status:** Implemented — full CRUD API and Blazor UI (WorkflowList, WorkflowDetail with process/link management, link condition add/remove, Validate button with results panel, edit modals for processes and links)

**Delivers:**
- Workflow CRUD with versioning and active flag
- WorkflowProcess nodes linking Processes into a workflow graph
- WorkflowLink edges with routing types (Always, GradeBased, Manual)
- WorkflowLinkCondition for grade-based routing decisions
- Validation endpoint checking entry points, link completeness, interface compatibility

**Key entities:**
- Workflow
- WorkflowProcess
- WorkflowLink
- WorkflowLinkCondition
- RoutingType enum

---

### Phase 5 — Execution / Runtime ✅

**Goal:** Track real work flowing through designed processes.

**Status:** Implemented — full CRUD API and Blazor UI (JobList/Detail, ItemList/Detail, BatchList/Detail, StepExecutionList/Detail with port transaction creation, execution data capture, lifecycle transitions)

**Delivers:**
- Create and manage Jobs with lifecycle transitions (Created → InProgress → OnHold → Completed/Cancelled)
- Create, track, and flow Items and Batches through Steps
- Record Step Executions with status transitions, port transactions, and data
- Port transaction form with port/item/batch dropdowns driven by job context
- Data association at three levels (Step Execution, Batch, Item)
- Top-level list endpoints for Items, Batches, and StepExecutions with filtering

**Key entities:**
- Job
- Item
- Batch
- StepExecution
- PortTransaction
- ExecutionData

---

### Cross-Cutting Improvements ✅

**Applied across all phases:**
- **Pagination:** All GetAll endpoints return `PaginatedResponse<T>` with `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`
- **Search:** All list endpoints accept `?search=` parameter filtering on Code/Name
- **Filtering:** `?active=` on StepTemplates/Processes/Workflows; `?status=` on Jobs/Items/Batches/StepExecutions; `?processId=` on Jobs; `?jobId=`/`?kindId=` on Items/Batches/StepExecutions
- **DTO Validation:** All Create/Update DTOs have `[Required]`, `[StringLength]`, `[Range]` attributes, auto-validated by `[ApiController]`
- **Lightweight Listing:** ProcessesController GetAll returns `ProcessSummaryResponseDto` (no Steps/Flows graph) for performance
- **Type Safety:** ExecutionData DataType uses `DataValueType` enum instead of string
- **Eager Loading:** All Item and Batch queries include `.Include(Job)` and `.Include(Batch)` so response DTOs carry `JobName` and `BatchCode` for display without extra round-trips
- **UI Polish:** Delete confirmations (JS `confirm()`) on all destructive actions; empty-state messages on all list tables; friendly names (JobName, BatchCode) in list views instead of raw GUIDs

---

### Phase 6 — Production Infrastructure (in progress)

**Goal:** Make the system deployable in real multi-user environments.

**Completed:**
- **PostgreSQL:** SQLite replaced with PostgreSQL (Npgsql 8.0.11). `Program.cs` includes a `ToNpgsqlConnectionString()` helper that converts the `postgresql://` URL injected by Render into an Npgsql-compatible connection string. `appsettings.json` carries a localhost default for development; the production connection string is supplied via environment variable.
- **EF Core Migrations:** `Database.EnsureCreated()` replaced with a proper migrations pipeline. `20260301175025_InitialCreate` covers the full initial schema with Npgsql identity column annotations.
- **Authentication & Authorization:** JWT-based auth with Admin and Engineer roles. Full user management UI (list, add, delete) in the Blazor admin panel.
- **Audit Trail:** `ProcessManagerDbContext.SetAuditFields()` automatically populates `CreatedAt`, `UpdatedAt`, `CreatedBy`, and `UpdatedBy` on every `SaveChanges`/`SaveChangesAsync` call, using `IHttpContextAccessor` to resolve the current user's username from the JWT principal. All four fields are already in `BaseEntity` and covered by the `InitialCreate` migration — no schema change required.

**Remaining:**
- **Multi-tenancy:** See architecture decision below.

---

## Architecture Decision: Multi-Tenancy (2026-03-02)

### Deployment Models

The system must support two deployment scenarios:
1. **SaaS** — hosted on Render, serving multiple independent companies from the same deployment.
2. **On-premises** — a single company runs the system on their own hardware with their own database.

### Decision: Database-per-Tenant (Option B), deferred until a second real tenant exists

Three options were evaluated:

| Option | Description | Verdict |
|---|---|---|
| A: Row-level tenancy | `TenantId` column on every table; single shared database | Rejected for now — invasive to implement, data leakage risk, premature at current scale |
| B: Database-per-tenant | Each tenant gets their own PostgreSQL database; middleware resolves connection string per request | **Selected** — strong isolation, no schema changes, natural fit for both deployment models |
| C: Deployment-per-tenant | Separate Render service + DB per customer (current state) | Fine now, but operationally unscalable beyond a handful of customers |

### How It Works

- A lightweight "management" database holds a `Tenants` table mapping subdomains/identifiers to connection strings.
- A tenant resolver middleware reads the request hostname, looks up the tenant, and selects the appropriate connection string before the request hits any controller.
- Migrations run independently per tenant database using the same migration pipeline already in place.
- On-premises customers provision one database and set `TenancyMode: SingleTenant` in `appsettings.json` — the middleware is bypassed and the connection string comes directly from config.
- **No changes to any entity, query, or migration are required.** The same binaries and Docker image serve both deployment models.

### When to Build It

Do not build multi-tenancy infrastructure until a second real SaaS tenant is being onboarded. The current single-deployment model (Option C) is sufficient until then. If database-per-tenant ever becomes operationally unmanageable at scale (many dozens of tenants), revisit Option A.

---

---

## Architecture Decision: AI Integration (2026-03-02)

### Problem

Users — particularly new users — need help understanding how to configure the system (e.g., how to design a Process, what a Port is, when to use a Workflow). Building a proprietary AI chatbot would require managing API keys, costs, and a separate service. Many companies already have a licensed AI assistant (Microsoft Copilot, ChatGPT Enterprise, Claude, etc.) available on their networks.

### Decision: BYOAI (Bring Your Own AI) via two integration surfaces

Rather than embedding an AI, the system exposes structured integration surfaces that any external AI can consume. The company points their AI at these endpoints and it gains immediate, contextual knowledge of the system.

#### Surface 1: Context Document (`GET /api/help/context`)

A public, unauthenticated endpoint returning a comprehensive markdown document covering:
- All core concepts (Kind, Grade, StepTemplate, Process, Workflow, Job, StepExecution)
- How-to guides (building a process, setting up a workflow, recording step data)
- Terminology quick reference
- API endpoint overview for the most common operations

Any AI can consume this by including the URL in its system prompt or having IT pre-load it. No integration work required — paste the URL and the AI understands the domain.

#### Surface 2: MCP Server (`POST /mcp`)

[Model Context Protocol](https://modelcontextprotocol.io/) is the emerging standard (supported by Microsoft Copilot, GitHub Copilot agent mode, Claude Desktop, and others) for exposing live-data tools to AI assistants. The MCP server exposes tools that the AI can call to answer questions about the live system state:

| Tool | Auth | Description |
|---|---|---|
| `describe_domain` | Public | Returns the full domain context document |
| `list_processes` | Bearer token | Lists all active process definitions |
| `get_process` | Bearer token | Gets a process and its steps by name/code |
| `list_step_templates` | Bearer token | Lists all active step template definitions |
| `list_active_jobs` | Bearer token | Lists jobs currently in Created/InProgress/OnHold |
| `get_job_status` | Bearer token | Gets current state and step progress of a specific job |

MCP discovery methods (`initialize`, `tools/list`, `resources/list`) are unauthenticated. All `tools/call` with data access require a valid Bearer JWT — admins create a service account for the AI client.

### When to Build More

If a tool is frequently asked for by users but not yet exposed, add it to the MCP server. The architecture supports adding tools without any deployment model changes.

---

### Phase 7 — Quality Engineering Tools

**Goal:** Embed risk analysis and input-control evaluation directly into the process model so that PFMEA and C&E matrices are always in sync with the process and step definitions, not living in disconnected spreadsheets.

**Status:** Planned — see Architecture Decision below.

#### 7a — PFMEA Builder and Repository

A **Process Failure Mode and Effects Analysis (PFMEA)** identifies how each step in a process can fail, what the consequences are, and what controls are in place. The builder auto-generates a PFMEA shell from any defined Process and then provides an interface for engineering teams to complete it.

**Delivers:**
- Create a PFMEA for any Process; the system pre-populates one entry row per ProcessStep
- For each step entry: describe the step's function, add one or more failure modes, each with:
  - Failure mode description (how the step fails)
  - Failure effect (consequence to the customer / next step)
  - Failure cause (root cause or mechanism)
  - Current prevention controls (text)
  - Current detection controls (text)
  - Severity (S), Occurrence (O), Detection (D) — each rated 1–10
  - Risk Priority Number = S × O × D (computed automatically)
- Action items per failure mode:
  - Responsible person, target date, status (Open / InProgress / Completed / Cancelled)
  - Completion notes
  - Revised Occurrence and Revised Detection ratings after action completion
  - Revised RPN = S × RevisedO × RevisedD (Severity does not change — it reflects the effect, not the control)
- PFMEA versioning: when the underlying Process changes, a new PFMEA version can be branched from the previous one, preserving the history of risk decisions
- Repository view: browse all PFMEAs across all processes with RPN heat-map sortable by highest current risk

**Key entities:**
- `Pfmea` (Id, ProcessId, Name, Version, IsActive)
- `PfmeaFailureMode` (Id, PfmeaId, ProcessStepId, StepFunction, FailureMode, FailureEffect, FailureCause, PreventionControls, DetectionControls, Severity, Occurrence, Detection → RPN computed)
- `PfmeaAction` (Id, FailureModeId, Description, ResponsiblePerson, TargetDate, Status, CompletedDate, CompletionNotes, RevisedOccurrence, RevisedDetection → RevisedRPN computed)

---

#### 7b — C&E Matrix Builder

A **Cause and Effect (C&E) Matrix** (also known as a Cause and Effect Diagram Matrix) evaluates the degree to which each input of a process step influences each of its outputs. It produces a priority ranking of inputs so teams focus improvement effort on the inputs that most affect the things customers care about. The matrix lives at the **ProcessStep** level.

**Delivers:**
- Each ProcessStep can have one C&E matrix
- **Inputs (rows):** two sources, combined in one list
  - *Port inputs* — automatically linked from the step's existing input ports (the Items flowing in)
  - *Free-form factors* — user-added control or noise factors (e.g. "spindle speed", "ambient humidity", "fixture clamping force")
  - Each input is categorised: **Controllable Input** or **Noise Factor**
- **Outputs (columns):** two sources, combined in one list
  - *Port outputs* — automatically linked from the step's existing output ports (the Items flowing out)
  - *Quality characteristics* — user-added named characteristics (e.g. "flatness", "tensile strength", "surface finish")
  - Each output has an **Importance** weight (1–10)
- **Correlation cells:** for every input × output pair the user scores the relationship: 0 (none), 1 (weak), 3 (moderate), 9 (strong)
- **Computed Priority Score** per input = Σ (CorrelationScore × OutputImportance) across all outputs
- UI sorts inputs by Priority Score descending — the top of the list is where to focus improvement energy
- Matrix can be exported to CSV

**Key entities:**
- `CeMatrix` (Id, ProcessStepId, Name, Description)
- `CeInput` (Id, CeMatrixId, Name, Category: PortInput/ControlFactor/NoiseFactor, PortId nullable, SortOrder)
- `CeOutput` (Id, CeMatrixId, Name, Category: PortOutput/QualityCharacteristic, PortId nullable, Importance, SortOrder)
- `CeCorrelation` (Id, CeInputId, CeOutputId, Score: 0/1/3/9)

---

## Architecture Decision: Quality Engineering Tools (2026-03-02)

### Scope and Placement

Quality tools are tightly coupled to the process model: a PFMEA is meaningless without the process structure it analyses, and a C&E matrix is meaningless without the step inputs and outputs it relates. Embedding them inside the same application (rather than exporting to Excel) means they stay in sync when processes are revised and their data can contribute to analytics and alerts.

### PFMEA Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Risk scoring standard | Custom — S × O × D = RPN (1–1000) | Avoids strict AIAG/VDA standard dependency; teams already know this format; can tighten to full AIAG compliance later |
| Severity immutability | S never changes on actions; only O and D are revised | Severity is a property of the effect (harm to customer/next step), not the control — this is standard practice |
| Process coupling | PFMEA linked to ProcessId; auto-populated from ProcessSteps | Auto-population removes setup burden; engineers add failure modes on top |
| Versioning | New PFMEA version branched from previous when process changes | Preserves audit trail of risk decisions over time |
| Action items | Simple tracking (person/date/status) + before/after risk fields | Closes the loop without requiring a full task management system |

### C&E Matrix Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Granularity | One matrix per ProcessStep (not per Process) | Keeps the analysis at the right level; a process-level matrix would mix different mechanisms and be too coarse |
| Correlation scale | 0 / 1 / 3 / 9 | Standard QFD/C&E scale used across industries; the gaps (0→1→3→9) force analysts to be deliberate about distinction between weak and strong relationships |
| Input categories | Controllable vs. Noise | Distinguishes factors the process can control from sources of variation it cannot — drives different engineering responses |
| Port linkage | Port inputs/outputs are pre-linked but display name is editable | Keeps the matrix in sync with the process model while allowing user-friendly labels |
| Priority formula | Σ (Score × OutputImportance) | Simple, widely understood, directly produces an actionable ranked list |

### Relationship to Existing Features

- A high-RPN PFMEA failure mode whose `CurrentDetectionControls` describe a measurement prompt gives engineers a path to configure an **out-of-range alert** on that prompt — connecting PFMEA risk identification to live operational detection.
- C&E matrix priority scores are natural candidates for future **analytics overlays** (e.g. colouring run-chart series by input priority).
- Both tools will be exposed via the **MCP server** (Phase 7 MCP tools: `get_pfmea`, `list_high_rpn_failure_modes`, `get_ce_matrix`).

---

### Phase 8+ — Integrations (future)

**Goal:** Connect the process system to peripheral business functions.

**Potential integrations:**
- **Accounting:** Material costs, labor costs per step, WIP valuation
- **Planning:** Capacity, scheduling, material requirements
- **Sales:** Product availability, lead times derived from process durations
- **EHS:** Waste tracking, hazardous material flows, compliance reporting
- **Engineering:** Product design changes driving process revisions
- **Quality:** Statistical process control, inspection data analysis

Each integration is a consumer of execution data shaped by the process model. They can be added incrementally without redesigning the core.

---

## Current State (as of 2026-03-10)

All five phases are fully implemented. Phase 6 is in progress — PostgreSQL, EF Core migrations, authentication/authorization, and audit trail are complete. The system is deployable to Render.com.

Additional capability added post-Phase 6:
- **Run charts** on StepTemplateDetail for visualising measurement variability over time
- **Ad-hoc analytics** query builder with time-series charting (any numeric prompt, any time window)
- **Dashboard** with live KPI cards, job status breakdown, 30-day throughput trend, step-level performance, and recent completions
- **Out-of-range alerting** with rolling-window queries, NavMenu badge, and per-alert override tracking
- **Execution Gantt timeline** on JobDetail — SVG timeline of step executions coloured by status
- **CSV export** endpoints for step-execution history and alerts
- **AI integration** — public `/api/help/context` context document and MCP server at `/mcp` with live-data tools

### Technology Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 Web API |
| Frontend | Blazor Server (SSR + InteractiveServer render mode) |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL (production/development via Npgsql); in-memory SQLite for tests |
| Test framework | xUnit with `WebApplicationFactory` integration tests |
| Styling | Bootstrap 5 + Bootstrap Icons |
| Version control | Git / GitHub (SequencerPro/Process_Manager) |

### Test Coverage

167+ integration tests across all phases, all passing. Tests run against an in-memory SQLite database spun up per test run. Test files cover all controllers including Analytics and Alerts.

### Blazor UI Pages (22 total)

| Page | Features |
|---|---|
| KindList / KindDetail | CRUD, inline grade management, delete confirmations |
| StepTemplateList / StepTemplateDetail | CRUD, port management, run chart widget, pattern/qty rule display |
| ProcessList / ProcessDetail | CRUD, step management, cascading port dropdowns for flows, step editing, validation |
| WorkflowList / WorkflowDetail | CRUD, process/link management, link condition add/remove (grade badges), Validate button |
| JobList / JobDetail | CRUD, lifecycle transitions, step execution navigation, Gantt timeline, CSV export |
| ItemList / ItemDetail | CRUD, filtering by job/kind/status, displays JobName and BatchCode |
| BatchList / BatchDetail | CRUD, item membership management, lifecycle |
| StepExecutionList / StepExecutionDetail | Filter by status and job, port transaction creation, execution data capture, notes |
| VocabularyList | CRUD for domain vocabulary mappings |
| Dashboard | Live KPI cards, job status breakdown, 30-day throughput trend, step performance leaderboard, recent completions |
| Analytics | Ad-hoc time-series chart builder — any numeric prompt, any time window, up to 6 series |
| Alerts | Out-of-range prompt response feed with rolling window filter and CSV export |
| MyWork | Operator-focused view of in-progress step executions assigned to the current user |

### Known Limitations / Next Steps

- Multi-tenancy deferred until second SaaS tenant is onboarded (database-per-tenant approach selected — see Architecture Decision above)
- Email/webhook notifications for out-of-range alerts not yet implemented
- MCP server uses short-lived JWT tokens; a long-lived API-key auth path would improve service-account ergonomics for AI integrations
- **Phase 7 — Quality Engineering Tools** designed but not yet built: PFMEA builder (per-process risk analysis with failure modes, S/O/D ratings, action tracking) and C&E matrix builder (per-step input prioritisation via correlation scoring)
