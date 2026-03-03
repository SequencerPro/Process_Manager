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
| 1.4     | 2026-03-02 | Phase 7 implemented: PFMEA builder, C&E matrix builder, MCP tools `get_pfmea`/`list_high_rpn_failure_modes`/`get_ce_matrix` |
| 1.5     | 2026-03-02 | Project plan updated with Phase 8 design: process maturity scoring, content categorisation, guided operator execution wizard |
| 1.6     | 2026-03-02 | Project plan updated with Phase 9 additions (PFMEA staleness, change highlighting) and Phase 10 design: Root Cause Library, Ishikawa, branching 5 Whys |
| 1.7     | 2026-03-02 | Project plan updated with Phase 11 design: equipment catalog, downtime, PM scheduling, production visibility dashboard |
| 1.8     | 2026-03-03 | Phase 8a implemented: ContentCategory enum, entity fields (NominalValue, IsHardLimit, AcknowledgmentRequired), EF migration, controller + DTO updates |
| 1.9     | 2026-03-03 | Phase 8b implemented: MaturityScoringService (8 rules, 0-100 score, 4 levels), maturity endpoints, maturity badges in StepTemplateDetail/List/ProcessDetail |
| 1.10    | 2026-03-03 | Phase 8c implemented: NonConformance entity + DispositionStatus/LimitType enums, NonConformancesController, NonConformanceList page, EF migration |
| 1.11    | 2026-03-03 | Phase 8d implemented: ExecutionWizard 5-phase guided operator UI at /execute/{id}; MyWork + JobDetail updated to link to wizard |

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

### Phase 8 — Process Maturity & Guided Execution

**Goal:** Turn the process model into a *discipline tool* that enforces design completeness, and turn step execution into a guided operator experience that makes the system the authoritative work instruction — replacing paper entirely.

**Design premise:** The `StepTemplateContent` entity already supports ordered Text, Image, and Prompt blocks. Three small schema additions unlock everything: content categorisation, nominal values on numeric prompts, and hard-limit enforcement. The larger work is the completeness scoring engine and the guided operator UI that consumes these fields.

---

#### 8a — Content Categorisation + Spec Enrichment

**Schema additions to `StepTemplateContent`:**

| Field | Type | Purpose |
|---|---|---|
| `ContentCategory` | enum | `Setup`, `Safety`, `Inspection`, `Reference`, `Note` |
| `AcknowledgmentRequired` | bool | Safety blocks only — operator must explicitly tick before wizard proceeds |
| `NominalValue` | decimal? | Target value for NumericEntry prompts (e.g. 25 Nm) — paired with existing MinValue/MaxValue |
| `IsHardLimit` | bool | When true on a NumericEntry prompt, an out-of-spec entry blocks step completion and routes to non-conformance disposition |

**`ContentCategory` enum values:**
- `Setup` — what to prepare, tooling required, sequence of actions before work begins
- `Safety` — hazards, PPE requirements, stop conditions; always presented with mandatory acknowledgment gate
- `Inspection` — visual or measurement checks; typically paired with PassFail or NumericEntry prompts
- `Reference` — background information, diagrams, drawings; informational only
- `Note` — engineering notes, caveats, clarifications that don't fit other categories

**Delivers:**
- Engineers categorise each content block when authoring a step
- `Safety` blocks auto-set `AcknowledgmentRequired = true` on creation
- NumericEntry prompts gain Nominal + hard/soft limit choice
- Existing `MinValue`/`MaxValue` are formally renamed to LSL/USL in the UI (field names unchanged)
- StepTemplateDetail UI updated to show content blocks grouped by category with category badges

**Key entities modified:** `StepTemplateContent`  
**New enum:** `ContentCategory`  
**Migration:** additive-only — new nullable/defaulted columns, no breaking changes

---

#### 8b — Process Maturity Scoring

**Goal:** Make it machine-checkable whether a process design is complete enough to be used as a work instruction, without requiring human review of every content block.

**How it works:**

A `MaturityRule` is a named, configurable check that produces a pass/warn/fail result for a given `StepTemplate`. Rules are evaluated on demand (on detail page load and via a dedicated API endpoint). The system ships with a default rule set; it is not user-configurable in v1.

**Default rule set:**

| Rule | Scope | Level |
|---|---|---|
| Step has at least one Setup content block | All patterns | Warning |
| Step has at least one Safety content block | All patterns | Warning |
| Every Safety block has AcknowledgmentRequired = true | All patterns | Error |
| Inspection-pattern step has at least one PassFail or NumericEntry prompt | Pattern = Inspection | Error |
| Every NumericEntry prompt has MinValue and MaxValue | All patterns | Warning |
| Every NumericEntry prompt with IsHardLimit = true has NominalValue | All patterns | Warning |
| Step has at least one content block of any kind | All patterns | Error |
| Step has no untyped text blocks (ContentCategory is null) | All patterns | Warning (legacy data) |

**Scoring model:**
- `MaturityScore`: 0–100, computed as `(rules_passing / total_rules_applicable) × 100`
- `MaturityLevel`:
  - `Draft` (0–49) — incomplete, not suitable for production use
  - `Developing` (50–79) — has content but gaps remain
  - `Defined` (80–99) — meets minimum requirements; warnings present
  - `Optimised` (100) — fully compliant with all maturity rules
