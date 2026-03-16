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
| 1.12    | 2026-03-03 | Phase 9 API layer: ProcessStatus enum (Draft/PendingApproval/Released/Superseded/Retired), ApprovalRecord entity, PFMEA staleness fields, Job version pinning, lifecycle endpoints on ProcessesController + StepTemplatesController, ApprovalsController |
| 1.13    | 2026-03-03 | Phase 9 EF migration Phase9_ChangeControl: Status columns (default Released), IntroducedInVersion, ProcessVersion, ApprovalRecords table |
| 1.14    | 2026-03-03 | Phase 9 Blazor UI: ProcessList/Detail + StepTemplateList/Detail status badges, lifecycle buttons (Submit/Approve/Reject/NewRevision/Retire) with modals; ApprovalQueue page at /approval-queue; JobDetail superseded process banner; NavMenu Approval Queue link with pending badge |
| 1.15    | 2026-03-02 | Admin: Display Name field added to Add User form; Edit User modal (Display Name + Role) on UserList; PATCH api/auth/users/{id} admin endpoint |
| 1.16    | 2026-03-08 | Phase 12 design: workflow execution with OrgUnit assignment, automated process sequencing, and periodic scheduling via WorkflowSchedule |
| 1.17    | 2026-03-08 | Phase 2 design gap addressed: PromptDefinition and PromptOption entities added to data model; ExecutionData updated with prompt_definition_id FK, widened value field (text), and extended DataType enum |
| 1.18    | 2026-03-08 | Phase 13 plan: pre-populated process content library with seeded StepTemplates, Processes, and Workflows for common manufacturing functions |
| 1.19    | 2026-03-08 | Phase 12f plan: Participant Portal — execution-only UI surface with Participant role; hides all design, admin, and quality engineering tools |
| 2.0     | 2026-03-15 | Scope narrowed to manufacturing only; Phase 7c Control Plan builder added to quality engineering tools |
| 2.1     | 2026-03-15 | Phase 7c implemented: ControlPlan + ControlPlanEntry entities, CharacteristicType enum, ControlPlansController (CRUD + entries + CSV export + staleness), EF migration Phase7c_ControlPlan, ControlPlanList/Detail Blazor pages, ApiClient methods, staleness integration with ProcessesController.Approve, MCP tools get_control_plan/list_critical_characteristics, integration tests |
| 2.2     | 2026-03-15 | Phase 14 design: Document Control & QMS — ProcessRole enum, DocumentApprovalRequest entity, ParallelGroup on StepExecution, revision metadata on Process (RevisionCode, ChangeDescription, EffectiveDate, ParentProcessId, ApprovalProcessId), approval-as-process architecture, seeded Standard Document Approval routing |
| 2.3     | 2026-03-15 | Phase 10 expanded to include Material Review Board (Phase 10d): MrbReview entity, MrbParticipant entity, escalation from NonConformance Quarantine, SCAR flag, RCA linkage requirement; Phase 15 added: Tiered Accountability & Action Tracking — unified ActionItem entity, tiered views (Operator/Engineer/Manager/Executive), Management Review support (ISO 9001 clause 9.3); Phase 15+ Integrations renumbered to Phase 16+ |
| 2.4     | 2026-03-15 | UserPicker prompt type added to Phase 2 design (DataType enum extension — FK-backed user selection for instructor/witness/signatory capture); Phase 16 added: Training & Competency Management — ProcessRole Training value, CompetencyRecord entity, ProcessTrainingRequirement entity, expiry and re-training scheduling, competency matrix view, integration with Phase 15 tiered views; Phase 16+ Integrations renumbered to Phase 17+ |
| 2.5     | 2026-03-15 | Phase 2 enhancement implemented: `LongText` and `UserPicker` added to `PromptType` enum; `GET api/auth/users/picker` endpoint (all authenticated roles); `UserPickerDto`; `GetUserPickerListAsync` in ApiClient; StepTemplateDetail prompt type selector updated; ExecutionWizard renders LongText (textarea) and UserPicker (user dropdown, stores display name); StepExecutionDetail updated for both new types |
| 2.6     | 2026-03-15 | Phase 14 implemented: `ProcessRole` enum (5 values), `DocumentApprovalStatus` enum, `DocumentApprovalRequest` entity, `Phase14_DocumentControl` EF migration; `Phase14Dtos` (DocumentApprovalRequestDto, DocumentSubmitForApprovalDto, AdminReleaseDocumentDto); `ProcessResponseDto`/`ProcessSummaryResponseDto`/`JobResponseDto`/`StepExecutionResponseDto` extended with Phase 14 fields; `DocumentApprovalsController` (submit/withdraw/CRUD); `ProcessesController` processRole filter + admin-release endpoint; `StepExecutionsController` approval-completion hook (approve → Released, reject → Draft); `JobsController` MapToDto updated; ApiClient document-approval methods; `DocumentList.razor` (submit-for-approval + admin-release modals); NavMenu Document Library link; MCP `list_qms_documents` tool; MCP server version 1.5 |
| 2.7     | 2026-03-15 | Phase 10a implemented: `RootCauseCategory` enum (7M: Machine/Method/Material/People/Measurement/Environment/Management), `RootCauseEntry` entity (Title, Description, Category, Tags, CorrectiveActionTemplate, UsageCount), `Phase10a_RootCauseLibrary` EF migration; `Phase10aDtos` (RootCauseEntryResponseDto/CreateDto/UpdateDto); `RootCauseEntriesController` (CRUD + `/search` typeahead endpoint); ApiClient root-cause methods; `RootCauseLibraryList.razor` with category filter/search/create/edit/delete modals; NavMenu Root Cause Library link; MCP `list_recurring_root_causes` tool (top N by UsageCount, category filter); MCP server version 1.6 |
| 2.8     | 2026-03-15 | Phase 10b+10c implemented: `RcaStatus` enum (Open/Closed), `RcaLinkedEntityType` enum (Manual/NonConformance/PfmeaFailureMode), `IshikawaDiagram`/`IshikawaCause`/`FiveWhysAnalysis`/`FiveWhysNode` entities, `Phase10bc_RcaAnalysis` EF migration; `Phase10bcDtos` (full Ishikawa + FiveWhys DTO set, recursive `IshikawaCauseSummaryDto.SubCauses` and `FiveWhysNodeDto.ChildNodes`); `IshikawaController` (CRUD, causes CRUD, Open/Close/Reopen, one-level-nesting enforcement, UsageCount increment on IsSelectedRootCause); `FiveWhysController` (CRUD, nodes CRUD with recursive delete, Open/Close/Reopen, `HasIncompleteLeaves` computed on summary); ApiClient Ishikawa + FiveWhys method sections; `IshikawaList.razor` + `IshikawaDetail.razor` (fishbone card grid, per-category cause management, library typeahead, IsSelectedRootCause toggle); `FiveWhysList.razor` + `FiveWhysDetail.razor` (paginated list, recursive tree renderer, RenderFragment recursion pattern, incomplete-leaf warning); NavMenu Ishikawa Diagrams + 5 Whys links; MCP `get_rca_summary` tool (filter by linkedEntityId/status, returns both Ishikawa and FiveWhys tables); MCP server version 1.7 |
| 2.9     | 2026-03-15 | Phase 10d implemented: `MrbStatus`/`MrbDispositionDecision`/`MrbParticipantRole`/`MrbLinkedRcaType` enums; `MrbReview` + `MrbParticipant` entities (circular-FK-safe via `WithMany()` pattern, `NonConformance.MrbReviewId` stored as plain nullable property); `Phase10d_MrbReview` EF migration; `Phase10dDtos` (MrbReviewCreateDto/UpdateDto/ResponseDto/SummaryDto, MrbDecisionDto, MrbLinkRcaDto, MrbParticipantDto/AddDto/UpdateAssessmentDto); `MrbController` (create, update header, start-review, decide with RCA-gate, close, reopen, link-rca with RCA existence validation, participants CRUD); `NonConformancesController` MapToDto updated (MrbRequired + MrbReviewId), Quarantine auto-sets MrbRequired; ApiClient 12 MRB methods; `MrbList.razor` (/mrb) with status/decision/SCAR/supplier filters; `MrbDetail.razor` (/mrb/{id}) with NC summary, header edit, decision modal, RCA link modal, participant management; `NonConformanceList.razor` updated (Escalate to MRB modal + Open MRB navigation); NavMenu Material Review Board link; MCP `get_mrb_summary` tool (filter by status/scarRequired/supplierCaused); MCP server version 1.8 |
| 3.0     | 2026-03-15 | Phase 15 implemented: `ActionItemPriority`/`ActionItemStatus`/`ActionItemSourceType`/`ManagementReviewType`/`ManagementReviewStatus` enums; `ActionItem` + `ManagementReview` entities; `Phase15_ActionTracking` EF migration; `Phase15Dtos` (ActionItemDto/SummaryDto, CreateActionItemDto/UpdateActionItemDto/CompleteActionItemDto/VerifyActionItemDto, ManagementReviewDto/SummaryDto, CreateManagementReviewDto/UpdateManagementReviewDto, QualityScorecardDto with ActionItemAgeGroupDto/ActionItemSourceBreakdownDto); `ActionItemsController` (paginated list with 6 filters including assignedToMe, start/complete/verify/cancel lifecycle, two-step closure anti-self-certification, scorecard aggregation endpoint); `ManagementReviewsController` (CRUD, start with auto-populated NC/action-rate/MRB snapshots, complete, linked action items CRUD); ApiClient 18 Phase 15 methods; `MyActions.razor` (/my-actions, all roles, 4-section view: Overdue/Due Soon/Open/Awaiting Verification, Complete+Verify modals); `TeamActions.razor` (/team-actions, Admin/Engineer, filter bar, paginated table, Create modal); `QualityScorecard.razor` (/quality-scorecard, Admin/Engineer, KPI cards + priority/source breakdown tables + top overdue items); `ManagementReviewList.razor` (/management-reviews, Admin/Engineer, filter + create modal); `ManagementReviewDetail.razor` (/management-reviews/{id}, breadcrumb, auto-populated inputs card, manual inputs edit modal, decisions/targets edit modal, action items table + add action modal); Accountability NavMenu section with overdue badge; `MyWork.razor` overdue action items alert widget; MCP `get_management_review_status` tool; MCP server version 1.9 |
| 3.1     | 2026-03-16 | Phase 16 implemented: `ProcessRole.Training` value; `CompetencyRecord` + `ProcessTrainingRequirement` entities; `CompetencyTitle` + `CompetencyExpiryDays` on `Process`; `Phase16_TrainingCompetency` EF migration; `Phase16Dtos` (CompetencyRecordDto/SummaryDto, CompetencyMatrixRowDto/CellDto, TrainingComplianceSummaryDto, ProcessTrainingRequirementDto, AddCompetencyRecordDto, AddTrainingRequirementDto); `CompetencyController` (my-records, all-records, matrix, training-compliance aggregate, add-record, update-record, delete-record); `JobsController` enforcement hook (blocks job creation when assigned operator lacks enforced competency); `ManagementReviewsController` snapshot extended with training compliance; `ProcessesController.GetAll` projects `CompetencyTitle`/`CompetencyExpiryDays`; `ProcessSummaryResponseDto` extended with two optional trailing parameters; `Phase16Dtos` ApiClient methods (10 methods: GetMyCompetencyRecordsAsync, GetAllCompetencyRecordsAsync, GetCompetencyMatrixAsync, GetTrainingComplianceAsync, AddCompetencyRecordAsync, UpdateCompetencyRecordAsync, DeleteCompetencyRecordAsync, GetTrainingRequirementsAsync, AddTrainingRequirementAsync, DeleteTrainingRequirementAsync); `TrainingList.razor` (/training, all roles, process cards with competency status badges, Launch Training modal); `CompetencyMatrix.razor` (/competency-matrix, Admin/Engineer, cross-tab users × training-processes with icon cells); `ProcessDetail.razor` training requirements section (add/remove, enforcement badge); `QualityScorecard.razor` Training Compliance KPI panel; `ManagementReviewDetail.razor` training compliance column in snapshot card; Training NavMenu section (Training Catalogue + Competency Matrix); MCP `get_competency_status` tool; MCP server version 2.0 |
| 3.2     | 2026-03-16 | Three-view Process Builder: diagram view (existing), slide view (PowerPoint-style left thumbnail rail + full-height inline content editor), document view (Word-style read-only scrollable review); `_viewMode` state toggle (`Diagram`/`Slide`/`Document`) in builder top bar; slide view supports full inline editing (all content block types, add/edit/delete/reorder, step name/description with Apply, step add/reorder/delete via thumbnail rail); document view renders all steps with content blocks in a clean typeset layout with per-step Edit button that switches to slide view; `CommitStepEdit` extracted so name/description edits auto-commit on view switch; new methods: `SetViewMode`, `SelectSlideStep`, `MoveSlideStep`, `RemoveSlideStep`, `SwitchToSlideStep`, `LoadAllDocContent`; edit step modal restricted to Diagram view; CSS: `pb-slide-layout`, `pb-slide-rail`, `pb-slide-thumbnail`, `pb-slide-editor`, `pb-doc-view`, `pb-doc-step` |
| 3.3     | 2026-03-16 | Training Catalogue: role-gated "New Training" button (`AuthorizeView Roles="Admin,Engineer"`) added to `TrainingList.razor`; `ProcessBuilder.razor` extended with `@page "/training/builder"` and `@page "/training/{Id:guid}/builder"` routes; `IsTrainingContext` property (`Nav.Uri.Contains("/training/")`) controls breadcrumbs, page title, cancel navigation, process role assignment, and post-save redirect for training context |
| 3.4     | 2026-03-16 | `SeedQmsDocumentsAsync` added to `DataSeeder.cs` — 21 ISO 9001:2015 mandatory QMS documents seeded (QMS-001–QMS-021); 20 Released + 1 Draft (QMS-021 Knowledge Management, new and under review); covers all mandatory clauses; idempotency guard on QMS-001; called from `Program.cs` after existing seed |
| 3.5     | 2026-03-16 | `SeedTrainingDocumentsAsync` added to `DataSeeder.cs` — 12 system onboarding training courses seeded (TRN-SYS-001–TRN-SYS-012); descriptions serve as live user-facing module documentation; expiry: Never (awareness courses), 1 year (operational), 2 years (admin/approver); all Released; idempotency guard on TRN-SYS-001; called from `Program.cs` |
| 3.6     | 2026-03-16 | Process Timing report: `ProcessTimingReport.razor` at `/reports/process-timing` — per-process cards with min/avg/median/P95/max job duration stats, proportional stacked colour bar (one segment per step), collapsible per-step detail table, role filter dropdown, expand/collapse all; `GET /api/reports/process-timing?processRole=` endpoint in `ReportsController`; `ProcessTimingDto`/`StepTimingDto` records in `ReportDtos.cs`; `GetProcessTimingAsync` in `ApiClient`; "Process Timing" nav link added to Reports section |
| 3.7     | 2026-03-16 | Document Library filter and navigation: `Training` added to `documentRolesOnly` filter in `ProcessesController`; "Training" option added to type-filter dropdown in `DocumentList.razor`; Document Library replaced with a collapsible nav section in `NavMenu.razor` with 4 sub-links (All Documents / QMS Documents / Work Instructions / Training); `DocumentList.razor` gains `[SupplyParameterFromQuery(Name = "type")] TypeParam` with `OnParametersSetAsync` for query-parameter deep-linking; `_sectionPaths` updated to include `["documents"] = ["documents"]` |
| 4.0     | 2026-03-15 | Process engine scope expanded: `ContextEntityType` enum extended with `Supplier`/`Product`/`ManagementReview`/`AuditFinding` values; Phase 11f-6 added — Context Data Blocks (`ContentBlockType.ContextData`, `ContextDataKey` enum, automatic context panel per entity type, authored inline data slots); Phase 15c Management Review redesigned as Work Order (seeded workflow replaces bespoke entity for future runs; existing entity preserved); Phase 17 Internal Audit added (Work Order-based, findings as ExecutionData, CAPA as ActionItems); Phase 18 Supplier Management added (`Supplier` entity, supplier qualification/SCAR workflow, `ProcessRole.SupplierAudit`); Phase 19 Product & FAI added (`Product` entity, FAI workflow `ProcessRole.Inspection`, ECO workflow `ProcessRole.ChangeControl`); Phase 20 NPI added (Work Order gate framework, `Product.IsReleased`, dependency on Phases 12/19/11f) |
| 4.3     | 2026-03-15 | Phase 14c designed: `DocumentApprovalRequest` entity identified as inconsistent with Design Principle #6 (bespoke approval infrastructure reimplements Work Order capabilities); Phase 14c spec added — replaces `DocumentApprovalRequest` with a Work Order against a seeded Document Control Workflow once Phase 12 is built; `Job.DocumentApprovalRequestId` FK to be removed; rejection routing via Manual WorkflowLink replaces bespoke parallel-step cancellation hook; Phase 14 Implementation Status entry annotated with ⚠️ warning; Phase 14c added to Not Yet Built list |
| 4.2     | 2026-03-15 | Design Principle #6 added: Process Engine First — any activity that can be modelled as a Process or Workflow must be; bespoke entity hierarchies, status machines, or sequencing logic for specific features are a design error; "Process engine first" callout section added to Core Construct Hierarchy |
| 4.1     | 2026-03-15 | Core Construct Hierarchy documented and formalised: Step (atomic unit, no sub-units) → Process (linear sequence of Steps, no branching) → Workflow (network of Processes, branching via Routing Decisions); Job (one execution of a Process) → Work Order (one execution of a Workflow, auto-sequences Jobs through the graph); `WorkflowJob` entity and all associated terminology renamed to `WorkOrder` / Work Order throughout the plan; §3.4 Job in terminology.md corrected (previously described Job as "overarching work order driving items through Workflows" — now correctly states a Job executes one Process); terminology.md v0.5 |
| 3.9     | 2026-03-15 | Phase 11 architecture revised: `MaintenanceTask` entity superseded by Jobs-based PM design; Phase 11d spec updated to fire `Job` records (not `MaintenanceTasks`) from `MaintenanceTrigger.ProcessId`; Phase 11f added — Process Engine Enhancements: `Job.ContextEntityType`/`ContextEntityId` polymorphic context pattern, `ProcessRole.Maintenance`, `Process.ExecutionMode` (Auto/Wizard/Checklist), free-form job (`Job.ProcessId` nullable), Quick-create Job modal; `Equipment.DefaultMaintenanceProcessId`; `DowntimeRecord.LinkedJobId` replaces `LinkedMaintenanceTaskId` |
| 3.8     | 2026-03-16 | Phase 11 implemented (Production Management): `DowntimeType`/`MaintenanceTriggerType`/`MaintenanceTaskType`/`MaintenanceTaskStatus` enums; `EquipmentCategory`/`Equipment`/`DowntimeRecord`/`MaintenanceTrigger`/`MaintenanceTask` entities; `StepTemplate` extended with `ExpectedDurationMinutes`/`RequiredEquipmentCategoryId`; `Job` extended with `DueDate`/`PlannedStartDate`; `StepExecution` extended with `EquipmentId`; `Phase11_ProductionManagement` EF migration; `Phase11Dtos` (full Equipment, Category, Downtime, Trigger, Task, WipJobDto, BottleneckStepDto, ProductionDashboardDto); `EquipmentController` (full CRUD for categories, equipment, downtime start/close, triggers CRUD, tasks lifecycle: create/start/complete/cancel, paginated all-tasks); `ProductionController` (`/api/production/wip` WIP board + dashboard, `/api/production/bottlenecks`); `JobsController` updated (DueDate/PlannedStartDate on Create/Update/MapToDto, EquipmentId/EquipmentCode on MapStepExecutionToDto); `Phase11` ApiClient section (30 methods: categories, equipment, downtime, triggers, tasks, dashboard, bottlenecks); `ProductionDashboard.razor` (/production — KPI cards, late jobs table, WIP board, bottlenecks, maintenance due); `EquipmentList.razor` (/equipment — paginated table, search/category/active filters, create modal); `EquipmentDetail.razor` (/equipment/{id} — downtime log/resolve/history, triggers CRUD, tasks lifecycle); `MaintenanceTaskList.razor` (/maintenance — paginated all tasks, status/type filter, create modal, task actions); Production NavMenu section (Production Dashboard / Equipment / Maintenance Tasks, overdue+due badge); MCP tools `get_production_status`, `list_equipment_downtime`, `list_overdue_maintenance`; MCP server version 2.1 |

