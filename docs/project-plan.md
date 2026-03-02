# Process Manager — Project Plan

## Version History

| Version | Date       | Notes                          |
|---------|------------|--------------------------------|
| 0.1     | 2026-02-16 | Initial draft                  |
| 0.2     | 2026-02-17 | All phases 1-5 implemented, cross-cutting improvements added |
| 0.3     | 2026-02-21 | API fixes (IsActive toggle, Workflow versioning), Blazor detail pages for Items/Batches/StepExecutions |
| 0.4     | 2026-02-21 | CRUD modals on all detail pages, 17 Blazor pages complete |
| 0.5     | 2026-02-22 | Full UI polish: cascading port dropdowns, workflow validation UI, port transaction forms, delete confirmations on all pages, empty-state messages, display name fixes (JobName/BatchCode), StepExecution job filter, WorkflowDetail link condition management |

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

### Phase 6 — Production Infrastructure (planned)

**Goal:** Make the system deployable in real multi-user environments.

**Planned work:**
- **Database provider abstraction:** Replace the current SQLite-only setup with a configurable provider selected at deployment time. Target providers: PostgreSQL (preferred for open-source deployments), SQL Server (enterprise/Windows environments), with Oracle and others as secondary targets. The EF Core provider is read from `appsettings.json` (`"DatabaseProvider": "PostgreSQL"`) and branched in `Program.cs`. SQLite remains available for development and single-user embedded use.
- **EF Core Migrations:** Replace `Database.EnsureCreated()` with a proper migrations pipeline so schema changes can be applied to live databases without data loss.
- **Authentication & Authorization:** User identity, role-based access (e.g., operator vs. engineer vs. admin), and audit trails tied to specific users.
- **Multi-tenancy:** Namespace isolation so multiple factories or business units can share a single deployment.

### Phase 7+ — Integrations (future)

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

## Current State (as of 2026-02-22)

All five phases are fully implemented. The system is working end-to-end in a single-developer local environment.

### Technology Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 Web API |
| Frontend | Blazor Server (SSR + InteractiveServer render mode) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (development) |
| Test framework | xUnit with `WebApplicationFactory` integration tests |
| Styling | Bootstrap 5 + Bootstrap Icons |
| Version control | Git / GitHub (SequencerPro/Process_Manager) |

### Test Coverage

164 integration tests across all phases, all passing. Tests run against an in-memory SQLite database spun up per test run.

### Blazor UI Pages (17 total)

| Page | Features |
|---|---|
| KindList / KindDetail | CRUD, inline grade management, delete confirmations |
| StepTemplateList / StepTemplateDetail | CRUD, port management, pattern/qty rule display |
| ProcessList / ProcessDetail | CRUD, step management, cascading port dropdowns for flows, step editing, validation |
| WorkflowList / WorkflowDetail | CRUD, process/link management, link condition add/remove (grade badges), Validate button |
| JobList / JobDetail | CRUD, lifecycle transitions, step execution navigation |
| ItemList / ItemDetail | CRUD, filtering by job/kind/status, displays JobName and BatchCode |
| BatchList / BatchDetail | CRUD, item membership management, lifecycle |
| StepExecutionList / StepExecutionDetail | Filter by status and job, port transaction creation, execution data capture, notes |
| VocabularyList | CRUD for domain vocabulary mappings |

### Known Limitations / Next Steps

- SQLite is used for all environments; not suitable for multi-user production (see Phase 6)
- No authentication or authorization
- No EF Core migrations (uses `EnsureCreated` — fine for dev, not for live schema updates)
- Port quantity rule fields (QtyRuleN/Min/Max) not yet exposed in UI forms
- No toast/notification system — errors shown as inline alerts