- Any Error-level rule failure caps the score at `Developing` regardless of numeric score

**Surfaces:**
- `StepTemplateDetail`: maturity badge + expandable rule results panel (pass/warn/fail per rule with remediation hint)
- `StepTemplateList`: maturity level badge column; filterable by level
- `ProcessDetail`: aggregate maturity — lowest step score drives the process-level badge
- `GET /api/steptemplates/{id}/maturity` — returns full rule evaluation as JSON
- MCP tool `get_step_maturity` — returns maturity report for a step template by code/name

**Gating behaviour:**
- A `Process` with any step at `Draft` maturity displays a warning but is not hard-blocked from activation (engineers must be able to work iteratively)
- A hard error is reserved for: attempting to assign a `Draft` step to a Job (operator execution path is blocked)

**Key entities added:** `MaturityRule` (static seed data, not user-editable in v1), `MaturityResult` (transient — computed, not persisted)  
**No migration required** — maturity is computed from existing + 8a fields

---

#### 8c — Non-Conformance Disposition

**Goal:** When a hard-limit prompt is answered out of spec, give the system a structured path to resolve it rather than leaving the operator with a blocked screen and no guidance.

**How it works:**

When a hard-limit `NumericEntry` prompt or a `PassFail` prompt (result = Fail) is encountered during operator execution, a `NonConformance` record is created and the operator is presented with a disposition choice:

| Disposition | Meaning | Effect |
|---|---|---|
| `Rework` | Return to a prior step for correction | Step execution status → Rework; job routing decision required |
| `Scrap` | Item/batch removed from the flow | Item/Batch status → Scrapped; step execution closed |
| `Quarantine` | Hold for engineering review | Item/Batch status → Quarantined; engineer notified |
| `UseAsIs` | Accept with deviation | Requires approver name + justification text; creates a formal deviation record |

**Key entities added:**
- `NonConformance` (Id, StepExecutionId, ContentBlockId, ActualValue, LimitType: LSL/USL/FailResult, DispositionStatus, DisposedBy, DisposedAt, JustificationText)
- `DispositionStatus` enum: `Pending`, `Rework`, `Scrap`, `Quarantine`, `UseAsIs`

**Surfaces:**
- Disposition modal in the guided wizard (8d) — presented immediately on hard-limit breach
- `NonConformanceList` page — all open non-conformances, filterable by status and job
- `JobDetail` — NC count badge and link to filtered NC list
- Alert integration — a `Quarantine` disposition auto-creates an alert for engineer review

---

#### 8d — Guided Operator Execution Wizard

**Goal:** A purpose-built operator UI that replaces the current StepExecutionDetail edit-form pattern with a phase-ordered wizard, surfaces all content categories appropriately, enforces acknowledgments and hard limits, and is usable on a tablet at a workstation.

**Wizard phase sequence:**

1. **Setup phase** — displays all `Setup` content blocks (text + images). Read-only. "Ready to proceed" button to advance.
2. **Safety acknowledgment phase** — one screen per `Safety` block with explicit ✓ checkbox per block. Cannot advance until all are ticked. Skipped if no safety blocks.
3. **Reference phase** — (optional) displays `Reference` blocks for the engineer to review. Skip button available.
4. **Execution phase** — prompts and `Inspection` content interleaved in `SortOrder`. For each prompt:
   - NumericEntry: large number input with live green/amber/red band showing nominal ± tolerance. Badge shows conformance state in real time.
   - PassFail: two large buttons (Pass / Fail). Fail immediately triggers disposition modal (8c).
   - Checkbox: single large tick. If `IsRequired`, must be ticked to advance.
   - MultipleChoice: radio button list.
   - Scan / TextEntry: text input.
   - Hard-limit breach on NumericEntry: disposition modal appears; step cannot proceed until disposition is recorded.
5. **Sign-off phase** — summary of all entered values with conformance indicators. Port transaction recording (items flowing in/out). Optional notes. "Complete Step" button (enabled only when all required prompts are answered and no pending NCs).

**Gating rules:**
- `IsRequired = true` prompts must be answered before sign-off
- Any unresolved `NonConformance` with status `Pending` blocks sign-off
- Safety blocks with `AcknowledgmentRequired = true` must be acknowledged in phase 2

**Design constraints:**
- Mobile/tablet-first layout: large tap targets, minimal typing
- Works in `InteractiveServer` render mode (existing Blazor pattern)
- Route: `/execute/{stepExecutionId}` — separate from the management StepExecutionDetail
- Accessible from `MyWork` page and from `JobDetail` step list

**Surfaces:**
- `ExecutionWizard.razor` — the wizard component
- `MyWork` page updated: each in-progress execution links to `/execute/{id}` instead of `/step-executions/{id}`
- `JobDetail` step rows: "Execute" action button links to wizard

---

## Architecture Decision: Guided Execution UX Rationale (2026-03-02)

### The passive exposure principle

Traditional work instructions fail in practice because they rely on operators actively seeking out a document, reading it, and applying it. Each of those steps is a friction point that degrades over time as familiarity increases — operators stop reading instructions they feel they already know, even when the instruction has changed.