---

## Vision

A system that treats manufacturing process designs as the central organizing structure of a manufacturing enterprise. The process model is the primary instrument of operational discipline: defining what work looks like, how it is authorised, how it is executed, and how conformance is demonstrated. By investing in rigorous process definition, a manufacturing company maximises its ability to understand, control, and continuously improve its operations.

**Scope:** This system is designed specifically for manufacturing operations. Generalisation to other business process domains (HR, finance, sales) is intentionally deferred until the manufacturing use case is fully mature.

---

## Design Principles

1. **Process model is the core.** Everything else is a consumer of or contributor to the process model.
2. **Design together, build incrementally.** The data model for Phases 1–3 is designed as a unit before any code is written, then built phase by phase.
3. **Each phase delivers standalone value.** A manufacturing engineer can benefit from the system before it is "complete."
4. **Domain-neutral internals, domain-specific labels.** The system uses generic terms (Kind, Grade, Item) internally and maps them to user-facing vocabulary (Part, Disposition, Serial Number) via configuration.
5. **Type safety prevents errors.** Ports enforce Item Types (Kind + Grade) so that wrong items cannot flow to wrong places.
6. **Process engine first — no bespoke architecture for what the engine already handles.** If an activity can be modelled as a Process or Workflow, it must be. Custom entities, custom status lifecycles, and custom sequencing logic for specific features are a design failure. The management review, internal audit, maintenance, FAI, supplier qualification, ECO, and NPI gate frameworks are all deliberately expressed as Processes and Work Orders rather than as dedicated entity hierarchies. When a new feature requires tracking status, assigning responsibility, sequencing steps, or recording execution history, **the first question is always: can this be a Process?**

---

## Core Construct Hierarchy

The system is built around five constructs arranged in two layers. Understanding this hierarchy is essential to all architectural decisions.

### Design layer — what work looks like

| Construct | Definition |
|---|---|
| **Step** | The atomic unit of work. One operation, clearly scoped, with explicitly defined Input Ports and Output Ports. A Step is the smallest unit the system can track and execute. |
| **Process** | A strictly linear sequence of Steps. One entry point, one exit point. **There is no branching within a Process.** Branching occurs only at the Workflow level. |
| **Workflow** | A network of Processes connected by Routing Decisions. Expresses the full end-to-end path that work can take, including branches, merge points, and rework loops. |

### Execution layer — how work runs

| Construct | Definition |
|---|---|
| **Job** | One running execution of a Process. Created whenever a Process needs to be performed — manually requested, triggered by a schedule, or automatically created by the Workflow sequencing service as a Work Order advances. |
| **Work Order** | One running execution of a Workflow. Creates and sequences Jobs for each Process node in the graph, advancing them automatically as each Job completes and Routing Decisions resolve. |

### The mapping is precise

- A Job executes exactly one Process.
- A Work Order executes exactly one Workflow.
- Every Job spawned by a Work Order carries a `WorkOrderId` FK linking it back to the parent Work Order.

### Why this matters

- Routing complexity is isolated to the Workflow level — Process authors never reason about branching.
- A single Job is always interpretable in isolation: who does it, what the Process requires, and whether it succeeded.
- Completed Work Orders provide a complete, queryable execution record of an entire multi-process activity.
- The design/execution split means Processes and Workflows are reusable templates; Jobs and Work Orders are the immutable history.

### Process engine first

> **Any activity that can be modelled as a Process or Workflow must be. Building a bespoke entity hierarchy, status machine, or sequencing mechanism for a specific feature — when the process engine already provides all of that — is a design error.**

This principle exists because the temptation to build dedicated entities for specific workflows (management reviews, audits, maintenance plans, change orders) is strong and recurring. Each time it is done, the result is a partial reimplementation of the process engine that cannot share assignment, scheduling, execution wizard, action item, or reporting infrastructure. The correct response to any new structured-activity requirement is: define a Workflow, seed its Processes and Steps, and let the engine execute it. Reserve new entities for data that genuinely has no process-engine equivalent.

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

### Phase 2 — Step Design (Steps, Ports, and Prompts) ✅

**Goal:** Define *what work looks like.*

**Status:** Implemented — full CRUD API and Blazor UI (StepTemplateList, StepTemplateDetail with port management). `PromptDefinition` and `PromptOption` are **designed, not yet built** — see below.

**Delivers:**
- Design individual Steps with named Input and Output Ports
- Each Port declares exactly one Item Type (Kind + Grade) and a Quantity Rule
- Steps are classified by pattern (Transform, Assembly, Division, General)
- Steps are reusable — designed once, used in multiple Processes
- **Prompts:** define what data the operator must collect during the step, independent of Ports

**Standalone value:** Documented operations with formal input/output definitions and structured data-collection forms.

**Key entities:**
- Step (template/definition)
- Port (Input / Output) — quality-tool connection points for PFMEA, C&E Matrix, Control Plan
- PromptDefinition — operator data-collection form fields (label, data type, required, scope, validation)
- PromptOption — choice list entries for Select / MultiSelect prompts
- Quantity Rule

**PromptDefinition design notes:**

Ports and PromptDefinitions are two independent extension points on a `StepTemplate`. A Port models process-knowledge relationships (why this step affects quality). A `PromptDefinition` models what the operator is asked to enter and gates step completion. They coexist and may overlap conceptually but neither depends on the other.

Key `PromptDefinition` fields:
- `key` — machine-readable name, unique per StepTemplate, copied to ExecutionData on capture
- `collection_scope` — `PerStep` / `PerItem` / `PerBatch` (controls form repetition)
- `is_required` — if true, step cannot be completed without an answer
- `data_type` — extended enum: `String`, `Integer`, `Decimal`, `Boolean`, `DateTime`, `Select`, `MultiSelect`, `Barcode`, `Photo`, `Signature`, `UserPicker`
  - `UserPicker` renders as a user search/select backed by the Identity user table; stores the selected user's Id string in `ExecutionData.Value`; displays the user's Display Name in the wizard and in reports. Use cases: recording who delivered training (instructor capture), two-person integrity witness, handoff signatory, customer/supplier buyoff witness. The executing operator's own identity is captured automatically from the session — `UserPicker` is for *other named parties* involved in the step.
- `lower_limit` / `upper_limit` / `validation_pattern` — field-level validation rules

`ExecutionData` updated to add `prompt_definition_id` (nullable FK), widen `value` from `string(1000)` → `text`, and share the extended `DataType` enum.

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

#### 7c — Control Plan

A **Control Plan** is the operational companion to the PFMEA and C&E Matrix. Where the PFMEA identifies what can go wrong and rates risk, and the C&E Matrix ranks which inputs most affect quality, the Control Plan specifies *what to actually do*: what characteristics to measure, with what equipment, at what sample rate, and what to do when a result falls out of specification. It is the document the production floor uses during execution — the source of truth for inspections, gauging steps, and reaction instructions.

Building Control Plans inside the system rather than in spreadsheets means they stay in sync with the process model and the quality tools that informed them.

**Delivers:**
- Create a Control Plan for any Process; the system pre-populates one entry row per ProcessStep
- For each step entry, define one or more **characteristic rows** (a step commonly has multiple measured characteristics):
  - Characteristic name (e.g. "Torque", "Surface Finish Ra", "Insertion Depth")
  - Characteristic type: **Product** (measuring the output item) or **Process** (measuring the process parameter)
  - Specification or tolerance (e.g. "25 ± 2 Nm", "Ra ≤ 1.6 µm", "12–14 mm")
  - Measurement technique — the tool or method used (torque wrench, CMM, calliper, visual, attribute gauge)
  - Sample size (e.g. "100%", "1 per 50 pieces", "3 per batch")
  - Sample frequency (e.g. "Each piece", "First article", "1 per shift")
  - Control method (SPC chart, go/no-go gauge, poka-yoke, visual check)
  - Reaction plan — what the operator must do when a measurement is out of specification
  - Optional link to a `PfmeaFailureMode` row — tracing the control back to the risk that motivated it
  - Optional link to a `Port` — connecting the Control Plan specification to the port where this characteristic is measured
- PFMEA staleness integration: when a Process is released as a new version, linked Control Plans are marked stale alongside PFMEAs (same Phase 9 staleness mechanism)
- CSV export of the full Control Plan (one row per characteristic)

**Key entities:**
- `ControlPlan` (Id, ProcessId, Name, Version, IsActive)
- `ControlPlanEntry` (Id, ControlPlanId, ProcessStepId, CharacteristicName, CharacteristicType: Product/Process, SpecificationOrTolerance, MeasurementTechnique, SampleSize, SampleFrequency, ControlMethod, ReactionPlan, LinkedPfmeaFailureModeId nullable, LinkedPortId nullable, SortOrder)
- `CharacteristicType` enum: `Product`, `Process`

---

## Architecture Decision: Quality Engineering Tools (2026-03-02)

### Scope and Placement