The guided execution wizard is designed around a different premise: **the instruction should be present during the work, not sought before it.** When an operator is entering a torque measurement, the nominal value, tolerance band, and any setup notes are visible on the same screen. They don't need to read them — they're there. Passive exposure is more reliable than active compliance because it doesn't depend on the operator's motivation or memory.

A deeper consequence: the instruction and the compliance evidence are produced by the **same act**. Entering a torque value simultaneously:
- Displays the target and tolerance (the instruction)
- Records that the measurement was taken (the compliance evidence)
- Provides immediate conformance feedback (the quality gate)

Paper separates these into three documents — work instruction, sign-off sheet, inspection record — each requiring a separate administrative act. The wizard collapses all three into one interaction. The audit trail is a byproduct of execution, not an additional burden.

### UX design tension: friction calibration

This model only works if the wizard doesn't allow operators to skip past instructional content so easily that passive exposure is defeated. There is a real tension:

- **Too much friction** — operators resent the system, seek workarounds, management grants exceptions that undermine the model
- **Too little friction** — operators tap through without looking, defeating the passive exposure benefit

The resolution is to use **natural pacing** rather than forced acknowledgment wherever possible:

| Content type | Friction design | Rationale |
|---|---|---|
| Setup blocks | Displayed on a phase screen; single "Ready to proceed" button | Visible while operator prepares; no gate needed |
| Reference blocks | Skippable phase | Informational; forcing read would create resentment |
| Safety blocks | Explicit per-block acknowledgment checkbox | The act of ticking creates a moment of attention; also creates a defensible audit record |
| Data prompts | Cannot be skipped if `IsRequired`; hard limits cannot be overridden without disposition | Natural pacing — data entry takes time; conformance feedback is immediate |
| Sign-off phase | Requires all required prompts answered and no pending NCs | Gate is on completeness of evidence, not on reading |