Quality tools are tightly coupled to the process model: a PFMEA is meaningless without the process structure it analyses, a C&E Matrix is meaningless without the step inputs and outputs it relates, and a Control Plan is meaningless without the process steps and measured characteristics it governs. The three tools form a complete quality planning loop — C&E identifies the inputs worth controlling, PFMEA rates the risk of failure modes and selects controls, and the Control Plan operationalises those controls for the shop floor. Embedding all three inside the same application (rather than in separate spreadsheets) means they stay in sync when processes are revised, and their traceability links remain intact.

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

### Control Plan Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Granularity | One Control Plan per Process | A Control Plan covers the full manufacturing routing and is used as a single shop-floor document; process-level is the right scope, matching how PFMEA is scoped |
| Entry scope | One or more characteristic rows per ProcessStep | Multiple characteristics per step are common (e.g. torque AND angle on a fastening step); forcing one row per step would lose information |
| PFMEA traceability | Optional `LinkedPfmeaFailureModeId` per entry | Connects detection controls rated in the PFMEA to their operational specification in the Control Plan; not forced because not all characteristics originate from a PFMEA action |
| Port traceability | Optional `LinkedPortId` per entry | Ties the Control Plan characteristic directly to the Port where it is measured; enables future validation that hard limits on prompts match Control Plan specifications |
| Versioning | Control Plan marked stale when Process is released (same mechanism as Phase 9 PFMEA staleness) | Ensures the Control Plan is reviewed whenever the process changes — the same obligation applies to both quality documents |
| Reaction plan | Free text per entry | Reaction plans vary significantly by characteristic and step; prescriptive structure adds no value here |

### Relationship to Existing Features

- A high-RPN PFMEA failure mode whose `CurrentDetectionControls` describe a measurement prompt gives engineers a path to configure an **out-of-range alert** on that prompt — connecting PFMEA risk identification to live operational detection.
- C&E matrix priority scores identify which inputs are worth writing Control Plan entries for — the matrix output drives the control plan content.
- A Control Plan entry specifies *how to act* on what the PFMEA identified: the detection controls that rate `D` in the PFMEA become the measurement technique and sample plan in the Control Plan, closing the loop from risk rating to operational instruction.
- Control Plan entries whose `LinkedPortId` matches a Step Template port connect the Control Plan specification directly to the operator's data-capture screen — the tolerance in the Control Plan and the hard limit on the prompt should agree.
- All three tools will be exposed via the **MCP server** (Phase 7 MCP tools: `get_pfmea`, `list_high_rpn_failure_modes`, `get_ce_matrix`, `get_control_plan`, `list_critical_characteristics`).

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

### Phase 10 — Root Cause Analysis & Material Review

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

#### 10d — Material Review Board

A Material Review Board (MRB) is the formal, cross-functional process for reviewing nonconforming material whose disposition cannot be determined unilaterally at the floor level — where the consequence, complexity, or origin of the non-conformance requires a structured group decision and a written record.

**Relationship to Phase 8c (NonConformance):** These are not two different things. A `NonConformance` is the *detection record* — generated at the moment an out-of-spec measurement or failure is encountered during execution. The MRB is the *formal disposition process* for that NC when it cannot be closed quickly. The `Quarantine` disposition introduced in Phase 8c is the bridge: an NC dispositioned as Quarantine is waiting for MRB review. The MRB review updates the NC's final disposition when decided.

**Escalation criteria — any of the following triggers MRB:**
- NC disposition is `Quarantine` (the only structured exit from Quarantine is an MRB decision)
- `UseAsIs` deviation on a safety-critical or customer-facing characteristic
- Repeat NC on the same step or process within a rolling window
- Supplier-caused NC (incoming material affected)
- Customer notification may be required

**Key entity: `MrbReview`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `NonConformanceId` | Guid | FK → NonConformance |
| `Status` | enum | `Draft` / `UnderReview` / `Decided` / `Closed` |
| `ItemDescription` | string | Full description of the nonconforming item |
| `QuantityAffected` | string? | Quantity or lot size affected |
| `ProblemStatement` | string | Technical description of the nonconformance |
| `DispositionDecision` | enum | `UseAsIs` / `Rework` / `Scrap` / `ReturnToSupplier` / `ReworkAndReturn` |
| `DispositionJustification` | string? | Technical justification recorded at time of decision |
| `CustomerNotificationRequired` | bool | |
| `ScarRequired` | bool | Supplier Corrective Action Request needed |
| `SupplierCaused` | bool | NC originates from incoming material |
| `RequiresRca` | bool | An RCA analysis must be linked before status can advance to Closed |
| `LinkedRcaAnalysisType` | enum? | `Ishikawa` / `FiveWhys` |
| `LinkedRcaId` | Guid? | FK to `IshikawaDiagram` or `FiveWhysAnalysis` |
| `CreatedBy` | string | |
| `CreatedAt` | DateTime | |
| `DecidedBy` | string? | |
| `DecidedAt` | DateTime? | |

**Key entity: `MrbParticipant`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `MrbReviewId` | Guid | FK |
| `UserId` | string | FK → Identity user |
| `Role` | enum | `QualityEngineer` / `ManufacturingEngineer` / `DesignEngineer` / `CustomerRepresentative` / `SupplierRepresentative` / `ManagementApproval` |
| `IsRequired` | bool | Must be present for quorum |
| `Assessment` | string? | Notes from this participant's technical review |
| `AssessedAt` | DateTime? | |

**Fields added to `NonConformance`:**
- `MrbRequired` bool — set automatically when escalation criteria are met, or manually by a Quality Engineer
- `MrbReviewId` Guid? — FK to the linked `MrbReview` once opened

**Integration with Phase 10 RCA:**
- `MrbReview.Status` cannot advance to `Closed` while `RequiresRca = true` and `LinkedRcaId` is null
- The MRB detail page shows the linked RCA summary: status, root causes identified, corrective actions assigned
- Corrective actions from the RCA flow into Phase 15's unified `ActionItem` system with `SourceEntityType = MrbReview`

**Integration with Phase 15 (Action Tracking):**
- On MRB decision, required corrective actions are entered directly on the MRB detail page and become `ActionItem` records
- SCAR generation creates an `ActionItem` assigned to the procurement or supplier quality team with a due date
- The MRB cannot close until all linked action items are in `Complete` or `Verified` status

**Surfaces:**
- `NonConformanceDetail` — "Escalate to MRB" button shown when `MrbRequired = true` or disposition is `Quarantine`
- `MrbList` — all open and recent reviews, filterable by status / disposition decision / supplier-caused flag
- `MrbDetail` — full review record: NC summary card, participant list, disposition decision form, RCA link, corrective actions
- MCP tool `get_mrb_summary` — count and details of open MRBs, any with SCAR required

**Status:** Designed — Phase 8c (NonConformance) is already built; Phase 10a–c (RCA tools) and Phase 15 (ActionItem) are prerequisites for the full linked workflow.

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
| `LinkedJobId` | FK? | The Job (PM or corrective maintenance) that caused or resolved this downtime — links directly to the Job engine rather than a bespoke maintenance task record |

**Derived metrics (computed from DowntimeRecords):**
- `Availability %` = (scheduled time − downtime) / scheduled time per equipment per period
- `MTBF` (Mean Time Between Failures) = average time between unplanned downtime starts
- `MTTR` (Mean Time To Repair) = average duration of unplanned downtime events

**Production impact linkage:** When a `DowntimeRecord` is open (no `EndedAt`), any StepExecution whose `EquipmentId` matches and whose status is pending or in-progress is flagged as *equipment-blocked* on the visibility dashboard.

---

#### 11d — Preventive Maintenance Scheduling

Time-based and usage-based maintenance triggers that automatically schedule PM work before failures occur. PM work is executed through the standard Job engine — not a bespoke maintenance task system — giving PM the full benefit of process definitions, structured prompts, content blocks, competency enforcement, and the guided execution wizard. The `MaintenanceTask` entity used in the initial Phase 11 implementation is superseded by this Jobs-based design; see Phase 11f for the prerequisite engine enhancements.

**Key entity: `MaintenanceTrigger`**

| Field | Type | Purpose |
|---|---|---|
| `EquipmentId` | FK | The machine this trigger watches |
| `ProcessId` | FK → Process | The PM process to run when this trigger fires; must be `ProcessRole.Maintenance` |
| `TriggerType` | enum | `TimeBased` or `UsageBased` |
| `IntervalDays` | int? | For TimeBased: days between jobs |
| `IntervalUsageCycles` | int? | For UsageBased: step executions on this equipment between jobs |
| `LastTriggeredAt` | DateTime? | When the most recent PM job was created |
| `NextDueAt` | DateTime? | Computed: `LastTriggeredAt + IntervalDays`, or derived from usage count |
| `AdvanceNoticeDays` | int | How many days before due to surface the upcoming PM work |

**`Equipment`** gains:
- `DefaultMaintenanceProcessId FK? → Process` — the standard PM process for this machine; pre-fills the Quick PM Start button on EquipmentDetail (see Phase 11f-5)

**PM generation:** When `NextDueAt` is within `AdvanceNoticeDays`, a background check (or on-demand trigger) creates a `Job` against `MaintenanceTrigger.ProcessId` with:
- `ContextEntityType = Equipment`
- `ContextEntityId = MaintenanceTrigger.EquipmentId`
- `DueDate = NextDueAt`

The resulting Job is a standard Job in every respect — the operator executes it through the guided wizard (or checklist view for simple routines), all steps and prompts are defined by the PM process, and completion is tracked by Job status. `MaintenanceTrigger.LastTriggeredAt` is updated on Job creation; `NextDueAt` is recomputed from the recurrence interval.

**Calibration integration:** For equipment subject to calibration, the `MaintenanceTrigger` references a Calibration-category PM Process whose steps capture the calibration result and pass/fail outcome. Equipment with an overdue calibration Job (`ContextEntityType = Equipment`, `DueDate` past, `Status != Completed`) is flagged on the equipment card and on any `StepExecution` that used that equipment while it was out of calibration — important for traceability in regulated environments.

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

**Maintenance Due List:** All Jobs where `ContextEntityType = Equipment`, `Status != Completed`, and `DueDate < today`, sorted by `DueDate` ascending, showing equipment name, job name, due date, and days overdue.

**Surfaces:**
- `ProductionDashboard.razor` — the main visibility page (route `/production`)
- NavMenu entry under a new "Production" section
- `EquipmentList.razor` / `EquipmentDetail.razor` — catalog with full history; EquipmentDetail includes a **Jobs tab** filtered to `ContextEntityType = Equipment, ContextEntityId = equipment.Id`, showing all PM and ad-hoc maintenance jobs for that machine
- `GET /api/equipment` — paginated equipment catalog
- `GET /api/equipment/{id}/downtime` — downtime history
- `GET /api/jobs?contextEntityType=Equipment&contextEntityId={id}` — PM and ad-hoc jobs for a specific equipment record (standard jobs endpoint with context filters; see Phase 11f-1)
- `GET /api/production/wip` — current WIP state for the dashboard
- `GET /api/production/bottlenecks` — ranked bottleneck step list
- MCP tools: `get_production_status` (WIP summary), `list_equipment_downtime` (current and recent), `list_overdue_maintenance` (queries Jobs by context entity type)

---

#### 11f — Process Engine Enhancements for Lightweight Work

**Goal:** Remove the friction that makes the process engine feel heavy for simple tasks, so that the same engine that handles a 15-step manufacturing process also handles a 3-step lubrication routine or an ad-hoc "fix the coolant line" ticket. This sub-phase is a prerequisite for the Phase 11d PM refactor and delivers general capability improvements that benefit every phase.

**Status:** Designed — must be built before Phase 11d refactor is implemented.

---

##### 11f-1 — `Job.ContextEntityType` + `Job.ContextEntityId`

Two new nullable fields on `Job`:

| Field | Type | Purpose |
|---|---|---|
| `ContextEntityType` | enum? | The type of entity this job is contextually attached to |
| `ContextEntityId` | Guid? | The specific entity instance |

`ContextEntityType` enum values (closed set — not a free string):

| Value | Entity | Use case |
|---|---|---|
| `Equipment` | Equipment | PM job, corrective maintenance job, inspection job for a specific machine |
| `NonConformance` | NonConformance | Investigation or rework job triggered by an NC |
| `MrbReview` | MrbReview | Corrective action job generated by an MRB decision |
| `Supplier` | Supplier | Supplier qualification audit, SCAR follow-up, re-audit job |
| `Product` | Product | First Article Inspection job, engineering change verification, NPI gate review |
| `ManagementReview` | ManagementReview (Work Order context) | Inputs collection or follow-up action job linked to a management review run |
| `AuditFinding` | StepExecution (audit finding ExecutionData) | Corrective action job generated from an audit finding |

The closed enum allows the UI to render typed navigation links and context data panels: a Job with `ContextEntityType = Equipment` renders an "Equipment: CNC-01" card that routes to `/equipment/{ContextEntityId}`; a `Product` job renders the product name and routes to `/products/{ContextEntityId}`.

**Effect on API:** `GET /api/jobs` accepts `?contextEntityType=` and `?contextEntityId=` query parameters for filtering. Job create/update DTOs accept optional `ContextEntityType` + `ContextEntityId`.

**Effect on UI:** Job list and job create modal updated. EquipmentDetail gains a **Jobs tab** filtered to `ContextEntityType = Equipment, ContextEntityId = equipment.Id`. Future phases: NonConformanceDetail will similarly show related jobs.

---

##### 11f-2 — `ProcessRole.Maintenance`

The `ProcessRole` enum (values: `ManufacturingProcess`, `ApprovalProcess`, `QmsDocument`, `WorkInstruction`, `Training`) gains a sixth value: `Maintenance`.

Effects:
- PM processes are organized separately from manufacturing and training processes in all lists and pickers
- Quick PM Start button on EquipmentDetail pre-filters the process picker to `ProcessRole = Maintenance`
- `MaintenanceTrigger.ProcessId` is validated on creation to reference a Maintenance-role process
- Maintenance processes do not appear in the standard manufacturing job queue

---

##### 11f-3 — `Process.ExecutionMode` (Checklist vs. Wizard)

A new `ExecutionMode` field on `Process`:

| Value | Behaviour |
|---|---|
| `Auto` (default) | System decides: if all steps have zero prompts and zero AcknowledgmentRequired safety blocks → render as checklist; otherwise → wizard |
| `Wizard` | Always use the 5-phase execution wizard regardless of step content |
| `Checklist` | Always use checklist mode (explicit override; appropriate for routines the operator should follow sequentially without data capture) |

**Checklist view** (`/execute/{jobId}/checklist`): All steps rendered on one page as a vertical list. Each step row shows the step name, description, and a "Mark Complete" button. No phase navigation. No prompt forms. Completing all steps closes the job. Appropriate for simple PM routines where the operator follows a procedure card and just needs to confirm each action was done.

The 5-phase wizard remains the default for any process with data capture.

---

##### 11f-4 — Free-form Job (`Job.ProcessId` nullable)

Making `ProcessId` nullable enables ad-hoc work orders that do not correspond to any defined process — e.g. "Replace broken coolant guard on CNC-01", "Fix labels on rack B".

When `ProcessId` is null:
- `Job.Title` is a standalone required field (for process-backed jobs, `Name` is currently derived from the process)
- No `StepExecution` records are created
- A single "Mark Complete" action closes the job; the user records a completion note
- Rendered as a simple card with a completion button; no wizard navigation

Free-form jobs fully support `ContextEntityType` / `ContextEntityId`, so ad-hoc maintenance tickets on specific equipment still appear in the equipment's Jobs tab and in the production dashboard's maintenance view.

---

##### 11f-5 — Quick-create Job Modal

A reusable `<QuickCreateJobModal>` Blazor component parameterised by:
- `PreselectedProcessRole` — pre-filters the process dropdown (e.g. `Maintenance`)
- `ContextEntityType` + `ContextEntityId` — pre-fills the context association
- `DefaultDueDate` — pre-fills due date (e.g. `NextDueAt` from the triggering `MaintenanceTrigger`)
- `AllowFreeForm` — when true, shows a "No process / ad-hoc" toggle

Used from:
- `EquipmentDetail` — "Start PM" button (role = Maintenance, context = Equipment)
- `EquipmentDetail` — "Log Ad-hoc Task" button (free-form, context = Equipment)
- `NonConformanceDetail` (future) — "Start Investigation Job" button
- `JobList` — general new-job creation

Creates the Job (and optionally starts the first step) in one action without navigating away from the triggering page.

---

**Schema changes (single migration `Phase11f_ProcessEngineEnhancements`):**

| Change | Scope |
|---|---|
| `ContextEntityType enum?` + `ContextEntityId Guid?` → `Job` | New nullable columns |
| `DefaultMaintenanceProcessId Guid?` → `Equipment` | New nullable FK |
| `ProcessId FK` replaces `Title string` on `MaintenanceTrigger` | Schema change + data migration |
| `ExecutionMode enum` → `Process` | New column, default `Auto` |
| `ProcessRole.Maintenance` value | Enum extension, no schema change |
| `Job.ProcessId` → nullable | Column nullability change |
| New `ContextEntityType` enum | New C# enum |
| New `ExecutionMode` enum | New C# enum |

---

##### 11f-6 — Context Data Blocks

**The problem:** The execution wizard today has two information layers — static *content blocks* (authored text and images) and *prompts* (data capture). There is a third layer that certain process types require: **live system data resolved at execution time** — not authored when the process was designed, but fetched from the database when the wizard opens. Without this, process authors must write instructions like "check the equipment's recent downtime history in a separate window" instead of embedding it inline.

**Two-layer approach:**

**Layer 1 — Automatic Context Panel (no authoring required):**
When a Job has a `ContextEntityType`, the wizard automatically renders a collapsible context card at the top of every step showing the most relevant live data for that entity type:

| ContextEntityType | Auto context panel content |
|---|---|
| `Equipment` | Equipment name, current status, last 3 downtime events, upcoming PM due dates |
| `NonConformance` | NC summary: step, actual value, limit breached, current disposition |
| `Supplier` | Supplier approval status, count of open NCs, last audit date |
| `Product` | Product code/name/revision, linked process count, open FAI status |
| `MrbReview` | MRB status, NC summary, participant list, required RCA status |

This requires zero process authoring — it is generated entirely from the job's `ContextEntityType` value and the API layer.

**Layer 2 — Authored Context Data Blocks (process author places live data inline):**
A new `ContentBlockType.ContextData` with a `ContextDataKey` enum value. Authors embed named data slots into the step content sequence (interleaved with text, images, and prompts) where they want live data to appear at execution time:

`ContextDataKey` enum values (closed set):

| Key | Resolves to | Required ContextEntityType |
|---|---|---|
| `equipment.recent_downtime` | Last N downtime events for this machine | Equipment |
| `equipment.open_pm_jobs` | Open PM jobs for this machine | Equipment |
| `review_period.nc_summary` | NC count + trend chart for the configured review period | any / ManagementReview |
| `review_period.action_close_rate` | % of action items closed on time | any |
| `review_period.mbr_open_count` | Count of open MRBs and average age | any |
| `prior_management_review.action_items` | Action items from the most recent prior review | ManagementReview |
| `audit.open_findings_count` | Open corrective actions from prior audit | AuditFinding |
| `product.linked_processes` | Processes currently associated with this product | Product |
| `product.pfmea_status` | PFMEA list for this product's processes + RPN summary | Product |
| `product.control_plan_status` | Control Plan list for this product's processes | Product |
| `product.trial_run_job_results` | Cycle time, yield, NC count from Jobs tagged to this product | Product |
| `product.fai_job_status` | FAI jobs for this product + pass/fail status | Product |
| `supplier.open_ncs` | Open NCs linked to this supplier | Supplier |

**Schema:** `StepTemplateContent` gains two new nullable fields:
- `ContextDataKey enum?` — set when `ContentBlockType = ContextData`
- `ContextDataLabel string?` — optional override for the panel heading (defaults to a system label per key)

**Wizard rendering:** When the wizard encounters a `ContextData` content block, it calls a new `GET /api/context-data/{key}?jobId={id}` endpoint that resolves the key against the job's `ContextEntityId` and returns a structured data object. The wizard renders it as a well-formatted readonly panel (table, list, or chart as appropriate to the data type) in-line with the other content blocks.

**Migration:** Additive-only — new nullable columns on `StepTemplateContent`. The `ContextDataKey` enum and the `ContextData` value on `ContentBlockType` require an enum extension.

**Status:** Designed — included in `Phase11f_ProcessEngineEnhancements` migration. API endpoint `GET /api/context-data/{key}` required; implementations for each key added incrementally as new phases introduce the underlying entities.

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

### Phase 12 — Workflow Execution & Department Assignment

**Goal:** Enable a workflow to be *run* as a single top-level work order, with each contained process automatically assigned to a responsible department, work area, or role, and advanced through the workflow graph as each process job completes.

**Design premise:** The workflow graph, routing links, and grade-based conditions are all already built. The missing pieces are: (1) an assignable-entity model, (2) a workflow-level execution record that ties the run together, and (3) a sequencing service that watches for job completion and creates the next job in the graph.

---

#### 12a — OrgUnit (Assignable Entity)

A flexible entity covering any type of responsible party: department, work area, role, or individual. A self-referential parent relationship supports hierarchy (e.g. "Quality" as a parent of "Incoming Inspection" and "Final Inspection").

**Key entity: `OrgUnit`**

| Field | Type | Purpose |
|---|---|---|
| `Code` | string(50) | Short identifier (e.g. "QC", "ASSY", "ENG") |
| `Name` | string(200) | Human-readable name |
| `Type` | enum | `Department`, `WorkArea`, `Role`, `Person` |
| `ParentId` | FK → OrgUnit, nullable | Parent in the hierarchy |
| `IsActive` | bool | Whether available for assignment |

---

#### 12b — Assignee on WorkflowProcess

A single nullable FK added to the existing `WorkflowProcess` node entity:

```
WorkflowProcess
  + assignee_id  (FK → OrgUnit, nullable)
```

Nullable because not all workflows use assignment. When set, it declares which OrgUnit is responsible for executing the process at that node.

---

#### 12c — Work Order (Workflow Execution Record)

A parent-level record for a complete workflow run. Analogous to `Job` for a single process, but spanning the entire workflow graph.

**Key entity: `WorkOrder`**

| Field | Type | Purpose |
|---|---|---|
| `WorkflowId` | FK → Workflow | The workflow being executed |
| `Subject` | string(500) | What this run is about (e.g. "Batch #4421", "New hire: J. Smith") |
| `Status` | enum | `Running`, `Completed`, `Cancelled` |
| `StartedAt` | DateTime? | When the first process job was created |
| `CompletedAt` | DateTime? | When the final process job completed |

**`Job` gains two nullable FKs:**

```
Job
  + workorder_id      (FK → Work Order, nullable)
  + workflow_process_id  (FK → WorkflowProcess, nullable)
```

These link each process-level Job back to its parent workflow run and to the specific graph node it represents, enabling the sequencing service to determine which outgoing links to follow on completion.

---

#### 12d — Sequencing Service

Triggered whenever a `Job` status transitions to `Completed`. If the job has a `WorkOrderId`:

1. Look up the `WorkflowProcess` node the job corresponded to (`WorkflowProcessId`)
2. Find all outgoing `WorkflowLink` edges from that node
3. Evaluate routing: `Always` links always fire; `GradeBased` links fire when the job's item grades match a `WorkflowLinkCondition`; `Manual` links wait for an operator to confirm
4. For each link that fires, create a new `Job` for the target `WorkflowProcess.ProcessId`, set `WorkOrderId` and `WorkflowProcessId`, and notify `WorkflowProcess.AssigneeId`
5. If no outgoing links fire (terminal node), mark the `WorkOrder` as `Completed`

This is the complete sequencing loop. No changes to `WorkflowLink` or `WorkflowLinkCondition` are required — the graph routing model is already sufficient.

---

**Key entities added:**
- `OrgUnit` (Id, Code, Name, Type, ParentId, IsActive)
- `WorkOrder` (Id, WorkflowId, Subject, Status, StartedAt, CompletedAt)
- `WorkOrderStatus` enum: `Running`, `Completed`, `Cancelled`
- `OrgUnitType` enum: `Department`, `WorkArea`, `Role`, `Person`

**Existing entities modified:**
- `WorkflowProcess` + `assignee_id` (FK → OrgUnit, nullable)
- `Job` + `workorder_id` (FK → Work Order, nullable) + `workflow_process_id` (FK → WorkflowProcess, nullable)

**Surfaces:**
- `OrgUnitList.razor` — manage departments, work areas, roles
- `WorkflowDetail` updated — assignee picker per node
- `WorkOrderList.razor` / `WorkOrderDetail.razor` — start a workflow run, view all in-flight runs, track progress through the graph
- `MyWork` page updated — operators see jobs assigned to their OrgUnit(s) in addition to directly assigned jobs
- `POST /api/workorders` — start a new workflow run
- `GET /api/workorders/{id}` — current state + graph progress
- Notification hooks for assignees when a new process job is created for their OrgUnit

#### 12e — WorkflowSchedule (Periodic Execution)

Workflows that run on a fixed recurrence (e.g. monthly calibration, weekly safety walk, quarterly management review) need a schedule entity that fires automatically and injects a `WorkOrder` into the OrgUnit queues at the right time. Once created by the scheduler, the `WorkOrder` is indistinguishable from an ad-hoc run — the same sequencing service handles it.

**Key entity: `WorkflowSchedule`**

| Field | Type | Purpose |
|---|---|---|
| `WorkflowId` | FK → Workflow | Which workflow to execute on schedule |
| `Name` | string(200) | Human label for this schedule (e.g. "Monthly PCB Final Inspection") |
| `RecurrenceType` | enum | `Daily`, `Weekly`, `Monthly`, `Quarterly`, `Annually` |
| `RecurrenceInterval` | int (default 1) | Every N units (e.g. every 2 weeks) |
| `DayOfWeek` | int? | 0–6, used when RecurrenceType = Weekly |
| `DayOfMonth` | int? | 1–31, used when RecurrenceType = Monthly/Quarterly/Annually |
| `StartDate` | DateOnly | When this schedule becomes active |
| `EndDate` | DateOnly? | When it expires (null = runs indefinitely) |
| `SubjectTemplate` | string(500) | Template for Work Order.Subject, e.g. `"Monthly QC Audit — {Month} {Year}"` |
| `IsActive` | bool | Whether the scheduler should process this record |
| `NextRunAt` | DateTimeOffset? | Computed datetime of the next scheduled fire |
| `LastRunAt` | DateTimeOffset? | When the scheduler last fired this schedule |

**`WorkOrder` gains one field:**
```
Work Order
  + schedule_id  (FK → WorkflowSchedule, nullable)
```
Null for ad-hoc runs; set when the job was created by the scheduler. Allows filtering "all runs of this schedule" and tracking whether a scheduled window was missed.

**Scheduler background service:**
1. Runs on a configurable interval (e.g. every minute)
2. Queries `WorkflowSchedule WHERE is_active = true AND next_run_at <= now`
3. For each due schedule: creates a `WorkOrder` (resolves subject template, sets `ScheduleId`), creates `Job` records for each entry-point `WorkflowProcess` node, pushes assignees from `WorkflowProcess.AssigneeId`
4. Updates `last_run_at = now`, computes and writes `next_run_at` from the recurrence rule
5. Handles missed windows gracefully — if the service was down, it fires once and advances `next_run_at` (no backfill of missed runs)

**Surfaces:**
- `WorkflowScheduleList.razor` — view and manage schedules per workflow
- `WorkflowDetail` — "Add Schedule" action on the workflow
- Schedule calendar view (future) — see all upcoming scheduled runs across all workflows

---

**Schema changes (migration required):**

| Change | Effort |
|---|---|
| New `OrgUnits` table | Small |
| `assignee_id` on `WorkflowProcesses` | Trivial |
| New `WorkOrders` table | Small |
| `workorder_id` + `workflow_process_id` on `Jobs` | Trivial |
| New `WorkflowSchedules` table | Small |
| `schedule_id` on `WorkOrders` | Trivial |
| Sequencing service (job completion hook) | Medium |
| Scheduler background service | Medium |

**Status:** Designed, not yet built.

---

#### 12f — Participant Portal (Execution-Only UI)

When a workflow is deployed for participants who have no business touching the design layer — survey respondents, production operators, onboarding new hires, maintenance technicians completing a PM — they must see *only* the work assigned to them. Every design tool, quality module, analytics dashboard, and admin screen must be invisible and inaccessible to them.

This is the same reason a survey platform shows respondents a clean form and not the survey builder: exposing the back office to participants creates confusion, risk of accidental or deliberate data corruption, and a poor user experience.

**New role: `Participant`**

Added alongside the existing `Admin` and `Engineer` roles:

| Role | Capabilities |
|---|---|
| `Admin` | Full access + user management |
| `Engineer` | Design tools, quality tools, analytics, reports, approval queue |
| `Participant` | Work queue and execution *only* |

A `Participant` role user is typically a member of one or more `OrgUnit`s. Their visible interface is their queue of assigned jobs and the ExecutionWizard.

**What Participants can see:**

| Surface | Access |
|---|---|
| My Work (assigned job queue) | ✅ Full access |
| ExecutionWizard (`/execute/{id}`) | ✅ Full access |
| Job status / step progress (read-only) | ✅ Read only |
| Everything else | ❌ Hidden — route guard returns 403 |