The safety acknowledgment is the only place where active compliance is required — and deliberately so. It is not there to prove the operator read the block (it doesn't prove that), but because the act of acknowledging a safety condition creates a moment of deliberate attention that passive reading does not. It also creates a timestamped record linking a named user to a specific safety statement, which is defensible in an incident investigation.

### Why data prompts are the primary mechanism

The most important implication of this philosophy is that **data prompts should be co-located with the instructional content they relate to**, not grouped separately at the end. A numeric entry for torque should appear directly below the text block describing how to apply it, not on a separate "data collection" screen. This is enforced by the `SortOrder` field on `StepTemplateContent` — engineers interleave prompts and text/image blocks in the order they should be encountered, and the wizard renders them in that order within the Execution phase.

---

## Architecture Decision: Process Maturity Scoring (2026-03-02)

### Why not a user-configurable rule engine?

The temptation is to make maturity rules configurable so each organisation can define their own standards. This is rejected for v1 for three reasons:

1. **Calibration cost** — an empty rule engine is as useful as no engine. Shipping a well-chosen default set immediately delivers value; configurability can be layered on later.
2. **Consistency** — rules must mean the same thing across all step templates for the aggregate process-level score to be meaningful. User-defined rules per template would make scores incomparable.
3. **Scope creep risk** — a rule engine is a significant engineering investment. The same outcome (organisation-specific standards) is better served by making the default rules cover ISO 9001 / IATF 16949 minimum requirements, which most manufacturing organisations already claim to follow.

### Gating philosophy: warn, don't block

The system should be a coach, not a bureaucrat. Hard errors are reserved for situations where the data is genuinely unusable (e.g. an Inspection step with no prompts produces no evidence). Everything else produces a warning and a clear remediation path. This means engineers can build iteratively without fighting the system, while the maturity score provides an honest view of where the process library stands.

### Relationship to existing validation

Process-level `Validate` (Phase 3) checks structural integrity: are ports compatible, are flows complete? Maturity scoring checks *content* completeness: do operators have sufficient guidance to execute safely? They are complementary and both surface as badges on `ProcessDetail`.

---

## Architecture Decision: Process Change Control (2026-03-02)

### Why change control is required, not optional

In a conventional system, process designs and work instructions are separate artefacts. A process might change in the system without the work instruction being updated, and vice versa. The gap between them is managed by people.

This system eliminates that gap by design — the process design *is* the work instruction. That means the obligations that apply to document revision control apply directly to process edits:

- Operators must always be working against an authorised version
- Changes must be reviewed before they reach the shop floor
- There must be a permanent record of who authorised each version and when
- Jobs in progress must not be disrupted by mid-run changes to the process they are executing

Without formal change control, the system's claim to replace work instructions is incomplete. A process that any Engineer can edit at any moment, with changes taking effect immediately for all in-progress Jobs, is less controlled than a paper instruction with a sign-off sheet.

### Why immutability on Release is the right model

The alternative — allowing edits to Released processes with version tracking but no freeze — creates ambiguity about what version any given execution was performed against. An immutable Released version, combined with job-level version pinning, means the execution record is unambiguous: the Job was started against version 3, version 3 is preserved, the full content of that version is recoverable years later.

This is the same model used by:
- Engineering drawing control (revision letters, release stamps)
- ISO 9001 document control requirements
- The PFMEA branching feature already built in Phase 7 (branch = create new Draft from Released version)

### Relationship to Phase 8 (maturity scoring)

Maturity scoring (Phase 8b) and change control (Phase 9) are complementary gates at different points in the process lifecycle:

- **Maturity scoring** gates the *content quality* of a Draft — is it complete enough to submit for approval?
- **Change control** gates the *authorisation* of a Released version — has an appropriate person reviewed and approved it?

Maturity scoring is a prerequisite for submission; approval is the final gate before release. Together they form a two-stage quality control on the process design itself, before any operator ever sees it.

---

### Phase 9 — Process Change Control & Approval

**Goal:** Give process designs a formal lifecycle so that changes go through the same change control obligations as document revisions — because once the process design *is* the work instruction, changing it unilaterally carries the same risk as issuing an unauthorised document revision.

**The core problem:** The `Process`, `StepTemplate`, and `Workflow` entities already carry a `Version` integer, but version increments are currently uncontrolled — any Engineer can edit any field at any time and the version ticks up. There is no approval gate, no release state, and no protection for Jobs that are already in execution against a version that has since changed.

---

#### Design intent

**Process lifecycle states:**

| State | Meaning |
|---|---|
| `Draft` | Being authored or revised; not available for new Jobs; maturity score may be incomplete |
| `PendingApproval` | Submitted for review; locked against further edits; cannot be used for new Jobs |
| `Released` | Approved and active; available for new Jobs; cannot be edited directly — a new Draft revision must be created |
| `Superseded` | Replaced by a newer Released version; Jobs already in execution continue against this version to completion |
| `Retired` | Withdrawn from use; no new Jobs; existing Jobs must be reviewed |

**Key design decisions:**

- **Immutability on Release.** Once a Process is Released, its steps, flows, ports, and content blocks are frozen. Edits require creating a new Draft revision (increment version, copy structure, status = Draft). This is the same model used by document management systems and the PFMEA branching feature already built in Phase 7.
- **Jobs pin to a version.** A Job records the `ProcessVersion` it was started against. If the Process is superseded mid-run, the Job continues against its pinned version. Operators executing a step see a banner if they are working against a superseded version, but are not blocked — the work they started was authorised against that version.
- **Approval roles.** Engineers (existing role) can create and edit Drafts and submit for approval. A new `Approver` role (or promoted Admin function) has the ability to Release or reject. Rejection returns the process to Draft with a required rejection reason.
- **Approval record.** Each Release creates an `ApprovalRecord` (ProcessId, Version, SubmittedBy, SubmittedAt, ApprovedBy, ApprovedAt, ApprovalNotes) — the permanent audit trail linking a named person to each released version.
- **Maturity gate.** A Draft with any Error-level maturity rule failure cannot be submitted for approval. Warnings are permitted but displayed on the approval review screen.

**Key entities added:**
- `ProcessStatus` enum: `Draft`, `PendingApproval`, `Released`, `Superseded`, `Retired`
- `ApprovalRecord` (Id, ProcessId, Version, SubmittedBy, SubmittedAt, ReviewedBy, ReviewedAt, Decision: Approved/Rejected, Notes)
- `ProcessRevision` — a snapshot of the process structure at each Release (enables operators to view the exact version they were working against, even if the process has since changed significantly)

**Surfaces:**
- ProcessList: status badge per process (Draft/Released/etc.), version number
- ProcessDetail: Submit for Approval / Approve / Reject / Create New Revision buttons (role-dependent)
- Approval queue page — Approver-role view of all processes pending review
- JobDetail: version indicator + superseded banner if applicable
- MCP tool `get_process_approval_status` — lists processes pending approval or recently released

**Applies to StepTemplates too.** The same lifecycle applies to StepTemplates, since they are the building blocks of processes. A Released StepTemplate cannot be edited; changes require a new Draft version. A Process can only be Released if all its StepTemplates are Released.

#### PFMEA staleness tracking

A PFMEA records the `ProcessVersion` it was authored against. The linkage between change control and quality engineering tools is enforced as follows:

- When a new Process version is Released, all PFMEAs linked to the previous version are automatically marked `IsStale = true`
- A stale PFMEA is prominently flagged on the PFMEA list and detail pages with a "Process has changed — review required" banner
- The engineer reviews the PFMEA and either: (a) branches it to create a new PFMEA version against the new process version, or (b) marks it as "Reviewed — no changes required" with a note, which clears the staleness flag and records who reviewed it and when
- A Process in `PendingApproval` state displays a warning if it has any linked PFMEAs in stale or unreviewed state — the approver can see the quality tool coverage gap during review
- This creates a closed loop: every time the process design changes, the risk analysis must be revisited

**Fields added to `Pfmea`:** `ProcessVersion int` (the released version this PFMEA was written against), `IsStale bool`, `StalenessClearedBy string?`, `StalenessClearedAt DateTime?`, `StalenessClearanceNotes string?`

#### Change highlighting in the execution wizard

When a Job is executing against a process version and the operator opens the wizard, the system can identify content blocks that are **new or modified** relative to the previous released version. These blocks are highlighted with a "Updated" badge in the wizard UI, drawing the operator's attention to what has changed without requiring them to compare versions manually.

This requires:
- Each `StepTemplateContent` block carries a `IntroducedInVersion int` (the process version in which this block was first added or last substantively modified)
- The wizard compares `IntroducedInVersion` against the previous released version number — any block where `IntroducedInVersion == currentVersion` is flagged
- Setup and Safety blocks that are flagged as changed are additionally promoted to the top of their respective phases so the operator encounters them first
- The flag is purely informational for Setup/Reference/Note blocks; for Safety blocks, a changed block always requires re-acknowledgment even if the operator has acknowledged it in a prior execution

---

### Phase 10 — Root Cause Analysis

**Goal:** Give engineers structured tools to analyse the causes of non-conformances, failures, and process problems, and build an institutional library of causes and corrective actions so that learning accumulates over time rather than being lost when personnel change.

**Design premise:** Root cause analysis tools are most useful when they are connected to the things that triggered the analysis — a non-conformance record (Phase 8c), a PFMEA failure mode (Phase 7), or a manually initiated investigation. The library is what distinguishes this from a standalone diagramming tool: causes identified in one analysis can be retrieved and reused in future analyses, and usage frequency reveals which root causes recur across the organisation.

---

#### 10a — Root Cause Library

The foundation for both tools. A shared, searchable catalogue of named causes that engineers build over time.

**Key entity: `RootCauseEntry`**

| Field | Type | Purpose |
|---|---|---|
| `Title` | string | Short cause name (e.g. "Fixture wear", "Operator training gap", "Incoming material variation") |
| `Description` | string? | Detail on how this cause manifests and how to detect it |
| `Category` | enum | `Machine`, `Method`, `Material`, `People`, `Measurement`, `Environment`, `Management` (the standard 7M taxonomy) |
| `Tags` | string? | Free-form comma-separated tags for cross-cutting retrieval |
| `CorrectiveActionTemplate` | string? | Suggested corrective action text — pre-populated into analyses that use this entry |
| `UsageCount` | int (computed) | Number of analyses that reference this entry — surfaces the most-encountered causes |

**Surfaces:**
- Root Cause Library page — searchable/filterable catalogue; create, edit, merge duplicate entries
- Typeahead search in both analysis tools — as an engineer types a cause, matching library entries are suggested
- Library entry detail shows all analyses that reference it — makes the pattern of recurrence visible

---

#### 10b — Ishikawa (Fishbone) Diagram

A structured cause enumeration tool organised by category. Best used when the space of possible causes is wide and the team wants to ensure no category is overlooked.

**Structure:** One diagram per investigation. Causes are grouped into category "bones" (Machine, Method, Material, People, Measurement, Environment). Each cause can have sub-causes (one level of nesting — deeper nesting produces diagrams too complex to act on).

**Key entities:**
- `IshikawaDiagram` (Id, Title, ProblemStatement, LinkedEntityType: NonConformance/PfmeaFailureMode/Manual, LinkedEntityId?, CreatedBy, Status: Open/Closed, ClosedAt, ClosureNotes)
- `IshikawaCause` (Id, DiagramId, Category, CauseText, ParentCauseId?, RootCauseLibraryEntryId?, IsSelectedRootCause bool)

**`IsSelectedRootCause`** marks which causes the team concluded were the actual root causes (as opposed to contributing or hypothesised causes). These selections drive the corrective action record and are the entries that enrich the library.

**UI:** Rendered as the standard fishbone layout — problem statement on the right, category spines branching left, causes hanging off each spine. Blazor SVG rendering, same approach as the existing Gantt timeline. Engineers add causes by clicking a category spine; typeahead suggests library entries.

---

#### 10c — Branching 5 Whys

An iterative depth-first cause analysis. Better than Ishikawa when the causal chain is relatively well understood and the team wants to reach an actionable root cause quickly. Branching is critical — the reason a standard linear 5 Whys fails in practice is that a single "why" often has multiple independent contributing causes, and ignoring all but one produces an incomplete corrective action.

**Structure:** A tree where each node is a "why" statement. The root node is the problem statement. Each node can have one or more child nodes (each child is an independent answer to "why did this happen?"). Leaf nodes are root causes. There is no fixed depth — "5 Whys" is a heuristic, not a rule; some chains reach root cause in 3, some require 7.

**Key entities:**
- `FiveWhysAnalysis` (Id, Title, ProblemStatement, LinkedEntityType, LinkedEntityId?, CreatedBy, Status: Open/Closed, ClosedAt, ClosureNotes)
- `FiveWhysNode` (Id, AnalysisId, ParentNodeId?, WhyStatement, IsRootCause bool, RootCauseLibraryEntryId?, CorrectiveAction string?)

**`IsRootCause`** is set by the engineer to mark where drilling down further would reach outside the scope of the organisation's control (e.g. "supplier material variation" may be a root cause if the organisation cannot control the upstream process). Leaf nodes without `IsRootCause = true` represent incomplete analysis — surfaced as a warning.

**UI:** Rendered as a horizontal tree expanding left-to-right: problem on the left, root causes on the right. Each node shows its why statement; engineers add child nodes inline. Library typeahead on each node. Corrective action field appears when `IsRootCause` is checked.

---

#### Integration points across the system

| Trigger | Analysis type | How linked |
|---|---|---|
| Non-conformance (Phase 8c) | Either | NonConformanceDetail shows "Start RCA" button; creates analysis with `LinkedEntityType = NonConformance` |
| PFMEA failure mode (Phase 7) | Either | PfmeaDetail failure mode row shows "Investigate" button; useful for proactive analysis before a failure occurs in production |
| Manual | Either | Engineers can initiate an analysis without a linked entity for ad-hoc investigations |

All analyses contribute to the Root Cause Library. The library's `UsageCount` and cross-referencing to analyses makes recurring causes visible — a cause that appears in 15 analyses is a systemic problem, not an isolated incident.

**MCP tools:** `list_recurring_root_causes` (top causes by usage count across all analyses), `get_rca_summary` (open analyses linked to a given non-conformance or PFMEA).

---

## Architecture Decision: Root Cause Analysis Library (2026-03-02)

### Why a shared library rather than free-text causes

Without a library, root cause analysis tools produce unstructured text that cannot be aggregated. "Operator error", "operator training issue", "insufficient operator training", and "employee not trained" are the same cause described four different ways — a system with no normalisation cannot tell they are related. The library provides the normalisation layer: engineers are encouraged (but not forced) to link causes to library entries, and over time the library becomes the organisation's vocabulary for talking about failure.

The library is intentionally not a locked taxonomy. Engineers can create new entries freely. The value comes from the usage data, not from enforcement — if "Operator training gap" appears in 23 analyses and "Insufficient training" appears in 2, management can see that these are likely the same systemic issue and merge the entries.

### Branching vs. linear 5 Whys

The standard 5 Whys technique as originally described is linear: one problem, one chain of whys, one root cause. This works for simple mechanical failures but fails for process problems, which typically have multiple independent contributing causes. A machine that produces out-of-spec parts may do so because of worn tooling *and* inconsistent clamping force *and* a measurement system that doesn't detect it until late. A linear 5 Whys would identify one of these and miss the others.

Branching makes the tree model explicit: each "why" node can have multiple children, each representing an independent cause. All branches must be resolved to root causes for the analysis to be considered complete. This produces a more complete corrective action set and prevents the common failure mode of fixing the most obvious cause while leaving others in place.

### Relationship to PFMEA

PFMEA (Phase 7) is *prospective* risk analysis — identifying failure modes before they occur and rating their severity. Root cause analysis is *retrospective* — investigating failures that have occurred. They are complementary:

- A recurring root cause identified through RCA should prompt a review of the relevant PFMEA to check whether that cause was anticipated and whether the current controls are adequate
- A high-RPN PFMEA failure mode with no corrective actions is a candidate for proactive RCA
- The RCA library and the PFMEA failure cause text share vocabulary — over time they should converge toward the same terminology

This connection is informational in the first implementation (engineers navigate between them manually) but can be formalised in a later iteration.

---

### Phase 11 — Production Management

**Goal:** Give production managers and planners a clear, honest view of what is happening on the shop floor — where every job is in its routing, which are on time and which are late, and what the state of equipment is. Provide a full equipment lifecycle management capability: availability modeling, downtime tracking, and preventive maintenance scheduling.

**Scope boundary:** This phase does **not** attempt finite capacity scheduling (computing an optimal sequence of jobs across constrained resources). That is a hard algorithmic problem with high implementation risk and significant configuration burden. Instead, the system provides the data and visibility that enables planners to make good scheduling decisions manually. The discipline is on the planners; the system's job is to make the current state and its implications impossible to ignore.

---

#### 11a — Expected Durations + Job Due Dates

Two small additions to existing entities that unlock all downstream visibility:

**`StepTemplate`** gains:
- `ExpectedDurationMinutes int?` — how long this step is expected to take under normal conditions. Used to estimate job completion dates and to flag slow executions.

**`Job`** gains:
- `DueDate DateTime?` — the committed delivery date for this job
- `PlannedStartDate DateTime?` — when the job is expected to begin

**Computed fields (not stored, derived on query):**
- `ExpectedCompletionDate` = `PlannedStartDate` + Σ(`ExpectedDurationMinutes`) across all remaining steps
- `IsLate` = `ExpectedCompletionDate` > `DueDate`
- `DaysLate` = max(0, (`ExpectedCompletionDate` - `DueDate`).Days)
- `ActualCycleTime` per step = `StepExecution.CompletedAt` - `StepExecution.StartedAt`
- `CycleTimeVariance` = `ActualCycleTime` - `ExpectedDurationMinutes`

These two field additions are a migration-only change, but they unlock the entire visibility layer.

---

#### 11b — Equipment Catalog

A registry of every machine, workstation, tool, or facility resource that steps are performed on.

**Key entity: `Equipment`**

| Field | Type | Purpose |
|---|---|---|
| `Code` | string | Short identifier (e.g. "CNC-01", "CMM-3") |
| `Name` | string | Human-readable name |
| `CategoryId` | FK → EquipmentCategory | Type of equipment |
| `Location` | string? | Physical location or cell |
| `Manufacturer` | string? | OEM name |
| `Model` | string? | Model number |
| `SerialNumber` | string? | For maintenance records and traceability |
| `InstallDate` | DateTime? | Used to drive age-based PM triggers |
| `IsActive` | bool | Whether available for assignment |

**`EquipmentCategory`** (e.g. CNC Lathe, CMM, Assembly Station, Oven, Press) — user-defined categories with a short code and name.

**Step template linkage:** `StepTemplate` gains an optional `RequiredEquipmentCategoryId` — declaring that this step must be performed on a machine of this category. This is the connection between process design and resource planning: the process model declares what type of equipment a step needs; the equipment catalog tracks which specific machines of that type exist.

**Step execution linkage:** `StepExecution` gains an optional `EquipmentId` — recording which specific machine this execution ran on. This is set by the operator in the execution wizard (11d of Phase 8 can surface the assignment prompt). Without this, equipment downtime is unconnected to production impact.

---

#### 11c — Downtime Tracking

A record of every period during which a piece of equipment was unavailable, whether planned or unplanned.

**Key entity: `DowntimeRecord`**

| Field | Type | Purpose |
|---|---|---|
| `EquipmentId` | FK | The affected machine |
| `Type` | enum | `Planned` (scheduled maintenance, changeover, shift gap) or `Unplanned` (breakdown, fault) |
| `StartedAt` | DateTime | When the equipment went down |
| `EndedAt` | DateTime? | Null if currently down |
| `Reason` | string | Description of why it went down |
| `ResolvedBy` | string? | Person who brought it back up |
| `LinkedMaintenanceTaskId` | FK? | If this downtime was caused by or resolved by a maintenance task |

**Derived metrics (computed from DowntimeRecords):**
- `Availability %` = (scheduled time − downtime) / scheduled time per equipment per period
- `MTBF` (Mean Time Between Failures) = average time between unplanned downtime starts
- `MTTR` (Mean Time To Repair) = average duration of unplanned downtime events

**Production impact linkage:** When a `DowntimeRecord` is open (no `EndedAt`), any StepExecution whose `EquipmentId` matches and whose status is pending or in-progress is flagged as *equipment-blocked* on the visibility dashboard.

---

#### 11d — Preventive Maintenance Scheduling

Time-based and usage-based maintenance triggers that automatically generate maintenance tasks before failures occur.

**Key entity: `MaintenanceTrigger`**

| Field | Type | Purpose |
|---|---|---|
| `EquipmentId` | FK | The machine this trigger watches |
| `Title` | string | Name of the maintenance task it generates (e.g. "Annual calibration", "Lubricate spindle") |
| `TriggerType` | enum | `TimeBased` or `UsageBased` |
| `IntervalDays` | int? | For TimeBased: days between tasks |
| `IntervalUsageCycles` | int? | For UsageBased: number of step executions on this equipment between tasks |
| `LastTriggeredAt` | DateTime? | When the most recent task was generated |
| `NextDueAt` | DateTime? | Computed: LastTriggeredAt + IntervalDays, or derived from usage count |
| `AdvanceNoticeDays` | int | How many days before due to surface the upcoming task |

**Key entity: `MaintenanceTask`**

| Field | Type | Purpose |
|---|---|---|
| `EquipmentId` | FK | The machine to be maintained |
| `TriggerId` | FK? | Null for ad-hoc tasks |
| `Title` | string | What to do |
| `Type` | enum | `PreventiveMaintenance`, `CorrectiveMaintenance`, `Inspection`, `Calibration` |
| `Status` | enum | `Upcoming`, `Due`, `Overdue`, `InProgress`, `Completed`, `Cancelled` |
| `DueDate` | DateTime | When the task must be completed |
| `AssignedTo` | string? | Person or team responsible |
| `CompletedAt` | DateTime? | Actual completion |
| `CompletedBy` | string? | Who completed it |
| `Notes` | string? | Completion notes, findings, parts used |
| `LinkedDowntimeRecordId` | FK? | If this task created or resolved a downtime record |

**PM generation:** A background process (or on-demand trigger) scans `MaintenanceTrigger` records and creates `MaintenanceTask` records when `NextDueAt` is within `AdvanceNoticeDays`. Tasks auto-transition to `Due` and `Overdue` based on `DueDate`.

**Calibration integration:** Calibration tasks (Type = Calibration) record the calibration result and due date. Equipment with overdue calibration is flagged on the equipment card and on any StepExecution that used that equipment while it was out of calibration — important for traceability in regulated environments.

---

#### 11e — Production Visibility Dashboard

A purpose-built view for production managers and planners. Not a replacement for the existing analytics dashboard — this one is operationally focused on the current state rather than historical trends.

**WIP Board:** All active Jobs grouped by their current step, showing:
- Job code, due date, days late / days remaining
- Current step name and how long the current step execution has been running vs. expected
- Equipment assigned (if any) and whether it is currently in a downtime event
- Colour coding: green (on time), amber (at risk — expected completion within 2 days of due date), red (late)

**Equipment Status Panel:** All active Equipment with current status:
- Available / In Use (with job/step) / Planned Downtime / Unplanned Downtime
- Upcoming maintenance tasks within the next N days (configurable)
- Availability % for the current month

**Bottleneck Flags:** Steps where the queue (number of pending step executions) is disproportionately long relative to expected duration — a simple WIP / expected throughput ratio. Surfaced as a ranked list, not as a scheduling algorithm output.

**Late Jobs List:** All jobs where `IsLate = true`, sorted by `DaysLate` descending, with a one-click path to the job detail.

**Maintenance Due List:** All `MaintenanceTask` records in `Due` or `Overdue` status, with equipment name and assigned person.

**Surfaces:**
- `ProductionDashboard.razor` — the main visibility page (route `/production`)
- NavMenu entry under a new "Production" section
- `EquipmentList.razor` / `EquipmentDetail.razor` — catalog with full history
- `MaintenanceTaskList.razor` — all tasks across all equipment, filterable by status/type/equipment
- `GET /api/equipment` — paginated equipment catalog
- `GET /api/equipment/{id}/downtime` — downtime history
- `GET /api/equipment/{id}/maintenance` — task list
- `GET /api/production/wip` — current WIP state for the dashboard
- `GET /api/production/bottlenecks` — ranked bottleneck step list
- MCP tools: `get_production_status` (WIP summary), `list_equipment_downtime` (current and recent), `list_overdue_maintenance`

---

## Architecture Decision: Production Visibility Over Scheduling (2026-03-02)

### Why not finite capacity scheduling

Finite capacity scheduling (FCS) — computing an optimal sequence and timing of all jobs across constrained resources — is one of the hardest problems in manufacturing software. It requires:
- Complete and accurate expected durations for every step
- Complete and accurate resource availability models for every machine and operator
- A defined objective function (minimise lateness? maximise throughput? minimise changeover?)
- An algorithm that can run fast enough to be re-run when the plan changes (which it will, constantly)
- Cultural buy-in from planners, who often distrust system-generated schedules

Every major ERP vendor has a scheduling module. Very few manufacturers actually use it, because the data quality requirements are never fully met in practice and the schedule becomes stale the moment a machine breaks down or a job is expedited.

### The visibility-first approach

The approach taken here is different: the system is honest about what it knows and doesn't try to infer what it doesn't. It knows:
- The process routing for every job (the process design already defines this)
- The current execution state of every step
- Expected durations (once 11a is built)
- Due dates (once 11a is built)
- Equipment status (once 11b–11d are built)

From these it can compute lateness, flag risks, and identify bottlenecks — without claiming to have solved the scheduling problem. The planner uses this information to make decisions. This is more honest, more robust, and more useful than a schedule that is optimistic about equipment availability and operator performance.

### The path to light scheduling

If planners consistently want to answer "can we commit to this due date given our current load?", the system can offer a simple capacity check without full FCS:
- Sum the expected durations of all pending steps for all active jobs
- Compare to available equipment-hours per category
- Flag if the category is over-subscribed in a given time window

This is a demand/capacity comparison, not a schedule. It tells the planner there is a problem without telling them exactly how to solve it. This can be added incrementally after 11e without redesigning anything.

### Equipment assignment as a learning flywheel

Recording `EquipmentId` on every `StepExecution` has value beyond production visibility. Over time it produces:
- Actual cycle time distribution per step per machine (some machines are faster than others for the same step)
- Correlation between equipment age/maintenance state and process capability (SPC data linked to machine)
- Traceability: which specific machine processed a given item, relevant for recall or quality investigation

None of this requires any additional design — it is a natural output of recording the assignment.

---

### Phase 12+ — Integrations (future)

**Goal:** Connect the process system to peripheral business functions.

**Potential integrations:**
- **Accounting:** Material costs, labor costs per step, WIP valuation
- **Planning:** Capacity load checking (Phase 11 visibility-first approach; finite scheduling explicitly deferred)
- **Sales:** Product availability, lead times derived from process expected durations
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
- **Phase 7 quality engineering tools** — PFMEA builder (per-process failure mode analysis with S/O/D RPN scoring, action tracking, branching) and C&E matrix builder (per-step input prioritisation via 0/1/3/9 correlation scoring, interactive grid, CSV export); MCP tools `get_pfmea`, `list_high_rpn_failure_modes`, `get_ce_matrix`

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

185 integration tests across all phases, all passing. Tests run against an in-memory SQLite database spun up per test run. Test files cover all controllers including Analytics and Alerts.

### Blazor UI Pages (26 total)

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
| PfmeaList / PfmeaDetail | PFMEA repository; per-process failure mode management; S/O/D/RPN badges with heat-map colouring; action tracking with before/after risk; Branch button for versioning |
| CeMatrixList / CeMatrixDetail | C&E matrix repository; interactive correlation grid; click-to-cycle scores; live priority scores; inline importance editing; CSV export |

### Known Limitations / Next Steps

- **Phase 8 — Process Maturity & Guided Execution** ✅ built: ContentCategory enum, NominalValue/IsHardLimit/AcknowledgmentRequired fields, MaturityScoringService (8 rules), maturity badges across list/detail views, NonConformance entity + disposition workflow, 5-phase ExecutionWizard at `/execute/{id}`
- **Phase 9 — Process Change Control & Approval** designed, not yet built: formal Draft/PendingApproval/Released/Superseded lifecycle; PFMEA staleness tracking; change highlighting in execution wizard; approval records; job-level version pinning
- **Phase 10 — Root Cause Analysis** designed, not yet built: Root Cause Library, Ishikawa fishbone diagrams, branching 5 Whys, linkage to non-conformances and PFMEA failure modes
- **Phase 11 — Production Management** designed, not yet built: expected durations + job due dates, equipment catalog, downtime tracking, PM scheduling, production visibility dashboard (WIP board, late jobs, bottleneck flags, equipment status)
- Multi-tenancy deferred until second SaaS tenant is onboarded (database-per-tenant approach selected — see Architecture Decision above)
- Email/webhook notifications for out-of-range alerts not yet implemented
- MCP server uses short-lived JWT tokens; a long-lived API-key auth path would improve service-account ergonomics for AI integrations