**What Participants cannot see (route-guarded):**
- StepTemplate list/detail and editor
- Process list/detail, Process Builder
- Workflow list/detail and graph editor
- PFMEA, C&E Matrix, Non-Conformances
- Analytics, Reports, Alerts
- Approval Queue
- Admin / User Management
- Kind / Grade management
- Work Order management (they receive work, they don't launch workflow runs)

**Implementation:**

- `NavMenu.razor` conditioned on role — Participant sees only "My Work" and their profile
- All design/admin routes decorated with `[Authorize(Roles = "Admin,Engineer")]`; Participant hitting a guarded route gets a friendly "You don’t have access to this page" screen, not a generic error
- A separate, stripped-down layout (`ParticipantLayout.razor`) removes the full sidebar and replaces it with a minimal header — clean enough to embed in a kiosk device or iframe
- Optional: `/portal` URL prefix as an entry point that forces `ParticipantLayout` regardless of role, suitable for sharing as a QR code link or embedded link in a notification email
- `OrgUnit` membership drives the work queue: a Participant sees jobs assigned to any OrgUnit they belong to, plus jobs directly assigned to them by name

**User management additions:**
- `POST /api/auth/users/{id}/org-units` — assign a user to one or more OrgUnits
- Admin UI: OrgUnit membership picker on user edit form

**Status:** Designed, not yet built.

---

### Phase 13 — Pre-populated Process Content Library

**Goal:** Ship a curated library of ready-to-run processes so that a new customer can begin executing real work without building their process definitions from scratch.

**Design premise:** Everything in this phase is *data*, not code. The process model (Phases 1–5), structured prompts (Phase 2 PromptDefinition), and workflow execution with OrgUnit assignment (Phase 12) must all be built first. Once they are, a library of well-designed content becomes immediately runnable with no additional development.

---

#### Prerequisites

This phase cannot deliver useful content until the following are built:

| Prerequisite | Why |
|---|---|
| Phase 2 PromptDefinition + PromptOption | Structured data collection on steps — without this, processes can't capture meaningful data |
| Phase 12 OrgUnit + WorkflowProcess.AssigneeId | Department routing — without this, multi-department workflows can't be assigned |
| Phase 12 Work Order + sequencing service | Workflow execution — without this, workflows are diagrams, not running operations |
| Phase 12 WorkflowSchedule | Periodic processes (audits, calibrations, reviews) require a schedule trigger |

#### Delivery Mechanism

Content is delivered as a versioned EF Core data seeder (or SQL seed script) that:
- Runs on startup if the library has not yet been seeded (idempotent)
- Tags all seeded records as `is_system_content = true` (a flag to be added to `StepTemplate`, `Process`, and `Workflow`)
- System content can be *copied* by users to create their own variants but cannot be deleted or overwritten by the seeder after initial load
- New library entries are additive — a re-run of the seeder on an existing deployment adds new content without touching existing records

#### Initial Content Areas

**Quality & Compliance**
- Incoming Inspection — supplier material receipt, visual check, dimensional verification, hold vs. accept decision
- Final Inspection — finished-product checklist, customer spec verification, pass/fail/rework routing
- Non-Conformance Handling — log, investigate, disposition (accept/rework/scrap), corrective action, closure
- Calibration Schedule — recurring workflow per instrument type: retrieve, calibrate, record result, label, return
- Internal Audit — scheduled workflow: plan, conduct, record findings, issue CARs, verify closure
- Supplier Audit — annual or triggered: questionnaire, on-site review, score, corrective action if needed

**Operations & Maintenance**
- Preventive Maintenance — recurring workflow per asset: notify, prepare parts, perform PM, record readings, sign off, reschedule
- Customer Complaint Handling — complaint received → acknowledge → investigate → respond → corrective action → close
- Change Request — request → impact assessment → approval → implementation → verification → close

**Management**
- Management Review — annual or quarterly: collect inputs, conduct review meeting, record outputs, assign actions, verify actions closed
- Document Control — draft → review → approve → release → distribute → archive

**Training** *(depends on Phase 16 — seeded after CompetencyRecord is built)*
- Safety Induction — facility rules, emergency exits, PPE requirements, acknowledgment prompts
- Quality System Orientation — quality policy, how to raise NCs, how to use the execution wizard
- Equipment Operation (template) — copy-and-customise per machine: setup, safety checks, operational rules, competency assessment prompts
- Process-Specific Training (template) — copy-and-customise per process: content blocks from the process itself, assessment questions, UserPicker for instructor name

#### `is_system_content` Flag

Added to `StepTemplate`, `Process`, and `Workflow`:

```
is_system_content  boolean  Not Null, Default: false
```

UI behaviour when `is_system_content = true`:
- Edit and Delete buttons replaced with a **"Copy to My Library"** action
- Records displayed in a separate "Library" section / filter
- Seeder will not overwrite on subsequent runs

**Status:** Planned — depends on Phase 12 completion.

---

### Phase 14 — Document Control & QMS

**Goal:** Enable the system to operate as an ISO 9001-compliant Quality Management System. Every controlled document (procedure, work instruction, policy) is a Process. Revision control, formal approval routing, and lineage tracking are first-class features.

---

#### Core design decisions

- **Approval routing is itself a Process.** An approval job executes against an `ApprovalProcess`-role template using the existing step / prompt / execution machinery. No separate approval engine is required.
- **Parallel approvals.** All approvers act simultaneously (all `StepExecution` records share `ParallelGroup = 1`). Any single Reject cancels all remaining open executions, closes the job, and reverts the document to Draft.
- **Binary decision.** Approve or Reject only — no "Approved with Conditions". If a reviewer is unhappy with a minor point they communicate out of band; the formal record is clean.
- **Role-based assignment, editable at submission.** Default assignees flow from step template roles. The author overrides per-step user assignment in the submission dialog before confirming.
- **Formal record via existing prompt machinery.** `ExecutionData` rows produced by the Decision + Comments prompts on each approval step are the permanent approval record. No separate signature table is required.

---

#### New entity: `DocumentApprovalRequest`

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `ProcessId` | Guid | FK → Process — the document being approved |
| `ProcessVersion` | int | The version number being approved |
| `ApprovalJobId` | Guid | FK → Job — the running approval job |
| `Status` | enum | `Pending` / `Approved` / `Rejected` / `Withdrawn` |
| `SubmittedBy` | string | Display name of the submitting author |
| `SubmittedAt` | DateTime | UTC timestamp of submission |

---

#### Schema additions to existing entities

**`Process` — new fields:**

| Field | Type | Notes |
|---|---|---|
| `ProcessRole` | enum | `ManufacturingProcess` \| `ApprovalProcess` \| `QmsDocument` \| `WorkInstruction` — document classification, not execution capability |
| `ApprovalProcessId` | Guid? | FK → Process (an `ApprovalProcess`-role process) that defines the approval routing for this document type |
| `RevisionCode` | string? | Human-readable revision label alongside integer `Version`: "A", "B", "1.0", "Rev 2" |
| `ChangeDescription` | string? | Summary of what changed in this revision; mandatory before submitting for approval |
| `EffectiveDate` | DateTime? | When the released revision becomes effective; defaults to approval timestamp |
| `ParentProcessId` | Guid? | FK to the exact Process row this revision was branched from; enables lineage view across all revisions |

**`StepExecution` — new fields:**

| Field | Type | Notes |
|---|---|---|
| `ParallelGroup` | int | Executions sharing the same group value start simultaneously; all approval steps use group 1 |
| `AssignedToUserId` | string? | Identity user Id; set at job creation time, overridable by the author in the submission dialog |

**`Job` — new field:**

| Field | Type | Notes |
|---|---|---|
| `DocumentApprovalRequestId` | Guid? | Nullable; only populated on approval routing jobs |

---

#### Approval step template content (seeded)

Each approval step template contains two prompts:

| Prompt label | Type | Validation |
|---|---|---|
| Decision | MultipleChoice (Approve / Reject) | Always required |
| Comments | LongText | Required when Decision = Reject |

---

#### Completion hook (`StepExecutionsController`)

On any approval step completion:

1. **Decision = Reject** → cancel all other open `StepExecution` records in the job, close the `Job`, set `DocumentApprovalRequest.Status = Rejected`, revert `Process.Status` to `Draft`.
2. **Decision = Approve and all parallel steps are now complete** → set `DocumentApprovalRequest.Status = Approved`, set `Process.Status = Released`, set `Process.EffectiveDate`, walk the `ParentProcessId` chain to find the previous Released revision in the same lineage and set its status to `Superseded`.

---

#### Submission flow

1. Author opens a document draft and clicks **Submit for Approval**.
2. System reads the linked `ApprovalProcess` template steps.
3. Author sees one user picker per step, pre-filtered to users with the required role — editable.
4. Author confirms `ChangeDescription` is filled (required).
5. Confirm → creates `DocumentApprovalRequest` + `Job` + all `StepExecution` records (`ParallelGroup = 1`) → all assignees see the task in their work queue immediately.

---

#### Admin bootstrap bypass

Admin users retain a direct **Release** action on any Process in `Draft` or `PendingApproval` status. This is required for bootstrapping the QMS — the foundational approval process templates must be released before the self-referential machinery can work. After initial setup, this bypass should be used sparingly and is audited via `ApprovalRecord`.

---

#### Seeded data (`Phase14_Seed` migration)

**Step templates (ApprovalProcess type):**
- "Document Technical Review" — Decision + Comments prompts
- "QE Sign-off" — Decision + Comments prompts
- "Management Authorization" — Decision + Comments prompts

**Process: "Standard Document Approval"**
- `ProcessRole`: `ApprovalProcess`
- Three steps in parallel (`ParallelGroup = 1`): Technical Reviewer, Quality Engineer, Authorizing Manager
- Released via admin bootstrap so it is immediately available for use as an `ApprovalProcessId` on new QMS documents

---

#### Architecture notes

- `ApprovalProcess`-role processes do not appear in the Create Job UI — they are only triggered by the Submit for Approval flow.
- `QmsDocument` and `WorkInstruction`-role processes do not appear in the manufacturing job queue.
- `ManufacturingProcess`-role processes remain exactly as they are today.
- A process can be both executable (as a manufacturing routing) and governed (version-controlled, approval-routed) — `ProcessRole` describes its document classification, not its execution capability.
- The existing `ProcessStatus` lifecycle (`Draft` → `PendingApproval` → `Released` → `Superseded` → `Retired`) introduced in Phase 9 applies unchanged; the Phase 14 machinery *drives* those transitions for QMS documents rather than the manual Approve button on `ApprovalsController`.

**Status:** Designed — depends on Phase 9 (ProcessStatus lifecycle) being complete. ✅ Phase 9 is built.

---

#### Phase 14c — Document Approval as a Work Order (future refactoring, Design Principle #6)

The `DocumentApprovalRequest` entity built in Phase 14 is a bespoke approval infrastructure that partially reimplements capabilities already provided by the process engine. It maintains its own status lifecycle (`Pending` / `Approved` / `Rejected` / `Withdrawn`), its own completion hooks in `StepExecutionsController`, and its own FK on `Job.DocumentApprovalRequestId`. This is exactly the pattern that Design Principle #6 prohibits.

The correct long-term architecture is:

- **Document control lifecycle** is a seeded Workflow: `Draft Submission → Technical Review → QE Sign-off → Management Authorisation → Release`.
- Each review/approval stage is a Process node with one step containing Decision + Comments prompts.
- **Running an approval** creates a Work Order against that workflow, with OrgUnit assignees per `WorkflowProcess.AssigneeId`. The Work Order replaces `DocumentApprovalRequest`.
- **Rejection** at any node follows the existing `Manual` routing link back to a `Rework / Revise` node, creating a new Job for the author to address comments — rather than the bespoke parallel-step cancellation hook that exists today.
- `Process.Status` transitions (`PendingApproval → Released / Draft`) are driven by the Work Order's terminal completion event, exactly as any other Work Order completion hook.
- `Job.DocumentApprovalRequestId` FK is removed; Work Order carries the context.

**Why not done in Phase 14:**  Phase 12 (Work Order + sequencing service) had not yet been designed when Phase 14 was built. Phase 14 delivered a working approval mechanism using the tools available at the time.

**Migration note:** The existing `DocumentApprovalRequest` entity and its machinery remain in place and are not removed until Phase 12 is implemented and this refactoring is completed. The two coexist during the transition.

**Status:** Designed — depends on Phase 12 (Work Order + sequencing service). Supersedes the `DocumentApprovalRequest` entity built in the current Phase 14 implementation.

---

### Phase 15 — Tiered Accountability & Action Tracking

**Goal:** Give every level of the organisation a view of quality and operational data scoped to their responsibility, and provide a unified action item system that captures required work generated by every quality event — non-conformances, MRB decisions, RCA corrective actions, PFMEA actions, audit findings, and management review outputs — with clear ownership, due dates, and completion tracking.

**Design premise:** Quality systems fail not from lack of data but from lack of accountability closure. An NC is found, an investigation is opened, root causes are identified — and then the corrective actions are written in a report that nobody checks. This phase makes action item completion a first-class tracked metric, visible to the level of the organisation that has the authority to drive it to completion. The tiered view design reflects a real organisational truth: operators need to know what they must do today; managers need to know what is overdue across their team; executives need to know whether the quality system is closing at an adequate rate.

---

#### 15a — Unified Action Item System

A single `ActionItem` entity replaces the ad-hoc corrective action tracking scattered across existing phases (PFMEA action fields, NC justification text, RCA node corrective actions). All of those become *source events* that generate `ActionItem` records, which are then tracked to completion centrally.

**Key entity: `ActionItem`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Title` | string | Concise statement of what must be done |
| `Description` | string? | Full detail and context |
| `AssignedToUserId` | string | FK → Identity user |
| `AssignedByUserId` | string | FK → Identity user |
| `DueDate` | DateTime | Required; drives overdue logic |
| `Priority` | enum | `Critical` / `High` / `Medium` / `Low` |
| `Status` | enum | `Open` / `InProgress` / `Complete` / `Verified` / `Cancelled` |
| `SourceEntityType` | enum | `NonConformance` / `MrbReview` / `RcaAnalysis` / `PfmeaAction` / `AuditFinding` / `ManagementReview` / `Manual` |
| `SourceEntityId` | Guid? | FK into the source entity |
| `CompletedBy` | string? | |
| `CompletedAt` | DateTime? | |
| `CompletionNotes` | string? | How the action was resolved |
| `VerifiedBy` | string? | A different person confirms adequate closure |
| `VerifiedAt` | DateTime? | |

**Overdue logic:** An action with `Status = Open` or `InProgress` where `DueDate < today` is treated as overdue. Overdue items are prominently flagged in dashboards at each tier and in the assignee's `MyActions` view. Items are never automatically cancelled — overdue status persists until the assignee records completion.

**Two-step closure:** Completion requires the assignee to record completion (`CompletedAt`, `CompletionNotes`), then a separate verifier confirms adequate closure (`VerifiedAt`). The verifier cannot be the same user as the assignee. This prevents self-certification of corrective action closure — a common ISO 9001 audit finding.

---

#### 15b — Tiered Accountability Views

Four tiers, each scoped to their organisational responsibility:

| Tier | Users | Primary focus | Escalation signal |
|---|---|---|---|
| **Tier 1 — Operator** | Participant | My Work queue, my open action items | My overdue items |
| **Tier 2 — Quality Engineer** | Engineer | Team NCs, open RCAs, open MRBs, PFMEA review status | Actions overdue within their scope |
| **Tier 3 — Quality Manager** | Engineer / Admin | NC frequency by process and part, corrective action closure rates, recurring root causes | Actions overdue across all engineers; MRBs open > 30 days |
| **Tier 4 — Executive** | Admin | Quality scorecard: scrap/rework trends, NC frequency vs. targets, action close rate %, quality system health | Any Tier 3 unresolved escalations; system-wide overdue rate |

No new roles are introduced in v1 — tiers are delivered as purpose-built pages and widgets on top of the existing `Admin`, `Engineer`, `Participant` role hierarchy. Navigation items and page sections show or hide based on the current user's role.

**Key surfaces:**
- `MyActions` page — action items assigned to the current user; grouped by Overdue / Due Soon / Open / Complete awaiting verification; accessible from `MyWork`
- `TeamActions` page (Engineer role) — action items across the engineer's scope with overdue emphasis; filterable by assignee and source type
- `QualityScorecard` page (Admin role) — aggregate quality metrics: NC count by process/part/period, action item close rate %, average days to close, top overdue items by age
- Tier-1 summary widget on `MyWork` — badge count of the current user's open and overdue action items

---

#### 15c — Management Review Support

Formal periodic review at the executive level, satisfying ISO 9001 clause 9.3. **Redesigned as a Work Order** against a seeded "Management Review" workflow, replacing the original bespoke `ManagementReview` entity with the standard scheduling, OrgUnit assignment, execution wizard, and completion record infrastructure.

**Why Work Order rather than a bespoke entity:**
- Management Review is a structured multi-step activity with defined inputs, a meeting phase, and action outputs — exactly the process engine's domain
- The existing `ManagementReview` entity essentially reimplements a subset of Work Order capabilities (status lifecycle, auto-populated inputs, action items)
- Running it as a scheduled Work Order means review runs appear in OrgUnit work queues, can be delegated, and produce permanent execution records via the standard wizard
- Context Data Blocks (Phase 11f-6) replace the auto-populated input panel: the wizard shows live NC count, action item rates, and MRB status as authored context slots in the relevant process steps

**Seeded "Management Review" workflow (ProcessRole.ManagementReview):**

```
WorkflowProcess A: "Collect Review Inputs"  [Parallel — one assignee per step]
  Step: Quality Performance Inputs
    ContextData block: review_period.nc_summary (NC count + trend for configured period)
    ContextData block: review_period.action_close_rate (% closed on time)
    ContextData block: review_period.mbr_open_count (open MRBs + avg age)
    Prompt: NC performance vs target (Select: Acceptable / Concern / Unacceptable)
    Prompt: Notable quality trends (LongText)

  Step: Customer Feedback Inputs
    Prompt: Number of customer complaints (Integer)
    Prompt: Customer satisfaction summary (LongText)

  Step: Supplier Performance Inputs
    Prompt: Number of supplier NCs (Integer)
    Prompt: Supplier performance notes (LongText)

  Step: Internal Audit Status Inputs
    ContextData block: audit.open_findings_count
    Prompt: Audit findings summary (LongText)

  Step: Resource Adequacy Assessment
    Prompt: Are resources (people, equipment, infrastructure) adequate? (Select: Adequate / Concern / Inadequate)
    Prompt: Resource notes (LongText)

  Step: Prior Review Actions Close-out
    ContextData block: prior_management_review.action_items (list of action items from last review + status)
    Prompt: Prior action items review notes (LongText)

WorkflowProcess B: "Conduct Management Review Meeting"
  Step: Review Meeting Execution
    Reference block: guidance on ISO 9001 §9.3 required discussion topics
    Prompt: Quality objectives reviewed and updated (LongText)
    Prompt: Decisions made (LongText)
    Prompt: Performance targets for next period (LongText)
    UserPicker: Management attendees (multi-capture pattern)
    UserPicker: Quality representative present

WorkflowProcess C: "Record and Distribute Outputs"
  Step: Assign action items (operator creates ActionItems from meeting decisions; each becomes an ActionItem with SourceEntityType = ManagementReview)
  Step: Distribute review record (UserPicker: distribution list acknowledgment)
```

**WorkflowSchedule:** A `WorkflowSchedule` record with `RecurrenceType = Quarterly` (or Annual for smaller organisations) drives automatic Work Order creation. The `SubjectTemplate` resolves to `"Management Review — {Quarter} {Year}"`.

**Context:** WorkOrders for management reviews carry `ContextEntityType = ManagementReview` (a lightweight reference entity, or simply the Work Order's own Id) enabling the DashboardScorecard to query all management review runs and their action item close rates.

**Migration note:** The existing `ManagementReview` entity built in Phase 15 remains in the codebase and is not removed — it represents the implementation that exists today and its data is preserved. The Work Order-based design is the *future* execution path once Phase 12 (Work Order + sequencing service) is built. The two approaches coexist: existing records stay in `ManagementReview`; new runs created after Phase 12 is built use the Work Order path.

**Surfaces (Phase 12 dependent):**
- `WorkflowScheduleList` on the Management Review workflow — configure quarterly/annual firing
- MCP tool `get_management_review_status` updated to query Work Order execution data rather than the ManagementReview entity

**Status:** Designed — depends on Phase 12 (Work Order + sequencing) and Phase 11f-6 (Context Data Blocks).

---

### Phase 16 — Training & Competency Management

**Goal:** Make the system the single source of truth for operator and staff competency. Training is delivered as Process execution — the same content, wizard, and data-capture machinery used for manufacturing — and completion automatically generates a durable competency record. The competency record in turn enforces training prerequisites at job assignment time and drives re-training scheduling.

**Design premise:** Training processes are not a special type — they are Processes with a `ProcessRole` of `Training`. Content blocks deliver the material, prompts provide the assessment, a `UserPicker` prompt captures the instructor's identity, and the ExecutionWizard is the delivery interface. The only genuinely new capability is the competency record and the enforcement logic that consults it.

---

#### `ProcessRole` extension

The `ProcessRole` enum introduced in Phase 14 gains a new value:
- `Training` — process is a training course; creates a `CompetencyRecord` on successful completion; does not appear in the manufacturing job queue

---

#### New entity: `CompetencyRecord`

The durable claim that a named user has demonstrated competency in a training topic.

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `UserId` | string | FK → Identity user — the trainee |
| `TrainingProcessId` | Guid | FK → Process (ProcessRole = Training) |
| `TrainingProcessVersion` | int | The exact version completed — pinned at completion |
| `JobId` | Guid | FK → Job — the execution that produced this record |
| `InstructorUserId` | string? | FK → Identity user — captured via UserPicker prompt on the training step; nullable for self-directed training |
| `CompletedAt` | DateTime | UTC timestamp of job sign-off |
| `ExpiresAt` | DateTime? | Null = does not expire; set from `TrainingProcess.CompetencyExpiryDays` at completion time |
| `Status` | enum | `Current` / `Expired` / `Superseded` |
| `Notes` | string? | Any completion notes recorded during sign-off |

`Status` is maintained by a background check (or on-read computation):
- `Current` — `ExpiresAt` is null or in the future
- `Expired` — `ExpiresAt` is in the past and no newer record exists
- `Superseded` — a newer `CompetencyRecord` for the same user + training process exists

**Fields added to `Process`** (Training-role only):
- `CompetencyExpiryDays` int? — if set, `CompetencyRecord.ExpiresAt = CompletedAt + CompetencyExpiryDays`
- `CompetencyTitle` string? — a human-readable competency label distinct from the process title (e.g. "Fork Lift Operator", "CMM Operation")

---

#### New entity: `ProcessTrainingRequirement`

Declares that a `Process` or `StepTemplate` requires the assigned operator to hold a current `CompetencyRecord` in one or more training processes before a job can be started.

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `SubjectEntityType` | enum | `Process` / `StepTemplate` — the entity being gated |
| `SubjectEntityId` | Guid | FK into the respective table |
| `RequiredTrainingProcessId` | Guid | FK → Process (ProcessRole = Training) |
| `IsEnforced` | bool | If true, job creation is blocked when the operator lacks a current record; if false, a warning is shown but execution proceeds |

**Enforcement hook:** On job creation (or step execution assignment), the system checks all `ProcessTrainingRequirement` records for the linked Process and each StepTemplate. For each enforced requirement, it verifies the assigned operator has at least one `CompetencyRecord` with `Status = Current` for that training process. Failure blocks job creation with a clear message listing the missing competencies.

---

#### Competency matrix view

A read-only cross-tab showing, for a selected set of users (or OrgUnit), which training processes each user holds a current record for — and when any current records will expire.

| | Safety Induction | CMM Operation | Fork Lift | CNC Setup |
|---|---|---|---|---|
| Alice | ✅ Current | ✅ Current | — | ⚠️ Expires in 14 days |
| Bob | ✅ Current | — | ✅ Current | ✅ Current |
| Carlos | ⚠️ Expired | ✅ Current | — | — |

This view is the primary tool for a Training Coordinator or Quality Manager to spot gaps before they become audit findings or job-creation blockers.

---

#### Integration with Phase 15 (Tiered Accountability)

- Expired competencies generate `ActionItem` records (`SourceEntityType = CompetencyExpiry`) assigned to the user's line manager or the training coordinator
- `QualityScorecard` (Tier 3) includes a training compliance panel: % operators current on required competencies, number expiring within 30 days, number currently expired
- `ManagementReview` auto-populated inputs include training compliance summary

---

#### Integration with Phase 13 (Content Library)

Seeded training process templates (Safety Induction, Quality System Orientation, equipment and process-specific templates) are delivered via the Phase 13 seeder, but they are only *functional* once `CompetencyRecord` is built. The seeder marks them `is_system_content = true` and `ProcessRole = Training`.

---

#### Surfaces

- `TrainingList` — all Training-role processes; launch training job, view completion history
- `CompetencyRecord` list on user profile — all current and historical competency records for the logged-in user
- `CompetencyMatrix` page (Engineer/Admin) — cross-tab by user and training process; filterable by OrgUnit
- `StepTemplateDetail` / `ProcessDetail` — "Training Requirements" section: list of required competencies with enforce/warn toggle
- ExecutionWizard unchanged — training delivery uses the existing 5-phase wizard with no modifications
- MCP tool `get_competency_status` — for a given user, lists current competencies and any expiring within N days

**Status:** Designed — `ProcessRole` enum (Phase 14 prerequisite) already planned; Phase 12f Participant Portal (OrgUnit membership) clarifies operator identity for enforcement; Phase 13 content library delivers the seeded training templates.

---

### Phase 17 — Internal Audit Management

**Goal:** Run internal quality audits as WorkOrders, capturing audit findings as structured `ExecutionData` on audit steps rather than in bespoke entities. Each finding that warrants a corrective action creates an `ActionItem` with `SourceEntityType = AuditFinding`, linking the action back to the specific step execution that identified the issue.

**Design premise:** An audit is inherently a structured sequence: plan → notify → conduct by clause/area → issue findings → issue corrective actions → verify closure. This maps directly to a Work Order with process-steps. The finding data (clause reference, conformity rating, evidence description) lives in `ExecutionData` on the audit step — no separate `AuditFinding` entity is required. The `AuditFinding` value in `ContextEntityType` / `ActionItem.SourceEntityType` references the `StepExecution.Id` of the finding step, which carries the full finding data.

---

#### Seeded "Internal Audit" Workflow (`ProcessRole.Audit`)

```
WorkflowProcess A: "Audit Planning"  (assigned: Lead Auditor)
  Step 1: Define audit scope and schedule
    Prompt: Audit scope (LongText — clauses, processes, areas)
    Prompt: Planned audit date (DateTime)
    Prompt: Audit standard (Select: ISO 9001:2015 / IATF 16949 / AS9100D / Custom)
    UserPicker: Lead auditor
    UserPicker: Audit team members

  Step 2: Notify auditees and confirm
    Prompt: Auditees notified (Boolean — AcknowledgmentRequired)
    Prompt: Audit plan distributed (Boolean)

WorkflowProcess B: "Conduct Audit"  (assigned: Lead Auditor; one step per audit area)
  [One step per clause/area — authors customise for their scope]
  Example Step: "Clause 8.5 — Production and Service Provision"
    Reference block: Clause 8.5 requirement text
    Prompt: Conformity rating (Select: Conforms / Observation / Minor NC / Major NC / Not Applicable)
    Prompt: Evidence reviewed (LongText)
    Prompt: Finding description (LongText — required when rating != Conforms and != Not Applicable)
    Prompt: Objective evidence reference (String — document number, record ID, photo description)

  [Hard gate: if any step has rating = Major NC, workflow routes to Process D in parallel with C]

WorkflowProcess C: "Close Audit"  (assigned: Lead Auditor)
  Step 1: Prepare audit report
    Prompt: Summary of findings (LongText)
    Prompt: Number of Minor NCs (Integer)
    Prompt: Number of Major NCs (Integer)
    Prompt: Number of Observations (Integer)
    Prompt: Overall audit conclusion (LongText)
  Step 2: Auditee sign-off
    UserPicker: Auditee representative
    Prompt: Auditee acknowledgment (AcknowledgmentRequired Boolean)

WorkflowProcess D: "Issue Corrective Actions"  (parallel branch — fires when any Major NC exists)
  Step 1: Per finding requiring CAPA (one step per Major NC finding)
    ContextData block: audit.open_findings_count (links back to finding ExecutionData)
    Prompt: Corrective action required description (LongText)
    Prompt: Assigned to (UserPicker)
    Prompt: Due date (DateTime)
    [Completion creates: ActionItem with SourceEntityType = AuditFinding, SourceEntityId = finding StepExecution.Id]

WorkflowProcess E: "Verify Corrective Action Closure"  (triggered when all CAPAs in D are Completed+Verified)
  Step 1: Verify effectiveness of each corrective action
    ContextData block: linked ActionItems from process D + completion status
    Prompt: Corrective actions effectively implemented (PassFail)
    Prompt: Verification notes (LongText)
    UserPicker: Verifying auditor
```

**WorkflowSchedule:** Typically Annual (`RecurrenceType = Annually`) or configured per clause/process area. Subject template: `"Internal Audit — {Year} — {scope}"`.

**Finding retrieval:** `GET /api/step-executions?jobId={auditJobId}&promptKey=conformity_rating` returns all audit finding steps with their ratings. The MCP tool `get_audit_summary` queries this pattern.

**`ActionItemSourceType` extension:** `AuditFinding` value already present in the ActionItem design — `SourceEntityId` points to the `StepExecution.Id` of the finding step, from which all finding detail can be retrieved.

**MCP tool:** `get_audit_summary` — for a given audit WorkOrderId, returns finding count by rating, open corrective actions, and verification status.

**Surfaces:**
- Internal Audit runs managed via `WorkOrderList` filtered to the Internal Audit workflow
- Finding summary visible on `WorkOrderDetail` (computed from step execution data)
- Corrective actions appear on `TeamActions` / `MyActions` like all other ActionItems

**Status:** Designed — depends on Phase 12 (Work Order + sequencing), Phase 11f (ContextEntityType + Context Data Blocks), and Phase 15 (ActionItem with AuditFinding source type).

---

### Phase 18 — Supplier Management

**Goal:** Give the quality and procurement teams a first-class `Supplier` entity so that supplier-related jobs (qualification audits, SCAR follow-ups, re-audits) can carry typed context (`ContextEntityType = Supplier`) and supplier performance can be tracked as a first-class metric rather than free-text strings on NC records.

**Design premise:** Suppliers are already referenced throughout the system — as free-text on NC records, as `SupplierCaused` booleans on MRBs, as free-text in management review inputs. Promoting them to a proper entity unlocks context-typed jobs, supplier performance metrics in the QualityScorecard, and the supplier qualification workflow.

---

#### 18a — Supplier Entity

**Key entity: `Supplier`**

| Field | Type | Purpose |
|---|---|---|
| `Code` | string | Short identifier (e.g. "SUP-042", "ACME") |
| `Name` | string | Full trading name |
| `Category` | string? | Type of supply (Raw Material, Sub-assembly, Services, Calibration, etc.) |
| `ApprovalStatus` | enum | `Approved` / `Conditional` / `Suspended` / `Disqualified` |
| `PrimaryContactName` | string? | |
| `PrimaryContactEmail` | string? | |
| `Website` | string? | |
| `Notes` | string? | |
| `IsActive` | bool | |
| `LastAuditDate` | DateTime? | Date of most recently completed qualification audit Job |
| `NextAuditDue` | DateTime? | Computed or manually set |

**NC and MRB linkage:** `NonConformance` and `MrbReview` gain an optional `SupplierId FK → Supplier` (replacing the current `SupplierCaused bool` + free text). Existing records retain the bool flag; new records can link to supplier.

**`ContextEntityType.Supplier`** enables:
- Supplier qualification jobs (`ProcessRole.SupplierAudit`)
- SCAR follow-up jobs with typed link to the supplier
- "Show all jobs for this supplier" tab on SupplierDetail, same pattern as EquipmentDetail

**Job filters:** `GET /api/jobs?contextEntityType=Supplier&contextEntityId={id}` returns all qualification, SCAR, and re-audit jobs for that supplier.

**Surfaces:**
- `SupplierList.razor` — paginated catalog, search/status filter, create modal
- `SupplierDetail.razor` — info card, NC history (filtered by SupplierId), Jobs tab, approval status management
- `GET /api/suppliers` — paginated CRUD
- QualityScorecard: supplier NC count + top suppliers by NC frequency

**Status:** Designed — Phase 11f (`ContextEntityType` pattern) is a prerequisite.

---

#### 18b — Supplier Qualification & Audit Workflow

A seeded workflow (`ProcessRole.SupplierAudit`) that runs as a `WorkOrder` against a `Supplier` context entity.

```
WorkflowProcess A: "Supplier Self-Assessment"
  Step: Self-assessment questionnaire
    Multiple Select / LongText prompts covering: quality system, certifications, capacity, sub-tier controls

WorkflowProcess B: "Desk Review"
  Step: Review documentary evidence
    ContextData block: supplier.open_ncs (existing NCs for this supplier)
    Prompt: Quality cert status (Select: ISO 9001 / IATF / AS9100 / None)
    Prompt: Desk review outcome (Select: Proceed to Site Audit / Conditionally Approved / Rejected — insufficient evidence)

Routes:
  GradeBased: Proceed to Site Audit → WorkflowProcess C
  GradeBased: Conditionally Approved → WorkflowProcess D (skip site audit)
  GradeBased: Rejected → WorkflowProcess E (Rejected notification + close)

WorkflowProcess C: "Site Audit"  (ProcessRole.SupplierAudit)
  Steps: Arrival + scope confirmation → facility walk → process observations →
         documentation review → finding summary
  Prompts: conformity ratings per audit area, finding descriptions (LongText)
  Prompt: Overall score (Integer 0–100)
  Prompt: Audit outcome (Select: Approved / Conditional / Rejected)

WorkflowProcess D: "Record Decision & Notify"
  Step: Update supplier approval status (sets Supplier.ApprovalStatus)
  Step: Notify supplier of outcome (UserPicker: notified by)

WorkflowProcess E: "Issue SCAR" (conditional branch on audit findings)
  Step: Define corrective action requirements
  Creates: ActionItem records with SourceEntityType = Supplier, linked SCAR jobs
```

**Re-audit scheduling:** A `WorkflowSchedule` fires annually (or as configured) to re-qualify active approved suppliers.

**Status:** Designed — depends on Phase 12 (Work Order) and Phase 18a (Supplier entity).

---

### Phase 19 — Product & First Article Inspection

**Goal:** Introduce a `Product` (part number) entity as the top-level context root for product-centric activities — First Article Inspections, engineering change orders, NPI gate reviews — enabling all those activities to carry `ContextEntityType = Product` and appear together on the product's detail page.

**Design premise:** Many activities in a manufacturing company are fundamentally about a *product variant* rather than a *process*: an FAI certifies that a specific revision of a specific part was produced correctly; an ECO routes a change to a specific drawing and its affected processes; an NPI run gates a new product from concept to production release. All of these need a persistent `Product` record to anchor the context entity chain.

---

#### 19a — Product Entity

**Key entity: `Product`**

| Field | Type | Purpose |
|---|---|---|
| `Code` | string | Part number or product code |
| `Name` | string | Human-readable description |
| `Revision` | string? | Current drawing/spec revision (e.g. "Rev C", "2.1") |
| `CustomerPartNumber` | string? | Customer's internal part number if different |
| `Category` | string? | Product family or type |
| `SupplierId` | FK → Supplier? | For purchased/supplied parts |
| `IsActive` | bool | |
| `Notes` | string? | |

**Linkage to existing entities:**
- `Kind` (Phase 1) remains the internal type-system entity (used for port compatibility, grade tracking). `Product` is the customer/commercial-facing identity. A `Product` may reference a `Kind` for process model linkage, or be independent for purely commercial tracking.
- `NonConformance` gains optional `ProductId FK → Product` (replacing free-text part description on NCs)
- `ControlPlanEntry` can optionally link a `ProductId` to ground the characteristic in a specific product revision

**`ContextEntityType.Product`** enables:
- FAI jobs tagged to a specific product
- ECO jobs tagged to the product being changed
- NPI Work Order runs tagged to the new product
- "Show all jobs for this product" on ProductDetail

**Surfaces:**
- `ProductList.razor` — paginated catalog with search/category filter
- `ProductDetail.razor` — info card, linked NCs, Jobs tab (FAI, ECO, NPI), linked processes/processes that produce this product
- `GET /api/products` — paginated CRUD

**Status:** Designed — Phase 11f (`ContextEntityType` pattern) is a prerequisite.

---

#### 19b — First Article Inspection (FAI) Workflow

A First Article Inspection verifies that a new or changed production process can consistently produce a part that meets all engineering and drawing requirements. It is required before first production shipment, after major process changes, and after long production breaks (PPAP / AS9100 compliance).

**Seeded "First Article Inspection" process** (`ProcessRole.Inspection`):

```
Step 1: Drawing & Specification Review
  Reference block: FAI scope and applicable standard (AIAG PPAP / AS9102)
  Prompt: Drawing revision confirmed (Boolean — must be ticked = hard gate)
  Prompt: Applicable standards (LongText)

Step 2: Dimensional Report
  For each characteristic (operators add rows for each drawing dimension):
    Prompt: Characteristic ID (String — drawing callout)
    Prompt: Nominal value (Decimal)
    Prompt: Actual measured value (Decimal — with LSL/USL from ControlPlan if linked)
    Prompt: Pass/Fail (Select)
  Prompt: Measurement system used (String — calibrated gauge ID)

Step 3: Material Certification Review
  Prompt: Material cert number (String)
  Prompt: Material cert conforms to drawing requirement (PassFail)
  Prompt: Material cert attached / on file (Boolean)

Step 4: Process Capability Study (if required)
  Prompt: Cpk value (Decimal)
  Prompt: Sample size (Integer)
  Prompt: Capability adequate (Cpk ≥ 1.33) (PassFail)

Step 5: Functional Test
  Prompt: Functional test standard (String)
  Prompt: All functional tests passed (PassFail)
  Prompt: Any deviations (LongText)

Step 6: Customer Approval & Submission (if customer approval required)
  UserPicker: Submitted to (customer contact name)
  Prompt: Submission date (DateTime)
  Prompt: Customer approval received (PassFail)
  Prompt: PPAP submission level (Select: 1/2/3/4/5)
```

Context: `ContextEntityType = Product` — the FAI Job is linked to the specific product being inspected.

Hard-limit on dimensional checks: any out-of-spec measurement generates a `NonConformance` via the existing Phase 8c disposition flow. The FAI cannot proceed to sign-off with open NCs unless dispositioned UseAsIs with justification.

**Status:** Designed — depends on Phase 19a (Product entity) and Phase 11f (ContextEntityType pattern and checklist/wizard execution).

---

#### 19c — Engineering Change Order (ECO)

An ECO manages the controlled transition of a product from one revision to another — ensuring that all processes, documents, tooling, and suppliers affected by the change are identified, reviewed, and updated before the new revision enters production.

**Seeded "Engineering Change Order" workflow** (`ProcessRole.ChangeControl`):

```
WorkflowProcess A: "Change Request & Impact Assessment"
  Step 1: Define the change
    Prompt: Change description (LongText)
    Prompt: Reason for change (Select: Customer requirement / Quality improvement / Cost reduction / Regulatory / Obsolescence / Other)
    Prompt: Affected drawing / document revisions (LongText)
    Prompt: Affected processes (multi-select from process list — ContextData block)
    ContextData block: product.linked_processes (processes currently producing this product)

  Step 2: Impact Assessment
    Prompt: Does change affect safety or regulatory compliance? (PassFail — if Fail, routes to expedited approval)
    Prompt: Estimated impact on cost/lead time (LongText)
    Prompt: Tooling or equipment changes required (Boolean)
    Prompt: Supplier changes required (Boolean → branches to supplier notification process)

WorkflowProcess B: "Approvals"  [Parallel — same ParallelGroup pattern as Phase 14]
  Step: Design Engineering approval
    Prompt: Decision (Approve / Reject)
    Prompt: Comments (LongText, required on Reject)
  Step: Manufacturing Engineering approval
    Prompt: Decision / Comments
  Step: Quality Engineering approval
    Prompt: Decision / Comments
  [Optional] Step: Customer approval (if contractually required)

WorkflowProcess C: "Implementation"
  Step: Update affected process definitions
    ContextData: list of affected processes identified in Step A
    Prompt: All process revisions submitted for approval (Boolean)
  Step: Update drawings and documents
    Prompt: New revision level (String)
    Prompt: Document control updated (Boolean)
  Step: Tooling / equipment changes (if flagged in impact assessment)
    Prompt: Changes completed (Boolean)
  Step: Training on changed process completed
    Prompt: Operators trained (Boolean — CompetencyRecord enforcement hook if training required)

WorkflowProcess D: "Verification"
  Step: First Article on new revision
    Creates: FAI Job with ContextEntityType = Product (links to Phase 19b)
  Step: Verification sign-off
    Prompt: FAI passed (PassFail)
    Prompt: Production release authorised (Boolean)
    UserPicker: Release authority
```

`ContextEntityType = Product` on the ECO Work Order — all affected processes, FAI results, and supplier notifications are traceable back to the specific product change.

**Status:** Designed — depends on Phase 12 (Work Order), Phase 19a (Product), and Phase 14 (process change control).

---

### Phase 20 — New Product Introduction (NPI)

**Goal:** Use the workflow engine to gate a new product from concept to production release, ensuring that every required phase of development — design review, process design, quality planning (PFMEA/Control Plan), trial production, FAI, and customer approval — is completed and signed off before the product ships. NPI is the most powerful expression of the process engine's capabilities: it orchestrates activities across every other phase of the system.

**Design premise:** NPI is a workflow. Each gate in the NPI process is a WorkflowProcess. The workflow graph enforces the sequence and gates. The evidence produced at each gate (PFMEA, Control Plan, FAI results, approval records) is captured by the existing quality engineering tools, already linked to Process and Product entities. NPI doesn't require new entities — it requires the workflow engine (Phase 12) and the Product entity (Phase 19a) to be in place, then a well-designed workflow definition.

---

#### NPI Gate Workflow (Seeded)

```
Work Order: "NPI — [Product.Code] [Product.Name]"
  ContextEntityType = Product

Gate 0: Concept Review
  Step: Product concept approval
    Prompt: Market requirement / customer commitment (LongText)
    Prompt: Preliminary cost/benefit assessment (LongText)
    Prompt: Proceed decision (Approve / Hold / Cancel)
    UserPicker: Approving authority

Gate 1: Design Review
  Step: Design review completion
    ContextData: product.linked_processes (processes currently planned)
    Prompt: Design FMEA (DFMEA) completed (Boolean)
    Prompt: Drawing revision submitted (String)
    Prompt: Material and component list approved (Boolean)
    Prompt: Design review outcome (Approve / Conditional / Cancel)
    UserPicker: Design review attendees

Gate 2: Process Design
  Step: Process plan completion
    Prompt: Process flow diagram approved (Boolean)
    Prompt: All process definitions created and Released (Boolean — ContextData: list of linked processes + their status)
    Prompt: Manufacturing locations / resources identified (LongText)

Gate 3: Quality Planning
  Step: PFMEA completed
    ContextData: product.pfmea_status (PFMEA for each linked process + RPN summary)
    Prompt: All high-RPN items have action plans (Boolean)
    Prompt: PFMEA approved (PassFail)
  Step: Control Plan completed
    ContextData: product.control_plan_status
    Prompt: Control Plan approved (PassFail)
  Step: Measurement system analysis (MSA) completed
    Prompt: R&R studies done for critical gauges (Boolean)
    Prompt: MSA results acceptable (PassFail)

Gate 4: Trial Production Run
  Creates: Job(s) against the production Process definitions, tagged ContextEntityType = Product
  Step: Trial run completion review
    ContextData: product.trial_run_job_results (cycle times, NC count, yield from trial Jobs)
    Prompt: Yield acceptable (PassFail)
    Prompt: Cycle time within target (PassFail)
    Prompt: Operator feedback (LongText)

Gate 5: First Article Inspection
  Creates: FAI Job (Phase 19b) with ContextEntityType = Product
  Step: FAI results review
    ContextData: product.fai_job_status
    Prompt: FAI passed (PassFail)
    Prompt: Any open NCs dispositioned (Boolean)

Gate 6: Customer Approval / PPAP Submission (if required)
  Step: PPAP package submission
    Prompt: Submission level (Select: 1/2/3/4/5)
    Prompt: Submission date (DateTime)
    UserPicker: Submitted by
  Step: Customer approval received
    Prompt: Approval received (PassFail)
    Prompt: PPAP approval date (DateTime)
    Prompt: Any conditions (LongText)

Gate 7: Production Release
  Step: Production release sign-off
    Prompt: All gate criteria met (Boolean — system validates all prior gates Completed)
    Prompt: Production start date (DateTime)
    UserPicker: Release authority
    Sets: Product.IsReleased = true (new field), Process.Status = Released (if not already)
```

**Key design point:** The "trial production" and FAI gates do not have embedded execution — they *create* child Jobs (using the Quick-create Job modal mechanism from Phase 11f-5) that run in the standard Job engine. The NPI gate step watches for those child jobs to complete before it can be signed off. This cross-job dependency is the most sophisticated pattern in the system and may require a lightweight "blocking dependency" field on StepExecution — or can be enforced manually (the operator checks the result and records it as a prompt response).

**Status:** Planned — depends on Phase 12 (Work Order), Phase 19 (Product + FAI + ECO), Phase 11f (ContextEntityType). Design intent documented; full spec to be written when Phase 12 is built.

---

### Phase 21+ — Integrations (future)

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
- **Process Timing report** — `GET /api/reports/process-timing` with per-process job duration stats (min/avg/median/P95/max in hours) and per-step breakdown (in minutes); `ProcessTimingReport.razor` shows proportional stacked colour bar and collapsible step table with role filter
- **ISO 9001 QMS document seeds** — 21 controlled documents (QMS-001–QMS-021) covering all mandatory ISO 9001:2015 procedures, seeded on startup via `SeedQmsDocumentsAsync`
- **System onboarding training seeds** — 12 training courses (TRN-SYS-001–TRN-SYS-012) covering every system module, seeded via `SeedTrainingDocumentsAsync`; descriptions serve as live user documentation

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

256 integration tests across all phases, all passing. Tests run against an in-memory SQLite database spun up per test run. Test files cover all controllers including Analytics, Alerts, Reports, and the Document Library filters.

### Blazor UI Pages (32 total)

| Page | Features |
|---|---|
| KindList / KindDetail | CRUD, inline grade management, delete confirmations |
| StepTemplateList / StepTemplateDetail | CRUD, port management, run chart widget, pattern/qty rule display, lifecycle status badges + Submit/Approve/Reject/NewRevision buttons |
| ProcessList / ProcessDetail | CRUD, step management, cascading port dropdowns for flows, step editing, validation, lifecycle status badges + Submit/Approve/Reject/NewRevision/Retire buttons |
| ProcessBuilder | Visual drag-and-connect process builder at `/processes/{id}/builder` (Diagram view), slide view (`/processes/{id}/builder?view=slide`), document view; training context routes at `/training/builder` and `/training/{Id:guid}/builder` |
| WorkflowList / WorkflowDetail | CRUD, process/link management, link condition add/remove (grade badges), Validate button |
| JobList / JobDetail | CRUD, lifecycle transitions, step execution navigation, Gantt timeline, CSV export, process version badge, superseded process banner |
| ItemList / ItemDetail | CRUD, filtering by job/kind/status, displays JobName and BatchCode |
| BatchList / BatchDetail | CRUD, item membership management, lifecycle |
| StepExecutionList / StepExecutionDetail | Filter by status and job, port transaction creation, execution data capture, notes |
| VocabularyList | CRUD for domain vocabulary mappings |
| Dashboard | Live KPI cards, job status breakdown, 30-day throughput trend, step performance leaderboard, recent completions |
| Analytics | Ad-hoc time-series chart builder — any numeric prompt, any time window, up to 6 series |
| Alerts | Out-of-range prompt response feed with rolling window filter and CSV export |
| MyWork | Operator-focused view of in-progress step executions assigned to the current user |
| ExecutionWizard | 5-phase guided operator UI at `/execute/{id}`: context → inputs → prompts → outputs → close-out |
| PfmeaList / PfmeaDetail | PFMEA repository; per-process failure mode management; S/O/D/RPN badges with heat-map colouring; action tracking with before/after risk; Branch button for versioning |
| CeMatrixList / CeMatrixDetail | C&E matrix repository; interactive correlation grid; click-to-cycle scores; live priority scores; inline importance editing; CSV export |
| NonConformanceList | Quality non-conformance log; disposition workflow (Accept/Rework/Scrap); filtering by status |
| ApprovalQueue | Pending approval feed for Processes and Step Templates; inline Approve/Reject modals; filterable by entity type and decision |
| Reports | Scheduled and ad-hoc report viewer |
| ProcessTimingReport | Per-process job duration stats (min/avg/median/P95/max); proportional stacked step colour bar; collapsible per-step table; role filter; expand/collapse all — at `/reports/process-timing` |
| Admin / UserList | User management: add users (with Display Name), edit Display Name + Role, delete users |

### Known Limitations / Next Steps

#### Completed phases (all implemented and tested)

- **Phase 8 — Process Maturity & Guided Execution** ✅ ContentCategory enum, NominalValue/IsHardLimit/AcknowledgmentRequired fields, MaturityScoringService (8 rules), maturity badges across list/detail views, NonConformance entity + disposition workflow, 5-phase ExecutionWizard at `/execute/{id}`
- **Phase 9 — Process Change Control & Approval** ✅ ProcessStatus lifecycle (Draft→PendingApproval→Released→Superseded→Retired), ApprovalRecord entity, PFMEA staleness tracking, job-level process version pinning, Submit/Approve/Reject/NewRevision/Retire endpoints, ApprovalQueue page, status badges across all list/detail views, NavMenu pending badge
- **Phase 7c — Control Plan Builder** ✅ ControlPlan + ControlPlanEntry entities, CharacteristicType enum, ControlPlansController (CRUD + entries + CSV export + staleness), EF migration, ControlPlanList/Detail Blazor pages, staleness integration with ProcessesController.Approve, MCP tools `get_control_plan`/`list_critical_characteristics`
- **Phase 10 — Root Cause Analysis & Material Review** ✅ Phase 10a: RootCauseLibrary with 7M categories, typeahead search, UsageCount tracking, MCP tool. Phase 10b+c: Ishikawa diagrams (fishbone card grid, per-category causes, library typeahead) and branching 5 Whys (recursive tree, incomplete-leaf warning, RCA open/close). Phase 10d: MrbReview + MrbParticipant entities, NC escalation, SCAR flag, RCA linkage gate, MrbList/Detail pages, MCP tool
- **Phase 14 — Document Control & QMS** ✅ ProcessRole enum (5 values), DocumentApprovalRequest entity, revision metadata on Process (RevisionCode, ChangeDescription, EffectiveDate, ParentProcessId, ApprovalProcessId), approval-as-process architecture, DocumentList page with submit-for-approval + admin-release modals, Document Library nav section, MCP `list_qms_documents` tool ⚠️ *Phase 14c (future): `DocumentApprovalRequest` to be replaced by a Work Order against a seeded Document Control Workflow once Phase 12 is built — the bespoke approval entity is inconsistent with Design Principle #6*
- **Phase 15 — Tiered Accountability & Action Tracking** ✅ ActionItem entity (two-step close/verify, anti-self-certification), ManagementReview entity (ISO 9001 clause 9.3, auto-populated snapshot inputs), MyActions/TeamActions/QualityScorecard pages, ManagementReviewList/Detail pages, Accountability nav section with overdue badge, MCP `get_management_review_status` tool
- **Phase 16 — Training & Competency Management** ✅ ProcessRole.Training, CompetencyRecord + ProcessTrainingRequirement entities, CompetencyExpiryDays/CompetencyTitle on Process, job-creation enforcement hook, competency matrix view, TrainingList page (launch modal), CompetencyMatrix page, training compliance in QualityScorecard and ManagementReview snapshot, MCP `get_competency_status` tool
- **Phase 2 enhancement — `LongText` and `UserPicker` prompt types** ✅ Both added to PromptType enum; UserPicker renders as Identity-backed user dropdown in ExecutionWizard (stores display name), used for instructor capture, witness, and handoff signatory; LongText renders as textarea for multi-line instructions
- **Phase 13 (partial) — Seeded content library** ✅ 21 ISO 9001:2015 QMS documents (QMS-001–QMS-021) and 12 system onboarding training courses (TRN-SYS-001–TRN-SYS-012) seeded with full step content; all served as live user documentation via the Document Library and Training Catalogue
- **Phase 11 — Production Management (initial build)** ✅ EquipmentCategory + Equipment entities (catalog with location, manufacturer, model, serial); DowntimeRecord (Planned/Unplanned, open/close with resolver); MaintenanceTrigger (time/usage-based, advance notice, auto-advance NextDueAt on task completion); MaintenanceTask ⚠️ *(initial implementation — to be superseded by Phase 11f Jobs-based design)*; StepTemplate.ExpectedDurationMinutes + RequiredEquipmentCategoryId; Job.DueDate + PlannedStartDate; StepExecution.EquipmentId; Phase11_ProductionManagement migration; EquipmentController + ProductionController (WIP board, bottlenecks); 4 Blazor pages (ProductionDashboard, EquipmentList, EquipmentDetail, MaintenanceTaskList); Production NavMenu section; 3 MCP tools (get_production_status, list_equipment_downtime, list_overdue_maintenance); MCP v2.1

#### Not yet built
- **Phase 14c — Document Approval as a Work Order** — replace `DocumentApprovalRequest` entity with a Work Order against a seeded "Document Control" Workflow; `DocumentApprovalRequest` entity + `Job.DocumentApprovalRequestId` FK removed; rejection routing via `Manual` WorkflowLink back to a Revise node (replaces bespoke parallel-step cancellation hook); `Process.Status` transitions driven by Work Order terminal completion event; depends on Phase 12 — `Job.ContextEntityType` + `ContextEntityId` (polymorphic context pattern: Equipment/NonConformance/MrbReview/Supplier/Product/ManagementReview/AuditFinding); `ProcessRole.Maintenance` enum value; `Process.ExecutionMode` (Auto/Wizard/Checklist) with checklist view at `/execute/{id}/checklist`; `Job.ProcessId` nullable (free-form jobs); `Equipment.DefaultMaintenanceProcessId`; `DowntimeRecord.LinkedJobId`; `MaintenanceTrigger.ProcessId` (replaces `Title` string); Context Data Blocks (`ContentBlockType.ContextData`, `ContextDataKey` enum, automatic context panel per entity type, `GET /api/context-data/{key}` endpoint); `Phase11f_ProcessEngineEnhancements` EF migration; Quick-create Job modal component; refactored Phase 11d (triggers create Jobs, not MaintenanceTasks); remove MaintenanceTask entity + related endpoints + MaintenanceTaskList.razor
- **Phase 12 — Workflow Execution & Department Assignment** — OrgUnit entity (department/work area/role/person); assignee field on WorkflowProcess nodes; Work Order execution record that tracks the active workflow node; sequencing service that advances the workflow graph on job completion; WorkflowSchedule entity for periodic recurrence with background scheduler service
- **Phase 12f — Participant Portal** — `Participant` role with access scoped to My Work + ExecutionWizard only; all design, admin, and quality engineering routes hidden and route-guarded; stripped `ParticipantLayout` with minimal navigation; optional `/portal` URL entry point; OrgUnit membership on users driving queue assignment
- **Phase 13 (remaining) — System content flag + "Copy to My Library"** — `IsSystemContent` flag on Process and StepTemplate to distinguish seeded content from user-created records; "Copy to My Library" action that clones a system process under the user's own code prefix; protects system records from accidental deletion or edit
- **Phase 17 — Internal Audit Management** — seeded "Internal Audit" workflow (`ProcessRole.Audit`): planning → notify → conduct by clause (conformity rating + evidence prompts) → close audit → issue corrective actions (creates ActionItems with `SourceEntityType = AuditFinding`, `SourceEntityId = StepExecution.Id`) → verify closure; `WorkflowSchedule` for annual/periodic firing; MCP `get_audit_summary` tool; depends on Phase 12 + Phase 11f + Phase 15
- **Phase 18 — Supplier Management** — `Supplier` entity (Code/Name/ApprovalStatus/contact fields); `ProcessRole.SupplierAudit`; seeded supplier qualification workflow (self-assessment → desk review → site audit → decision → SCAR branch); `WorkflowSchedule` for periodic re-qualification; `NonConformance.SupplierId` + `MrbReview.SupplierId` FKs; `ContextEntityType.Supplier`; SupplierList/Detail Blazor pages; depends on Phase 12 + Phase 11f
- **Phase 19 — Product & First Article Inspection** — `Product` entity (Code/Name/Revision/CustomerPartNumber/SupplierId); `ContextEntityType.Product`; seeded FAI process (`ProcessRole.Inspection`, 6 steps with dimensional/material/functional prompts, hard-limit NC integration); seeded ECO workflow (`ProcessRole.ChangeControl`, change request → impact assessment → parallel approvals → implementation → FAI verification → release); `Product.IsReleased`; ProductList/Detail Blazor pages; depends on Phase 11f + Phase 14
- **Phase 20 — New Product Introduction (NPI)** — seeded NPI workflow (`ProcessRole.NPI`): 7-gate Work Order (Concept → Design Review → Process Design → Quality Planning → Trial Production → FAI → Production Release); each gate is a WorkflowProcess with Context Data blocks showing live quality planning status; `Product.IsReleased` set on Gate 7 completion; depends on Phase 12 + Phase 17 + Phase 18 + Phase 19
- **Phase 2 enhancement — `UserPicker` stores user Id** — current implementation stores display name as a plain string; a future enhancement would store the ASP.NET Identity user Id as the value and resolve the display name at render time, enabling joins to competency and accountability records

#### Ongoing limitations

- Multi-tenancy deferred until a second SaaS tenant is onboarded (database-per-tenant approach selected — see Architecture Decision above)
- Email/webhook notifications for out-of-range alerts not yet implemented
- MCP server uses short-lived JWT tokens; a long-lived API-key auth path would improve service-account ergonomics for AI integrations
