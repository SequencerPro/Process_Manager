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
| 3.18    | 2026-03-23 | Phase 2 enhancement implemented: `UserPicker` now stores ASP.NET Identity user Id (not display name) as `ResponseValue`; `ResolvedDisplayName` nullable field added to `PromptResponseDto`; `GetPromptResponses` in `StepExecutionsController` batch-resolves user Ids to display names via `_db.Users` lookup; `ExecutionWizardContent.razor` option value changed from `user.DisplayName` to `user.Id`; `StepExecutionDetail.razor` gains `_userPickerList` + dropdown (replaces text input), with legacy display-name fallback for pre-existing data; `PromptType.UserPicker` XML docs updated |
| 3.17    | 2026-03-23 | Phase 13 (remaining) implemented: `IsSystemContent` bool on `Process` and `StepTemplate`; `Phase13_SystemContent` EF migration; DataSeeder marks QMS docs, training courses, DOC-SECT-01, TRN-MOD-01 as system content; ProcessesController + StepTemplatesController Update/Delete guarded (400 for system content); `POST /api/processes/{id}/copy` deep-clone endpoint (steps, port overrides, content blocks, flows with ID remapping; target is Draft, not system content); `ProcessCopyDto`; `CopyProcessToMyLibraryAsync` in ApiClient; response DTOs extended with `IsSystemContent`; ProcessList "Library" badge + "Copy to My Library" modal; ProcessDetail "System Content" badge hiding edit/lifecycle/delete; StepTemplateList "Library" badge + lock icon |
| 3.16    | 2026-03-23 | Phase 12f implemented: Participant Portal — `Participant` role (existing in AuthController); `ParticipantLayout.razor` + `ParticipantNavMenu.razor` (minimal sidebar, My Work only); `Portal.razor` (/portal redirect), `PortalMyWork.razor` (/portal/my-work), `PortalExecutionWizard.razor` (/portal/execute/{id}); `RedirectToPortal.razor`; Routes.razor NotAuthorized block redirects Participant role to `/portal/my-work` instead of showing 403; all design/admin pages carry `[Authorize(Roles = "Admin,Engineer")]`; NavMenu design/admin sections wrapped in `<AuthorizeView Roles="Admin,Engineer">`; UserList Edit modal extended with OrgUnit Memberships section — loads current memberships via `GET /api/users/{id}/orgunits`, add via `POST /api/orgunits/{id}/members`, remove via `DELETE /api/orgunits/{id}/members/{memberId}` |
| 3.15    | 2026-03-23 | Phase 19 design: Warehouse Management — `StorageLocation` entity (zone/aisle/bay/bin hierarchy), `InventoryTransaction` entity (Receipt/Issue/Transfer/Adjustment/Picklist types), `PickList` + `PickListLine` entities, inventory-on-hand view, job-creation picklist generation from BOM/process inputs, ExecutionWizard consumption hook, `WarehouseManagement` nav tab, MCP `get_inventory_status` tool |
| 3.24    | 2026-04-11 | Phase 22 design: Factory Design Suite — `FloorPlan` entity with JSON-serialised layout document (rooms, workstations, inventory locations, utility lines, annotations); `FloorPlanWorkstation` junction linking visual placements to Equipment/OrgUnit/StorageLocation with assigned Processes and tool Kinds; `FloorPlanInventoryLocation` junction to StorageLocation; material-flow analysis engine (Euclidean distance from workstation process input ports to nearest stocked inventory location); HTML5 Canvas editor via `factory-canvas.js` JS interop (drag-and-drop palette, grid snap, zoom/pan, undo/redo, resize handles, polyline utility drawing); properties panel with Equipment/Process/Kind/StorageLocation pickers; animated flow-arrow overlay; `FloorPlanController` (CRUD + layout save + publish/archive); `FloorPlanWorkstationController` (process/tool management); `get_floor_plan_summary` MCP tool; 10 implementation steps |
| 3.32    | 2026-04-22 | **M3 Execution Wizard Polish complete**: `operator-sw.js` service worker (static asset caching with network-first/cache-fallback strategy, cache versioning, activate cleanup); `operator-sync.js` IndexedDB offline queue (`ProcessManagerOfflineQueue` DB, `pendingResponses` object store with stepExecutionId+queuedAt indexes, enqueue/getPendingCount/getAllPending/removeBatch/flushQueue methods, grouped batch submission by stepExecutionId via `POST /api/step-executions/{id}/prompt-responses/batch`, sync-on-reconnect via `online` event listener, DotNetObjectReference status callback); `SyncStatus.razor` shared component (online/offline/pending/syncing states with color-coded pill in wizard header, JSInvokable `OnSyncStatusChanged` callback); `barcode-scanner.js` + `BarcodeScannerModal.razor` (native BarcodeDetector API with 8 format support, camera stream via `getUserMedia` with environment-facing preference, `requestAnimationFrame` detection loop, keyboard-wedge manual entry fallback with Enter key submit, camera cleanup on close); `photo-capture.js` + `PhotoCapture.razor` (file input with `capture="environment"`, Canvas-based JPEG compression with configurable maxWidth/maxHeight/quality, size reporting, preview with remove button); `signature-pad.js` + `SignatureCanvas.razor` (pointer-event drawing, quadratic Bezier curve interpolation for smooth strokes, multi-stroke support, clear/isEmpty/toDataUrl/toBase64/loadFromDataUrl/destroy methods, IAsyncDisposable cleanup); `ExecutionWizardContent.razor` updated: SyncStatus in header, Scan prompt gains camera button → BarcodeScannerModal, Phase 5 sign-off gains PhotoCapture + SignatureCanvas before notes, OperatorSync API base set on init; `App.razor` updated with JS script tags + service worker registration; `app.css` extended: signature canvas responsive sizing, sync-status pill, touch-target rules for photo/signature; 11 new `OfflineQueueTests` (sequential batch flush order, ClientId idempotency with 5 retries, Scan prompt accepts barcode text, TextEntry accepts keyboard-wedge, large 10-item batch, mixed offline/online merge, service worker file exists, operator-sync.js file exists, barcode-scanner.js file exists, photo-capture.js file exists, signature-pad.js with quadraticCurveTo file exists); full test suite 569 tests all green |
| 3.31    | 2026-04-22 | **M5 Billing Infrastructure implemented**: Stripe.net 46.0.0 NuGet package; `TenantSubscription` entity (StripeCustomerId, StripeSubscriptionId, PlanCode enum Trial/Starter/Professional/Enterprise, Status enum Trial/Active/PastDue/Suspended/Cancelled, TrialEndsAt, CurrentPeriodEnd, GraceEndsAt, FailedPaymentCount, LastStripeEventId) with unique-per-tenant index; `UsageMetric` entity (MetricType enum JobExecutions/PdfExports/ApiCalls/ActiveUsers, Count, PeriodStart, PeriodEnd) with unique composite index (TenantId+MetricType+PeriodStart); `BillingEvent` audit entity (StripeEventId unique, EventType enum 10 values, Description, RawPayload, ProcessedAt); `Phase_MVP05_BillingInfrastructure` EF migration; `IStripeService` abstraction (CreateCustomerAsync, CreateTrialSubscriptionAsync, CreateBillingPortalSessionAsync, CancelSubscriptionAsync, VerifyWebhookSignature); `StripeService` implementation using Stripe.net SDK; `BillingController` (Admin-only: `GET /api/billing` dashboard with subscription+usage+events, `GET /api/billing/subscription`, `POST /api/billing/portal-session` creates Stripe billing portal redirect, `GET /api/billing/usage` with date range filter, `GET /api/billing/events` with limit); `StripeWebhookController` (`POST /api/platform/stripe-webhook` AllowAnonymous, signature verification via IStripeService, idempotent processing by StripeEventId, handles invoice.payment_succeeded → reactivates subscription + clears failed count + unsuspends tenant, invoice.payment_failed → PastDue + grace period + auto-suspend after grace expires, customer.subscription.deleted → cancelled + suspended, customer.subscription.updated → audit); `TenantSuspensionMiddleware` (returns 402 Payment Required for suspended tenants on all /api/* endpoints except /api/billing, /api/auth/logout, /api/auth/me, /api/platform/stripe-webhook, /api/public/*, /health; platform admins bypass); `PublicSignupController.Signup` extended to create `TenantSubscription` with 30-day trial on every new tenant; `BillingDtos` (TenantSubscriptionDto, UpdateSubscriptionPlanDto, UsageMetricDto, BillingEventDto, BillingDashboardDto, CreatePortalSessionDto, PortalSessionResultDto); `TestStripeService` stub for integration tests (configurable signature verification, event ID/type); `TestWebApplicationFactory` updated to swap IStripeService with TestStripeService; 14 new `BillingTests` (signup creates trial subscription, billing dashboard returns data, subscription CRUD, 404 on missing subscription, webhook invalid signature rejected, webhook duplicate event idempotent, webhook payment succeeded reactivates, webhook payment failed sets PastDue+grace, suspended tenant blocked 402 on API, suspended tenant can access billing, usage metrics queryable, billing events list, billing requires auth, cross-tenant billing isolation); full test suite 558 tests (557 green, 1 pre-existing flake in WorkflowTests) |
| 3.26    | 2026-04-20 | MVP work stream planned: `docs/mvp-market-analysis.md` defines market need, MVP scope (5 pillars), and 9 customer segments; `docs/mvp-implementation-plan.md` sequences 5 MVP phases (M1 Multi-Tenant Isolation → M2 Onboarding Wizard → M3 Execution Wizard Polish → M4 PFMEA/Control Plan PDF Export → M5 Billing Infrastructure) with task breakdown, test requirements (80+ new tests planned across `MultiTenancyTests`, `OnboardingTests`, `ExecutionWizardUxTests`, `PdfExportTests`, `BillingTests`), ~6-week single-developer timeline, and progress tracker; no code changes yet — planning artifact only |
| 3.27    | 2026-04-20 | **M1 Multi-Tenant Isolation implemented**: `Tenant` entity (Id/Subdomain/Name/Status/CreatedAt/UpdatedAt) with `TenantStatus` enum (Trial/Active/Suspended/Archived) and sentinel `DefaultTenantId` for backfill; `TenantId` column on `BaseEntity` → stamped on all 74 domain entities; `ApplicationUser.TenantId`/`IsPlatformAdmin`; `ITenantContext`/`TenantContext` scoped service with `BeginScope` pattern for background work; `TenantSaveChangesInterceptor` stamps inserts + blocks cross-tenant updates (defence-in-depth); EF global query filter applied via reflection to every BaseEntity-derived type; `TenantContextMiddleware` reads `tenant_id`/`platform_admin` claims from JWT; `AuthController.GenerateJwt` issues tenant_id claim; `PlatformTenantsController` (`GET/POST /api/platform/tenants`, `PATCH /api/platform/tenants/{id}/status`) gated by `PlatformAdminPolicy`; `Phase_MVP01_MultiTenancy` EF migration (backfills existing rows to `DefaultTenantId`, creates `Tenants` table, adds `TenantId`/`IsPlatformAdmin` to AspNetUsers); `TestWebApplicationFactory` extended with `CreateTenant`/`CreateTenantClient`/`CreatePlatformAdminClient` and default-tenant seeding; 14 new `MultiTenancyTests` (JWT claim shape, cross-tenant read/write isolation, interceptor stamping, TenantId immutability on update, interceptor defence-in-depth, platform-admin endpoint gating, tenant provisioning, duplicate subdomain rejection, status updates); full test suite green (508 tests) |
| 3.30    | 2026-04-22 | **M4 PFMEA & Control Plan PDF Export implemented**: QuestPDF 2024.12.3 NuGet package (Community license); `TenantBranding` entity (LogoFileName, PrimaryColorHex, CompanyName, FooterText) with unique-per-tenant index; `Phase_MVP04_PdfExport` EF migration; `PfmeaPdfGenerator` static service (AIAG-4 column order: Function/Failure Mode/Effect/S/Cause/O/Prevention/Detection/D/RPN/Recommended Action/Responsible/Target Date/Actions Taken/New O/New D/New RPN; landscape A4; alternating row shading; RPN color coding red≥200/amber≥100/green; multi-action row expansion; DRAFT warning banner when IsStale); `ControlPlanPdfGenerator` static service (AIAG column order: #/Operation/Characteristic/Type/Spec/Measurement/Sample Size/Freq/Control Method/Reaction Plan/PFMEA Ref/Port; landscape A4; DRAFT banner); `GET /api/pfmeas/{id}/pdf` endpoint on PfmeasController; `GET /api/controlplans/{id}/pdf` endpoint on ControlPlansController; `TenantBrandingController` (Admin-only: `GET /api/tenant-branding`, `PUT` upsert CompanyName/PrimaryColorHex/FooterText, `POST /api/tenant-branding/logo` upload with 2 MB limit + PNG/JPG/SVG validation + old-file cleanup, `DELETE /api/tenant-branding/logo`); `TenantBrandingDtos` (TenantBrandingResponseDto, UpdateTenantBrandingDto); 12 new `PdfExportTests` (PFMEA PDF valid %PDF magic bytes + content-type, PFMEA contains all failure modes via PdfPig text extraction, PFMEA contains computed RPN values, PFMEA non-stale omits DRAFT banner, PFMEA 404 on nonexistent ID, Control Plan PDF valid, Control Plan contains entries, Control Plan 404, branding upsert round-trip, PDF reflects tenant branding, PDF requires authentication, cross-tenant PDF isolation returns 404); full test suite green (544 tests) |
| 3.29    | 2026-04-21 | **M3 Execution Wizard Polish (partial)**: `PromptResponse.ClientId` nullable field for offline-sync idempotency; `Phase_MVP03_ExecutionWizardPolish` EF migration; `POST /api/step-executions/{id}/prompt-responses/batch` idempotent batch endpoint (skips already-saved ClientIds, validates numeric ranges, upserts by content block); `BatchPromptResponsesDto`/`BatchPromptResponseItemDto` DTOs; `BatchPromptResponsesAsync` ApiClient method; `HoldConfirmButton.razor` shared component (pointer-event hold-to-confirm with CSS fill animation, configurable duration, cancellation on pointer leave); Complete Step button replaced with HoldConfirmButton in sign-off phase; `execution-wizard` CSS class wrapper with `@media (pointer: coarse)` touch-target rules (44px min interactive elements, 52px buttons, enlarged checkboxes/inputs); `beforeunload` JS interop guard for unsaved prompt changes; phase-navigation confirmation dialog when leaving execution phase with dirty prompts; `_dirtyPrompts` HashSet tracking modified-since-save state; 8 new `ExecutionWizardUxTests` (batch idempotency on retry, numeric limit validation, multi-item batch, content block reference validation, 404 on nonexistent step, empty batch, duplicate ClientId deduplication, in-range value not flagged); full test suite green (532 tests) |
| 3.28    | 2026-04-21 | **M2 Onboarding Wizard implemented**: `TenantOnboardingState` entity (Industry/CurrentStep/CompletedAt/SkippedAt/FirstKindId/FirstStepTemplateId/FirstProcessId/FirstJobId/SignupAt/FirstJobCompletedAt) + `OnboardingIndustry` enum (General/CNC/PCBA/Medical); `TenantFeatureFlags` entity (ShowAdvancedModules/ShowQualityTools/ShowProductionTools/ShowWarehouseTools/ShowTrainingTools); `Phase_MVP02_Onboarding` EF migration with unique-per-tenant indexes; JWT generation extracted to `JwtTokenService` (`Generate(user, role)` issues tenant_id, platform_admin, display_name claims) — `AuthController` refactored to inject it; `PublicSignupController` (`POST /api/public/signup` — `[AllowAnonymous]`, subdomain regex + duplicate checks, creates Tenant/Admin user/feature flags/onboarding state/industry-specific DomainVocabulary in one atomic op wrapped in `_tenantContext.BeginScope`, rolls back on user-creation failure, returns JWT for immediate login); `OnboardingController` (`GET /api/onboarding/industries` anonymous, `GET /state` lazy-creates legacy tenants as already-completed, `PATCH /state` clamps 0..5 with step-5-finishes rule, `POST /skip` with optional SeedSample, `POST /seed-sample`, `GET /feature-flags` lazy-creates with all-modules-on, `PUT /feature-flags` Admin-only); `DataSeeder.SeedSampleProcessAsync` creates industry-specific Kind+2 Grades+StepTemplate (Transform pattern, input Material/output Material port, numeric prompt, Setup block)+Process+ProcessStep (Released, RevisionCode A, tenant-ID suffixed codes for re-runnability); `OnboardingDtos` (PublicSignupDto/PublicSignupResultDto, OnboardingStateDto, UpdateOnboardingStepDto, SkipOnboardingDto, OnboardingIndustryOptionDto, TenantFeatureFlagsDto); ApiClient M2 section (7 methods: GetOnboardingIndustries/State/UpdateState/Skip/SeedSample/GetFeatureFlags/UpdateFeatureFlags); Blazor: `Signup.razor` (`/account/signup` — EmptyLayout, 4-industry picker, company/subdomain/admin form, posts to /api/public/signup and auto-signs-in via cookie), `OnboardingWizard.razor` (`/onboarding` — 5-step wizard with progress bar, Next/Back/Skip, seeds sample on skip, bounces finished tenants to /), `Settings/Modules.razor` (`/settings/modules` — Admin-only toggle UI using mutable local FlagsModel); `FeatureFlagService` scoped-per-circuit service loaded in MainLayout alongside VocabularyService (safe defaults all-true until loaded); `ModuleToggle.razor` shared component; NavMenu now gates Quality/Production/Warehouse/Training/Reports sections on feature flags, subscribes to FeatureFlagService.OnChange for live re-render after save, adds Admin → Modules entry; Login page links to Signup; 16 new `OnboardingTests` (signup happy path creates tenant/flags/state/vocab, duplicate-subdomain 409, invalid-subdomain 400, weak-password 400, signup is anonymous, legacy-tenant lazy state is completed, PATCH advances step, step 5 marks completed, invalid step 400, skip seeds sample and marks completed, seed-sample is industry-specific, legacy flags default all-true, feature-flags round-trip persists, non-admin flags PUT is forbidden, industries endpoint anonymous and returns all four, onboarding state isolated between tenants); full test suite green (524 tests) |
| 3.40    | 2026-05-05 | **Phase 26 Measurement System Analysis (MSA/GR&R) implemented**: `GageStudy` entity (Name, StudyType enum GRR_Range/GRR_ANOVA/Linearity/Bias/Stability, EquipmentId FK → Equipment, ProcessId FK → Process, CharacteristicName, Tolerance, LSL, USL, NumberOfParts, NumberOfOperators, NumberOfTrials, Status enum Draft/InProgress/Complete, GrrPercent, Ndc, AcceptanceDecision); `GageStudyMeasurement` entity (GageStudyId, PartNumber, OperatorId, TrialNumber, MeasuredValue with unique composite index); `GageStudyType`/`GageStudyStatus` enums; `GrrCalculationService` (Range method: d₂/K1/K2/K3 constants for subgroup sizes 2–10, R̄ computation, EV/AV/GRR/PV/TV decomposition, %GRR of total variation, %GRR of tolerance, ndc calculation, three-tier assessment Acceptable<10%/Marginal<30%/Unacceptable); `GageStudyController` (paginated list with status/equipment filters, CRUD, bulk measurement add with bounds validation, auto Draft→InProgress on first measurement, calculate endpoint with study completion and result storage, dashboard with acceptance breakdown and worst-performing studies); `Phase26Dtos` (GageStudyResponseDto/SummaryDto, CreateGageStudyDto/UpdateGageStudyDto, GageStudyMeasurementDto, AddGageStudyMeasurementsDto/MeasurementItemDto, GrrCalculationResultDto, GageStudyDashboardDto); NavMenu Gage Studies link under Quality section; `_sectionPaths` updated; MCP `get_msa_status` tool (total/complete/draft/in-progress counts, acceptance breakdown, worst performing studies table); MCP server version 3.6 with 35 tools total; 23 new `GageStudyTests` (CRUD + invalid type + invalid equipment + get by ID + not found 404, update fields, delete, paginated list, filter by status, measurements add transitions to InProgress, invalid part/trial bounds, get measurements list, calculate with full data, calculate sets complete, insufficient data 400, tolerance percent returned, update completed blocked, add to completed blocked, dashboard aggregates, auth required, cross-tenant isolation, MCP get_msa_status returns data); full test suite 782 tests (781 green, 1 pre-existing flake in UpgradeFlowTests) |
| 3.39    | 2026-05-05 | **Phase 28 Calibration Management implemented**: `CalibrationRecord` entity (EquipmentId FK → Equipment, CalibrationType enum Internal/External/Vendor, CalibrationDate, NextDueDate, CertificateNumber, CertificateFileName, Result enum Pass/Fail/Limited, PerformedBy, StandardsUsed, TemperatureHumidity, AsFoundReading, AsLeftReading, Uncertainty, Notes); `CalibrationSchedule` entity (EquipmentId unique, IntervalDays, IntervalAdjustmentMethod enum Fixed/ReliabilityBased, ConsecutivePassCount, MaxIntervalDays, MinIntervalDays, ExtensionPercent); reliability-based interval adjustment (consecutive passes extend interval, fail resets to minimum); `CalibrationResult`/`CalibrationType`/`IntervalAdjustmentMethod` enums; `Phase28_CalibrationManagement` EF migration; `CalibrationController` (records CRUD with paginated list/equipment/result filters, equipment calibration history, schedules CRUD with active-only filter + duplicate-equipment guard + min/max validation, dashboard with due/overdue recall detection from active schedules); `Phase28Dtos` (CalibrationRecordResponseDto/SummaryDto, CreateCalibrationRecordDto/UpdateCalibrationRecordDto, CalibrationScheduleResponseDto, CreateCalibrationScheduleDto/UpdateCalibrationScheduleDto, CalibrationDashboardDto/CalibrationRecallDto); NavMenu Calibration link under Production section; MCP `get_calibration_status` tool (record stats, active schedules, overdue/due-within-30-days recall tables); MCP server version 3.5 with 34 tools; 26 new `CalibrationTests` (record CRUD + invalid equipment + invalid result + get by ID + not found, update, delete, paginated list, filter by result, equipment history, schedule CRUD + duplicate conflict + invalid equipment + min-exceeds-max, get by ID, update, delete, list, reliability-based pass extends interval, fail resets to min, fixed schedule unchanged, dashboard aggregates, overdue recalls detected, auth required, cross-tenant isolation, MCP get_calibration_status returns data); full test suite 759 tests |
| 3.38    | 2026-05-04 | **Phase 27 CAPA Workflow implemented**: `CapaRecord` entity (Code auto-generated CAPA-YYYY-NNN, Type Corrective/Preventive, SourceType 7 values, ProblemStatement, ContainmentAction, RCA link, PermanentCorrectiveAction, PreventiveAction, VerificationMethod/DueDate, VerifiedByUserId/At, EffectivenessReviewDate/VerifiedByUserId/At, Status 7-stage lifecycle, OwnerUserId/DisplayName, TeamMemberIds, ClosedAt); `CapaStep` entity (StepType, CompletedBy, Notes, AttachmentFileName); `CapaStatus` enum (Open/Containment/RootCauseAnalysis/Implementation/Verification/EffectivenessReview/Closed); `CapaType` enum (Corrective/Preventive); `CapaSourceType` enum (Manual/CustomerComplaint/InternalAudit/SupplierIssue/SpcOutOfControl/NonConformance/ManagementReview); `ActionItemSourceType.Capa` added; `Phase27_CapaWorkflow` EF migration; `Phase27Dtos` (CapaRecordResponseDto/SummaryDto, CreateCapaRecordDto/UpdateCapaRecordDto, CapaStepResponseDto/CreateCapaStepDto, TransitionCapaDto/LinkRcaDto/VerifyCapaDto, CapaDashboardDto); `CapaController` (paginated list with status/type/sourceType/search filters, CRUD, linear lifecycle transitions with gate checks: containment action required for RCA, RCA link required for Implementation, corrective action required for Verification; close requires effectiveness verified; link-rca with Ishikawa/FiveWhys validation; verify with anti-self-certification; verify-effectiveness; steps CRUD; action items CRUD; dashboard with open/overdue/closed/avgDaysToClose/byStatus/bySource/effectivenessRate/overdueCapas); `get_capa_status` MCP tool (status/type filter, markdown summary with KPI, status breakdown, source type breakdown, overdue table); NavMenu CAPA link under Quality section; `_sectionPaths` updated; MCP server version 3.4 with 33 tools total; 31 `CapaTests` (CRUD + invalid type + get by ID + not found, update fields, delete only Open, delete non-Open blocked, list paginated, filter by type, filter by sourceType, lifecycle Open→Containment, containment→RCA gate, full lifecycle, verification without corrective action fails, RCA without link fails, link-rca Ishikawa succeeds, invalid RCA type, nonexistent RCA, steps list, add step, action item create linked, action items list, dashboard aggregates, verify anti-self-certification blocks owner, verify different user succeeds, close without effectiveness fails, close after effectiveness succeeds, update closed CAPA blocked, auth required, cross-tenant isolation, MCP get_capa_status returns data) |
| 3.37    | 2026-05-04 | **Phase 25 Supplier Quality Management — tests, MCP tool, NavMenu**: 22 new `SupplierTests` (CRUD + duplicate code conflict + get by ID + not found 404, update fields, soft-delete sets Inactive, list paginated, filter by status, search by name, status transition Pending→Approved sets ApprovedDate, invalid transition Pending→Suspended blocked, Approved→Suspended valid, invalid status string 400, evaluation add with computed OverallScore, evaluation invalid scores 400, get evaluations returns list, delete evaluation, evaluations on non-existent supplier 404, dashboard aggregate with approved count, auth required 401, cross-tenant isolation 404, MCP get_supplier_quality_status returns data); `get_supplier_quality_status` MCP tool (status breakdown Approved/Conditional/Suspended/Pending, supplier table with latest evaluation score, at-risk list, optional status filter and top-N limit); NavMenu Suppliers link added under Quality section; `_sectionPaths` updated; bug fix: `SuppliersController.GetDashboard` referenced non-existent `MrbStatus.InReview` → corrected to `MrbStatus.UnderReview`; MCP server version 3.3 with 32 tools total; full test suite 702 tests (700 green, 2 pre-existing flakes in UpgradeFlowTests + WorkflowTests) |
| 3.36    | 2026-05-04 | **Phase 21 Automatic Inventory Tracking implemented**: `ApiKey` entity (SHA-256 hashed, workstation-scoped, `X-Api-Key` header auth); `Workstation` entity (Code, Name, FixedLocationId FK → StorageLocation); `ScanEvent` append-only entity (WorkstationId, ApiKeyId, ScannedBarcode, ItemId, TransactionId, Result enum, ErrorMessage, ScannedAt); `ScanResult` enum (Transferred/AlreadyAtLocation/UnknownBarcode/InvalidItemStatus/WorkstationInactive/Error); `InventoryReferenceType.Workstation` enum value; `Item.Barcode`/`StorageLocation.Barcode`/`Kind.Barcode` unique nullable fields; `Phase17_F7_BarcodeAndScanTables` EF migration (barcode columns + Workstations/ApiKeys/ScanEvents tables with indexes); `ApiKeyAuthenticationHandler` (SHA-256 hash lookup, active/expired validation, workstation_id/fixed_location_id/api_key_id claims, tenant_id claim from key, LastUsedAt update); dual authentication scheme registration (JwtBearer + ApiKey) with combined default policy; `WorkstationsController` (Admin-only: list with search/active filter + paginated, get by ID with last scan time, create with code uniqueness + location validation, update, soft-delete with active-API-key guard); `ApiKeysController` (Admin-only: list with workstationId/active filters + paginated, get by ID, create returns raw key once with SHA-256 hash storage + pk_ prefix, PATCH name/active/expiry, hard-delete); `POST /api/warehouse/scan` scan endpoint (API key auth: workstation claim extraction, workstation/location active validation, barcode resolution Item.Barcode→Item.SerialNumber fallback, item status validation Available/InProcess only, idempotent already-at-location 200 no-op, Transfer/Receipt creation based on item current location, Item.StorageLocationId update, ScanEvent audit log for all outcomes including failures, `inventory.scan` webhook event); `GET /api/warehouse/scan-events` paginated query endpoint (Admin/Engineer, filter by workstationId/result/barcode/dateFrom/dateTo); `Phase21Dtos` (WorkstationResponseDto/SummaryDto, CreateWorkstationDto/UpdateWorkstationDto, ApiKeyResponseDto/CreatedDto, CreateApiKeyDto/UpdateApiKeyDto, ScanRequestDto/ScanResponseDto/ScanItemDto/ScanLocationDto/ScanWorkstationDto, ScanEventResponseDto); ApiClient 10 Phase 21 methods (GetWorkstationsAsync, GetWorkstationAsync, CreateWorkstationAsync, UpdateWorkstationAsync, DeleteWorkstationAsync, GetApiKeysAsync, CreateApiKeyAsync, UpdateApiKeyAsync, DeleteApiKeyAsync, GetScanEventsAsync); MCP `get_workstation_status` tool (active workstations with location, API key count, last scan time, optional code/active filter); MCP server version 3.2 with 31 tools total; 24 new `Phase21Tests` (workstation CRUD + duplicate code + delete blocked with active keys + list paginated, API key create returns raw key + list filter + update + hard-delete, API key auth valid/invalid/inactive, scan transfer/unknown-barcode-404/already-at-location-idempotent/consumed-item-409/receipt-for-unlocated/serial-number-fallback/audit-scan-event, scan-events auth-required + filter-by-result, cross-tenant workstation isolation, MCP get_workstation_status returns data); full test suite 666 tests (664 green, 2 pre-existing flakes in UpgradeFlowTests + WorkflowTests) |
| 3.35    | 2026-04-27 | **Phase 24 SPC & Capability Analysis implemented**: `SpcChart` entity (ProcessId, ContentBlockId, Name, ChartType enum XbarR/XbarS/IndividualMR/P/NP/C/U, SubgroupSize, ControlLimitSource enum Calculated/Manual, UCL/LCL/CL/RangeUCL/RangeLCL/RangeCL, TargetCpk, LSL/USL, IsActive); `SpcDataPoint` entity (SpcChartId, StepExecutionId, Value, SubgroupIndex, CapturedAt); `OutOfControlRule` enum (Rule1_BeyondThreeSigma, Rule2_TwoOfThreeBeyondTwoSigma, Rule3_FourOfFiveBeyondOneSigma, Rule4_EightConsecutiveOneSide); `Phase_F7_SPC` EF migration; `SpcCalculationService` (A2/D3/D4/d2 SPC constants for subgroup sizes 2–10, X-bar/R-bar chart limit calculation, within-subgroup σ estimation via R̄/d₂, overall σ via sample standard deviation, Cp/Cpk from within-subgroup σ with LSL/USL, Pp/Ppk from overall σ, one-sided spec limit support, 4 Nelson Rules for out-of-control detection); `SpcController` (paginated list with processId/active filters, CRUD, data points CRUD with auto-incrementing SubgroupIndex, calculate endpoint returning full SpcCalculationResultDto, dashboard endpoint with all active charts + computed Cp/Cpk/OOC counts); `SpcDtos` (SpcChartDto/SummaryDto, CreateSpcChartDto/UpdateSpcChartDto, SpcDataPointDto/AddSpcDataPointDto, SpcCalculationResultDto, SpcOutOfControlPointDto, SpcSubgroupDto); ApiClient 10 SPC methods; `SpcDashboard.razor` (/spc — KPI cards: active charts/OOC count/worst Cpk/total data points, charts table with Cpk badges color-coded red<1.0/amber<1.33/green, OOC indicators, create chart modal with process picker/chart type/subgroup size/spec limits); `SpcChartDetail.razor` (/spc/{id} — breadcrumb, spec limits card, capability indices panel with X-bar/R-bar/σ/Cp/Cpk/Ppk, calculated control limits display, Nelson rule violation table with rule badges, data points table); SPC Charts link in Quality NavMenu section; MCP `get_spc_status` tool (active charts with Cpk and OOC summary, optional process/OOC-only filters); MCP `get_process_capability` tool (detailed capability analysis with control limits, spec limits, capability indices with assessment labels, Nelson rule violations); MCP server version 3.1 with 30 tools total; 25 new `SpcTests` (chart CRUD, update, delete, not-found, paginated list, dashboard, data points empty list, calculate insufficient data, calculation service basic values, no-spec-limits null capability, empty values, Rule1 OOC detection, subgroup grouping, invalid chart type, invalid process, invalid subgroup size, filter by process, auth required, cross-tenant isolation, MCP get_spc_status, MCP get_process_capability missing/nonexistent, Pp/Ppk with spec limits, one-sided LSL/USL); full test suite 642 tests (641 green, 1 pre-existing flake in UpgradeFlowTests) |
| 3.34    | 2026-04-27 | **Phase 17 Standards Conformance Management implemented**: `StandardsClause` entity (Standard, ClauseNumber, Title, RequirementSummary, IsAs9100Addition) with `ConformanceStandard` enum (Iso9001_2015/As9100RevD); `AuditProgram` entity (Name, Standard, Year, LeadAuditor, Status) with `AuditProgramStatus` enum (Planning/Active/Closed); `Audit` entity (ProgramId, AuditType, Scope, PlannedDate, ActualDate, LeadAuditor, Status) with `AuditType` enum (Internal/External/Supplier/Surveillance) and `AuditStatus` enum (Planned/InProgress/Complete); `AuditFinding` entity (AuditId, ClauseId, FindingType, Description, ObjectiveEvidence, Status, ActionItemId FK) with `FindingType` enum (MajorNonconformance/MinorNonconformance/Observation/OpportunityForImprovement) and `FindingStatus` enum (Open/CorrectiveActionRaised/Closed); `ClauseEvidenceLink` many-to-many entity (ClauseId, EntityType, EntityId, EvidenceNote, IsAutoLinked) with `ClauseEvidenceEntityType` enum (Process/QmsDocument/ControlPlan/Pfmea/ManagementReview/NonConformance); `ClauseCoverageStatus` enum (Covered/PartialCoverage/Gap/OpenMajorFinding) computed per-clause from evidence links and open findings; `Phase17_StandardsConformance` EF migration; `SeedStandardsClausesAsync` in DataSeeder — 37 ISO 9001:2015 clauses (4.1–10.3) + 10 AS9100 Rev D additions, idempotency guard on clause 4.1, auto-links QMS documents to governing clauses via `SeedClauseEvidenceLinksAsync`; `StandardsClausesController` (list with standard filter, get by ID, dashboard endpoint with coverage aggregation + open finding counts + next audit date, evidence link CRUD with duplicate check and entity name resolution); `AuditProgramsController` (paginated list with status/year filter, CRUD, activate/close lifecycle, delete guard when audits exist); `AuditsController` (paginated list with programId/status filter, CRUD, start/complete lifecycle, findings CRUD, raise-corrective-action endpoint creating ActionItem with Critical/High priority by finding type, close-finding endpoint with CA verification gate); `Phase17Dtos` (StandardsClauseDto/SummaryDto, ClauseEvidenceLinkDto/CreateDto, AuditProgramDto/SummaryDto/CreateDto/UpdateDto, AuditDto/SummaryDto/CreateDto/UpdateDto, AuditFindingDto/CreateDto/CloseDto, ConformanceDashboardDto); ApiClient 18 Phase 17 methods; `ConformanceDashboard.razor` (/conformance — KPI cards, clause coverage heatmap with clickable clause buttons, standard filter); `ClauseList.razor` (/conformance/clauses — clause table with coverage badges, detail modal with evidence links + add/delete evidence); `AuditProgramList.razor` (/audit-programs — paginated table with status/year filter, create modal, activate/close/delete actions); `AuditProgramDetail.razor` (/audit-programs/{id} — KPI cards, audits table with start/complete lifecycle, findings panel with add finding modal + raise CA + close finding, edit programme modal); Conformance NavMenu section (Dashboard, Clauses, Audit Programmes); MCP `get_conformance_status` tool (clause coverage summary, open major findings, next audit date); 21 new `ConformanceTests` (seeded clauses accessible, standard filter, get-by-id detail, dashboard aggregate, evidence link add/delete/duplicate-rejection, audit programme CRUD/lifecycle/invalid-transition/delete-blocked-with-audits/delete-succeeds-empty, audit start/complete lifecycle/invalid-transition, finding add+raise-CA, observation-CA-blocked, close-blocked-until-CA-verified, close-observation-succeeds, dashboard-major-finding-affects-coverage, auth-required, cross-tenant-isolation, MCP tool returns data); full test suite 617 tests (616 green, 1 pre-existing flake in UpgradeFlowTests) |
| 3.33    | 2026-04-22 | **F10 Plan-Gated Feature Tiers & Usage Enforcement implemented**: `PlanEnforcementService` (code-defined `PlanLimits` per subscription plan — Trial: 3 users/1 process/50 executions/mo, Starter: 25 users/1 site, Professional: 100 users/3 sites/advanced modules, Enterprise: unlimited); `IPlanEnforcementService` abstraction with `CheckAsync(PlanResource)` returning `PlanCheckResult` (Allowed/AtLimit/Blocked with upgrade prompt); `PlanResource` enum (Users/Processes/Sites/MonthlyExecutions/AdvancedModules); `UsageMeteringService` (`IUsageMeteringService.IncrementAsync` — upsert monthly `UsageMetric` rows); `PlanEnforcementDtos` (PlanCheckResultDto, PlanLimitsDto, PlanUsageSummaryDto); `BillingController` extended with `GET /api/billing/plan` (usage summary with current counts vs limits), `GET /api/billing/plan/check/{resource}` (per-resource limit check), `POST /api/billing/sync-features` (syncs TenantFeatureFlags from current plan); `AuthController.Register` enforces user limit (402 with upgrade prompt); `ProcessesController.Create` enforces process limit; `JobsController.Create` enforces monthly execution limit; `JobsController.Complete` increments JobExecutions usage metric; `PfmeasController.ExportPdf` + `ControlPlansController.ExportPdf` increment PdfExports metric; `StripeWebhookController.HandlePaymentSucceeded` auto-syncs feature flags on plan activation; `SyncFeatureFlagsForPlan` static method propagates plan→flags (Professional/Enterprise enables advanced modules + production/warehouse/training tools); legacy tenants without subscription records bypass enforcement gracefully; 14 new `PlanEnforcementTests` (Trial blocked at 4th user, Starter allows beyond Trial limit, Trial blocked at 2nd process, Starter unlimited processes, Professional auto-enables advanced modules, usage metering on job completion, upgrade prompt in 402 body, Enterprise has no limits, plan check endpoint, plan change propagates flags, Trial blocked at execution limit, PDF export increments metric, billing plan requires auth, cross-tenant plan isolation); full test suite 583 tests all green |
| 3.25    | 2026-04-11 | Phase 22 implemented: 5 domain entities (`FloorPlan`, `FloorPlanWorkstation`, `FloorPlanWorkstationProcess`, `FloorPlanWorkstationTool`, `FloorPlanInventoryLocation`); `FloorPlanStatus` enum (Draft/Published/Archived); `Phase22_FactoryDesignSuite` EF migration; `Phase22Dtos.cs` (20+ DTOs including material-flow request/result); `FloorPlansController` (CRUD + layout save with version increment + publish/archive lifecycle + workstation process/tool management + inventory location linkage + material-flow analysis endpoint with Euclidean distance computation and on-hand inventory lookup); 25 integration tests in `FloorPlanTests.cs` (CRUD, duplicate code rejection, layout save version increment, soft-delete, status transitions with invalid state checks, archived layout rejection, workstation CRUD with duplicate placement detection, process/tool/inventory-location management, material-flow analysis with unresolved and stocked scenarios, list/filter endpoints); `FactoryDesignList.razor` (card grid with status badges, workstation/location counts, status filter, create modal); `FactoryDesignEditor.razor` (toolbar, element palette sidebar, HTML5 Canvas mount, properties panel placeholder, status bar); `factory-canvas.js` ES module (~580 lines: grid rendering, 7 element types with distinct visuals, select/draw tools, snap-to-grid, zoom/pan, resize handles, keyboard shortcuts, HiDPI support, Blazor JS interop callbacks); 7 ApiClient methods; NavMenu Factory Design entry under Production |
| 3.23    | 2026-04-11 | Mobile optimization plan: 16 work packages covering all 77 pages — WP1 MyWork/MyActions, WP2 ExecutionWizard, WP3–7 all list pages (page-heading-row, flex-wrap, col-hide-mobile), WP8–9 dashboards (stat scaling, KPI grid fixes), WP10–12 all detail pages (header wrapping, sub-table column hiding), WP13 matrix/grid pages (sticky first column, scroll shadows), WP14 form pages (login card max-width fix), WP15 shared components (Pager wrap, Toast mobile, global CSS consolidation), WP16 portal pages |
| 3.22    | 2026-04-10 | Mobile browser optimization: collapsible off-canvas sidebar with hamburger toggle (NavMenu IsOpen/OnClose parameters, MainLayout mobile topbar + backdrop), full responsive CSS (4 breakpoint media queries for sidebar drawer, touch targets, full-screen modals, table column hiding), page-level fixes (SearchBox class-based width, flex-wrap toolbars, page-heading-row, Dashboard de-duped padding, builder mobile info banners, col-hide-mobile on secondary table columns), reflection-based MobileLayoutTests (5 tests) |
| 3.21    | 2026-03-28 | Phase 21 design: Automatic Inventory Tracking — `ApiKey` entity (SHA-256 hashed, workstation-scoped, `X-Api-Key` header auth), `Workstation` entity (Code, FixedLocationId FK → StorageLocation), `ScanEvent` append-only log; `Item.Barcode`/`StorageLocation.Barcode`/`Kind.Barcode` unique nullable fields; `POST /api/warehouse/scan` single-barcode endpoint (API key → workstation → fixed location, barcode → Item resolution with SerialNumber fallback, Transfer/Receipt creation, idempotent re-scan handling); `ScanResult` enum; `InventoryReferenceType.Workstation`; admin CRUD for workstations and API keys; `inventory.scan` webhook event; `get_workstation_status` MCP tool; API-only — no Blazor scanning UI |
| 3.20    | 2026-03-28 | Phase 20 implemented: AI Integration — 6 MCP write tools (`create_nonconformance`, `create_action_item`, `complete_action_item`, `create_job`, `record_inventory_transaction`, `transition_job`) mirroring REST controller validation in partial class `McpController.WriteTools.cs`; `McpAuditLog` append-only entity with Stopwatch + try/catch/finally wrapper on all tool calls (classifies action from tool name prefix, extracts JWT user context, truncates response to 500 chars); `list_mcp_audit_log` MCP tool + `GET /mcp/audit` REST endpoint with paginated filters; `AiAuditLog.razor` Blazor page at `/ai-audit` with date range/tool/user/status filters and expandable detail rows; Structured JSON responses via auto-injected `format` parameter on all tool schemas (`markdown` default, `json` returns `application/json` content block with `{ tool, success, content }` envelope); Webhook event system: `IWebhookEventPublisher` interface, `WebhookEventQueue` (bounded `Channel<T>`), `WebhookDeliveryService` (`BackgroundService` with HMAC-SHA256 signing, 3-retry exponential backoff, delivery log), `WebhooksController` (6 endpoints: CRUD + delivery log + test event), `WebhookSubscription` + `WebhookDelivery` entities with cascade delete; webhook events fired from all write tools (`job.created/started/completed/cancelled`, `nonconformance.created`, `action_item.created/completed`, `inventory.*`); wildcard event matching (`*`, `job.*`); `WebhookList.razor` at `/webhooks` with create/edit modals, delivery log panel, test button; NavMenu Admin section with AI Audit Log + Webhooks links; `Phase20_AiIntegration` EF migration; MCP server version 3.0 with 28 tools total |
| 3.19    | 2026-03-27 | Phase 19 implemented: Warehouse Management — `StorageLocation` entity (self-referencing zone/aisle/bay/bin hierarchy, unique Code, IsActive); `InventoryTransaction` immutable event log (Receipt/Issue/Transfer/Adjustment/PicklistConsumption types with type-specific validation); `PickList` + `PickListLine` entities (late-binding ItemId at pick time); `InventoryTransactionType`/`PickListStatus`/`PickListLineStatus`/`InventoryReferenceType` enums; `Item.StorageLocationId` + `Kind.ReorderThreshold`/`ReorderQuantity` + `Job.PickListId` entity extensions; `Phase19_WarehouseManagement` EF migration; `WarehouseController` (10 endpoints: location CRUD, on-hand aggregation with low-stock filter, transaction recording with type-specific validation, dashboard KPIs, bulk receive-from-job); `PickListsController` (5 endpoints: list, detail, pick with Item/Kind/Location validation + Issue transaction, consume with PicklistConsumption transaction + Item.Status=Consumed, short-ship); Job creation auto-generates PickList from input material ports (QtyRuleMode derivation, best-fit source location suggestion); ExecutionWizard Phase 5 material consumption hook (picked-line table, editable consumed quantities, confirm-all button); 16 ApiClient methods; 5 Blazor pages (WarehouseDashboard, LocationList, LocationDetail, PickListList, PickListDetail); NavMenu Warehouse section; MCP `get_inventory_status` tool (on-hand by Kind with location filter, low-stock flag, markdown table); MCP server version 2.2 |
| 3.12    | 2026-03-24 | Phase 18 implemented: 3D Model Viewer in Process Builder & Execution — `StepModel` entity (Id, StepTemplateId, FileName, OriginalFileName, MimeType, UploadedAt, UploadedByUserId); `KindModelRefId` optional FK on `StepTemplate` (SetNull on Kind delete); `Phase18_StepModel` EF migration; `Phase18Dtos` (StepModelResponseDto, SetKindModelRefDto); `StepTemplateResponseDto`/`StepExecutionResponseDto`/`ProcessStepResponseDto` extended with HasStepModel/StepModel/KindModelRefId/KindModelRefMimeType fields; `StepTemplatesController` gains `POST {id}/model` (upload STL/OBJ/GLB/GLTF ≤ 100 MB, mutual exclusivity with KindModelRef), `GET {id}/model/download` (returns file bytes for direct model or 302 redirect to `/api/kinds/{id}/model/download` for KindModelRef), `DELETE {id}/model` (204, removes DB record + file), `PATCH {id}/kind-model-ref` (set/clear KindModelRefId, validates Kind has a model, rejects if direct StepModel exists); all 8 StepTemplates queries, JobsController step-execution queries, ProcessesController LoadProcess, and StepExecutionsController lifecycle queries updated with ThenInclude(StepModel)/ThenInclude(KindModelRef) eager-loading chains; `GetStepModelDownloadUrl` added to ApiClient; `UploadStepModelAsync`, `DeleteStepModelAsync`, `SetKindModelRefAsync` added to ApiClient; `StepTemplateDetail.razor` gains 3D Model card (upload/replace/delete direct model, set/clear KindModelRef from Kind picker, inline Three.js viewer via ModelViewer.init/destroy JS interop, 3-retry init pattern, IAsyncDisposable); `ProcessBuilder.razor` Slide view gains read-only inline viewer below content blocks (visible when step has model or KindModelRef, "No model" placeholder with link to StepTemplateDetail, IAsyncDisposable, destroy on step change/view mode switch); `ExecutionWizardContent.razor` Phase 4 gains collapsible 3D model side panel (col-lg-5 Bootstrap column, toggle ▲/▼ button, destroy on collapse/phase exit, re-init on expand, IAsyncDisposable); 12 integration tests in StepModelTests (upload valid STL, invalid extension, replace model, oversized file note, download direct + redirect + no-model, delete, set/clear KindModelRef, KindModelRef with no model, mutual exclusivity) |
| 3.14    | 2026-03-23 | Phase 18 design: 3D Model Viewer in Step Templates — `StepModel` entity (reuse existing upload pipeline: STL/OBJ/GLB/GLTF/STEP/IGES), `model-viewer.js` component embedded in ProcessBuilder slide view and ExecutionWizard step prompt phase, `StepTemplateDetail` 3D model upload/preview panel, `KindModelRef` optional FK linking a step model directly to a Kind's uploaded model |
| 3.13    | 2026-03-22 | Kind Enhancement: Extended Properties + Document Attachments + 3D Model Viewer — `KindSourceType` enum (Make/Buy/ReferenceDocument/Phantom/Consumable); `KindDocument` entity (file attachments with GUID-based storage); Kind entity extended with 13 new properties (SourceType, UnitOfMeasure, Cost, Price, VendorName, VendorPartNumber, LeadTimeDays, Weight, WeightUnit, RohsStatus, CountryOfOrigin, Revision, Notes) + 3 model fields (ModelFileName, ModelOriginalFileName, ModelMimeType); vendor fields server-side nulled when SourceType != Buy; `KindsController` gains sourceType filter, document upload/download/delete endpoints, 3D model upload/download/delete endpoints (STL/OBJ/GLB/GLTF); Three.js ES module integration via CDN importmap (`model-viewer.js` with STLLoader/OBJLoader/GLTFLoader + OrbitControls); `KindList.razor` extended with Source Type tile picker, cost/pricing section, conditional vendor fields, physical/compliance section, source type column + filter; `KindDetail.razor` gains Extended Properties card, Vendor Information section, side-by-side 3D Model Viewer (orbit/zoom/pan) + Documents panel with upload/download/delete; `KindEnhancement_ExtendedProperties` EF migration; 24 new tests (34 total Kind tests) |
| 3.12    | 2026-03-21 | Phase 12 Step 4: WorkflowJob execution record — WorkflowNodeStatus enum (Pending/Active/Complete/Skipped), WorkorderJob enriched with NodeStatus + nullable JobId, all non-terminal nodes pre-populated at workorder creation, GradeBased skipped-node detection, Cancel/Complete marks Pending→Skipped, WorkorderDetail.razor NodeStatus-driven display, 7 new tests, EF migration Phase12f_WorkflowJobExecutionRecord |
| 3.11    | 2026-03-21 | Phase 12 Step 3 implemented: `WorkflowSchedule` entity + background scheduler service — `ScheduleRecurrenceType` enum (Hourly/Daily/Weekly/Monthly/Quarterly/Annually); `WorkflowSchedule` entity (WorkflowId, Name, RecurrenceType, RecurrenceInterval 1–168h/365d/52w/24m/8q/10y, DayOfWeek, DayOfMonth, StartDate, EndDate, SubjectTemplate {Month}/{Year}/{Date} tokens, IsActive, NextRunAt, LastRunAt); `Workorder.ScheduleId` FK (SetNull on delete); `Phase12e_WorkflowSchedule` EF migration; `WorkflowSchedulesController` (GET list+filter by workflowId, GET by id, POST create, PUT update, DELETE block if workorders exist, POST activate/deactivate, static `ComputeNextRunAt` + `ComputeInitialNextRunAt`); `WorkflowSchedulerService` (BackgroundService, polls every 60s configurable via `Scheduler:IntervalSeconds`, fires due schedules, creates Workorder + Jobs + StepExecutions, resolves subject template tokens, advances NextRunAt from now not old NextRunAt, deactivates after EndDate, skipped in Testing environment, `ProcessDueSchedulesAsync` internal for tests); `InternalsVisibleTo` in Api csproj; `WorkflowScheduleList.razor` at `/workflows/{id}/schedules` (table with recurrence/NextRunAt/LastRunAt/workorder count/active badge, create/edit modal with 6-tile recurrence picker, interval with context label+hint, conditional DayOfWeek/DayOfMonth fields, SubjectTemplate with token hint, activate/deactivate/delete actions); `WorkflowDetail.razor` gains Schedules summary card (count badge, up to 5 active schedules with NextRunAt, "Manage Schedules →" link); `ApiClient` gains 7 schedule methods; `WorkflowScheduleDtos` + `WorkorderResponseDto.ScheduleId`; `appsettings.json` Scheduler section; 37 tests in `WorkflowScheduleTests` (CRUD, interval validation, DayOfWeek/Month null enforcement, ComputeNextRunAt unit tests, scheduler integration tests via direct ProcessDueSchedulesAsync invocation) |
| 3.10    | 2026-03-21 | Phase 12 Steps 1 & 2 implemented: (Step 1) GradeBased link routing in `ProgressWorkorder` — evaluates completed job's item grades against `WorkflowLinkCondition`, fires matching GradeBased outgoing links automatically, Manual links remain operator-only; 6 integration tests in `WorkorderTests`; (Step 2) MyWork OrgUnit-based job filtering — `GET /api/step-executions?myWork=true` filters to step executions for jobs where `WorkflowProcess.AssigneeId` is an OrgUnit the current user belongs to (via `OrgUnitMember`), or where `StepExecution.AssignedToUserId` matches the current user; `ApiClient.GetStepExecutionsAsync` gains `myWork` parameter; `MyWork.razor` updated to always pass `myWork=true`; `TestWebApplicationFactory` gains `GenerateJwt(userId, role)` and `CreateAuthenticatedClient(userId)` overloads; 5 integration tests in `MyWorkOrgUnitTests` |
| 3.9     | 2026-03-20 | Phase 12a+12b+12b½ implemented: `OrgUnit` entity (Code, Name, Type enum, ParentId self-ref FK, IsActive), `OrgUnitType` enum (Department/WorkArea/Role/Person), `OrgUnitMember` join entity (UserId FK → ApplicationUser, OrgUnitId FK → OrgUnit, unique composite index), `AssigneeId` FK on `WorkflowProcess` → OrgUnit; `OrgUnitsController` (full CRUD, hierarchy filtering, circular reference prevention, member count, children endpoint, membership endpoints: GET/POST `/{id}/members`, DELETE `/{id}/members/{memberId}`, GET `/api/users/{userId}/orgunits`); `OrgUnitList.razor` (table with search/type/active/top-level filters, create/edit modals, Members modal with user picker dropdown, add/remove members); `WorkflowBuilder.razor` updated with assignee dropdown on workflow nodes (sidebar + slide-out editor); `AddOrgUnit` + `AddWorkflowProcessAssignee` + `AddOrgUnitMember` EF migrations; `OrgUnitTests` (20 tests) + `OrgUnitMemberTests` (12 tests); ApiClient OrgUnit CRUD + membership methods |
| 3.10    | 2026-03-20 | Phase 17 design: Standards Conformance Management — `StandardsClause` seed table (ISO 9001:2015 + AS9100 Rev D), `AuditProgram`/`Audit`/`AuditFinding` entities with `ActionItem` link for CA tracking, `ClauseEvidenceLink` many-to-many between clauses and system entities, auto-linking of seeded QMS documents to their governing clauses, Conformance Dashboard with clause-coverage heatmap, Audit Program and Audit Finding pages, MCP `get_conformance_status` tool |
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

#### 12c — WorkflowJob (Workflow Execution Record)

A parent-level record for a complete workflow run. Analogous to `Job` for a single process, but spanning the entire workflow graph.

**Key entity: `WorkflowJob`**

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
  + workflow_job_id      (FK → WorkflowJob, nullable)
  + workflow_process_id  (FK → WorkflowProcess, nullable)
```

These link each process-level Job back to its parent workflow run and to the specific graph node it represents, enabling the sequencing service to determine which outgoing links to follow on completion.

---

#### 12d — Sequencing Service

Triggered whenever a `Job` status transitions to `Completed`. If the job has a `WorkflowJobId`:

1. Look up the `WorkflowProcess` node the job corresponded to (`WorkflowProcessId`)
2. Find all outgoing `WorkflowLink` edges from that node
3. Evaluate routing: `Always` links always fire; `GradeBased` links fire when the job's item grades match a `WorkflowLinkCondition`; `Manual` links wait for an operator to confirm
4. For each link that fires, create a new `Job` for the target `WorkflowProcess.ProcessId`, set `WorkflowJobId` and `WorkflowProcessId`, and notify `WorkflowProcess.AssigneeId`
5. If no outgoing links fire (terminal node), mark the `WorkflowJob` as `Completed`

This is the complete sequencing loop. No changes to `WorkflowLink` or `WorkflowLinkCondition` are required — the graph routing model is already sufficient.

---

**Key entities added:**
- `OrgUnit` (Id, Code, Name, Type, ParentId, IsActive) ✅ Implemented
- `OrgUnitMember` (Id, UserId FK → ApplicationUser, OrgUnitId FK → OrgUnit) ✅ Implemented — many-to-many join; unique on (UserId, OrgUnitId); cascade delete on both sides
- `WorkflowJob` (Id, WorkflowId, Subject, Status, StartedAt, CompletedAt)
- `WorkflowJobStatus` enum: `Running`, `Completed`, `Cancelled`
- `OrgUnitType` enum: `Department`, `WorkArea`, `Role`, `Person` ✅ Implemented

**Existing entities modified:**
- `WorkflowProcess` + `assignee_id` (FK → OrgUnit, nullable) ✅ Implemented
- `Job` + `workflow_job_id` (FK → WorkflowJob, nullable) + `workflow_process_id` (FK → WorkflowProcess, nullable)

**Surfaces:**
- `OrgUnitList.razor` — manage departments, work areas, roles
- `WorkflowDetail` updated — assignee picker per node
- `WorkflowJobList.razor` / `WorkflowJobDetail.razor` — start a workflow run, view all in-flight runs, track progress through the graph
- `MyWork` page updated — operators see jobs assigned to their OrgUnit(s) in addition to directly assigned jobs ✅ Implemented 2026-03-21
- `POST /api/workflowjobs` — start a new workflow run
- `GET /api/workflowjobs/{id}` — current state + graph progress
- Notification hooks for assignees when a new process job is created for their OrgUnit

#### 12e — WorkflowSchedule (Periodic Execution)

Workflows that run on a fixed recurrence (e.g. monthly calibration, weekly safety walk, quarterly management review) need a schedule entity that fires automatically and injects a `WorkflowJob` into the OrgUnit queues at the right time. Once created by the scheduler, the `WorkflowJob` is indistinguishable from an ad-hoc run — the same sequencing service handles it.

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
| `SubjectTemplate` | string(500) | Template for WorkflowJob.Subject, e.g. `"Monthly QC Audit — {Month} {Year}"` |
| `IsActive` | bool | Whether the scheduler should process this record |
| `NextRunAt` | DateTimeOffset? | Computed datetime of the next scheduled fire |
| `LastRunAt` | DateTimeOffset? | When the scheduler last fired this schedule |

**`WorkflowJob` gains one field:**
```
WorkflowJob
  + schedule_id  (FK → WorkflowSchedule, nullable)
```
Null for ad-hoc runs; set when the job was created by the scheduler. Allows filtering "all runs of this schedule" and tracking whether a scheduled window was missed.

**Scheduler background service:**
1. Runs on a configurable interval (e.g. every minute)
2. Queries `WorkflowSchedule WHERE is_active = true AND next_run_at <= now`
3. For each due schedule: creates a `WorkflowJob` (resolves subject template, sets `ScheduleId`), creates `Job` records for each entry-point `WorkflowProcess` node, pushes assignees from `WorkflowProcess.AssigneeId`
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
| New `WorkflowJobs` table | Small |
| `workflow_job_id` + `workflow_process_id` on `Jobs` | Trivial |
| New `WorkflowSchedules` table | Small |
| `schedule_id` on `WorkflowJobs` | Trivial |
| Sequencing service (job completion hook) | Medium |
| Scheduler background service | Medium |

**Status:** Implemented 2026-03-21 (version 3.11). See changelog for full details.

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
- WorkflowJob management (they receive work, they don't launch workflow runs)

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
| Phase 12 WorkflowJob + sequencing service | Workflow execution — without this, workflows are diagrams, not running operations |
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

Formal periodic review at the executive level, providing structured inputs and decision recording to satisfy ISO 9001 clause 9.3.

**Key entity: `ManagementReview`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Title` | string | e.g. "Q1 2026 Management Review" |
| `ReviewType` | enum | `Quarterly` / `Annual` / `Special` |
| `ScheduledDate` | DateTime | |
| `Status` | enum | `Scheduled` / `InProgress` / `Complete` |
| `ConductedBy` | string? | |

**Inputs collected at review time:**
- NC count by period — auto-populated from data
- Action item close rate % — auto-populated
- Open MRB count and average age — auto-populated
- Customer complaints — manual entry
- Supplier quality performance — manual entry
- Internal audit status — manual entry linking to audit finding records
- Prior review action item close-out status — linked `ActionItem` records for the previous review's outputs

**Outputs:**
- `ManagementReview.Decisions` — free-text decisions and strategic direction
- Action items created directly from the review — become `ActionItem` records with `SourceEntityType = ManagementReview`
- New performance targets for the next review cycle

**Surfaces:**
- `ManagementReviewList` / `ManagementReviewDetail` — create and conduct reviews; auto-populated input panels with manual-supplement fields; historical record of all reviews with trend comparisons between periods
- MCP tool `get_management_review_status` — current open action items from the most recent management review, with completion status

**Status:** Designed — depends on Phase 8c (NonConformance), Phase 10d (MRB), and Phase 10a–c (RCA) to have meaningful data to populate the auto-populated inputs.

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

### Phase 17 — Standards Conformance Management

**Goal:** Organise the evidence the system already produces — documents, approved processes, training records, non-conformances, MRB reviews, management reviews, action items — against the specific clause requirements of ISO 9001:2015 and AS9100 Rev D, giving a quality manager an auditable coverage map and giving an auditor a single place to find objective evidence.

**Design premise:** The system already generates most of the objective evidence required for a standards audit. What is missing is a *conformance layer* that ties those records to specific clause numbers and gives the organisation a picture of where evidence is present, where it is thin, and where audit findings remain open. This phase adds four new entities; everything else flows through infrastructure that already exists.

---

#### 17a — Standards Clause Register

A pre-seeded, read-only catalogue of addressable clauses from both standards. No UI for editing — these are fixed reference data, analogous to how the QMS document seeds work.

**Key entity: `StandardsClause`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Standard` | enum | `Iso9001_2015` / `As9100RevD` |
| `ClauseNumber` | string | e.g. `"8.5.2"` |
| `Title` | string | e.g. `"Identification and Traceability"` |
| `RequirementSummary` | string | One-paragraph plain-language summary of what the clause requires |
| `IsAs9100Addition` | bool | True for clauses that are AS9100-only and not present in base ISO 9001 |

**Seeded content:** All ~80 addressable clauses across ISO 9001:2015 (clauses 4–10) and AS9100 Rev D additions. Delivered as a `SeedStandardsClausesAsync` method in `DataSeeder.cs`, idempotent on re-run, guarded on `StandardsClause` for ISO 9001 clause `"4.1"`.

---

#### 17b — Clause Evidence Map

A many-to-many between clauses and existing system records that serve as objective evidence of conformance. Most links are **auto-generated** by known mappings; engineers can also add manual links with a free-text note.

**Key entity: `ClauseEvidenceLink`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `ClauseId` | Guid | FK → `StandardsClause` |
| `EntityType` | enum | `Process` / `QmsDocument` / `TrainingRecord` / `ControlPlan` / `Pfmea` / `ManagementReview` / `NonConformance` |
| `EntityId` | Guid | FK into the respective table |
| `EvidenceNote` | string? | Optional context note on how this record evidences the clause |
| `IsAutoLinked` | bool | True when created by the seeder or auto-link logic; false for manually added links |

**Auto-linking rules (applied on startup by the seeder):**

| Clause | Auto-linked entities |
|---|---|
| 4.1 / 4.2 | QMS-001 (Scope), QMS-004 (Quality Manual) |
| 5.2 | QMS-002 (Quality Policy) |
| 6.1 | QMS-005 (Risk Management) |
| 6.2 | QMS-003 (Quality Objectives) |
| 7.1.5 | QMS-008 (Calibration), all `MaintenanceTask` records of type `Calibration` |
| 7.2 / 7.3 | QMS-007 (Competence & Training), all `CompetencyRecord` rows |
| 7.5 | QMS-006 (Document Control), all Released `Process` rows with `ProcessRole = QmsDocument` |
| 8.2 | QMS-009 / QMS-010 (Customer Communication + Requirements Review) |
| 8.3 | QMS-011 (Design & Development) |
| 8.4 | QMS-012 (Supplier Control) |
| 8.5 | QMS-013 (Production Planning), all Released manufacturing processes |
| 8.6 | QMS-014 (Inspection & Testing), all `ControlPlan` rows |
| 8.7 | QMS-016 (Nonconformance Control), all `NonConformance` rows, all `MrbReview` rows |
| 9.1.2 | QMS-017 (Customer Satisfaction) |
| 9.2 | QMS-018 (Internal Audit) — and all `Audit` records (17c) once created |
| 9.3 | QMS-019 (Management Review), all `ManagementReview` rows |
| 10.2 | QMS-020 (Corrective Action), all `ActionItem` rows with `SourceEntityType ≠ Manual` |

**Coverage status** is derived at query time from `ClauseEvidenceLink` counts and the status of linked `AuditFinding` records:

| Status | Definition |
|---|---|
| `Covered` | At least one released / active evidence link, no open Major findings |
| `PartialCoverage` | Evidence present but at least one open Minor finding or Observation |
| `Gap` | No evidence links at all |
| `OpenMajorFinding` | At least one open Major nonconformance finding against this clause |

---

#### 17c — Audit Program & Findings

The formal audit record: who audited, what was in scope, and what they found.

**Key entity: `AuditProgram`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Name` | string | e.g. "2026 Internal Audit Programme" |
| `Standard` | enum | `Iso9001_2015` / `As9100RevD` / `Both` |
| `Year` | int | Calendar year this programme covers |
| `LeadAuditor` | string | Display name |
| `Status` | enum | `Planning` / `Active` / `Closed` |

**Key entity: `Audit`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `ProgramId` | Guid | FK → `AuditProgram` |
| `AuditType` | enum | `Internal` / `Surveillance` / `Recertification` / `SecondParty` |
| `Scope` | string | Free-text description of processes and areas covered |
| `PlannedDate` | DateTime | |
| `ActualDate` | DateTime? | Null until the audit has been conducted |
| `LeadAuditor` | string | |
| `Status` | enum | `Planned` / `InProgress` / `Complete` |

**Key entity: `AuditFinding`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `AuditId` | Guid | FK → `Audit` |
| `ClauseId` | Guid | FK → `StandardsClause` — the clause this finding is raised against |
| `FindingType` | enum | `MajorNonconformance` / `MinorNonconformance` / `Observation` / `OpportunityForImprovement` |
| `Description` | string | Finding statement |
| `ObjectiveEvidence` | string | Evidence seen that supports the finding |
| `Status` | enum | `Open` / `CorrectiveActionRaised` / `Closed` |
| `ActionItemId` | Guid? | FK → `ActionItem` — null for Observations/OFIs that don't require a CA |
| `ClosedAt` | DateTime? | |
| `ClosureNotes` | string? | |

**`ActionItem` integration:** Findings of type `MajorNonconformance` or `MinorNonconformance` create an `ActionItem` (via the standard Phase 15 mechanism, `SourceEntityType = AuditFinding`) and link `ActionItemId` back on the finding. The finding's `Status` advances to `Closed` automatically when the linked action item reaches `Verified`. This means audit CARs flow through the same tiered accountability system as all other quality actions — no separate CAR tracking table is needed.

**`ClauseEvidenceLink` integration:** On `Audit` completion, a `ClauseEvidenceLink` of `EntityType = Process` (the audit record itself) is auto-created for clause 9.2 (Internal Audit), with `IsAutoLinked = true`.

**`ManagementReview` integration:** The Management Review auto-populated inputs (Phase 15c) include a count of open audit findings per type (Major / Minor / OFI), with the finding source standard shown. This satisfies ISO 9001 clause 9.3.2(f) (results of monitoring and measurement, including audit results).

---

#### 17d — Conformance Dashboard

The primary surface for this module. A quality manager can see the state of their conformance programme at a glance.

**`/conformance` — Conformance Dashboard:**
- Standard selector (ISO 9001 / AS9100 / Both)
- Clause coverage heatmap — a grid of all clauses coloured by coverage status (`Covered` / `PartialCoverage` / `Gap` / `OpenMajorFinding`); click any cell to expand to that clause's evidence links and open findings
- Summary KPI bar: total clauses covered / partial / gap; open Major findings; open Minor findings; next audit date
- Open findings table — all findings not yet `Closed`, sorted by type (Major first) then age; each row links to the audit detail page

**`/conformance/clauses` — Clause Browser:**
- Filterable list of all seeded clauses (filter by standard, coverage status)
- Expand any clause: evidence links panel (entity type, linked record name, status, note), open findings panel

**`/audit-programs` — Audit Program List:**
- All programmes with status badge, year, standard, lead; create modal

**`/audit-programs/{id}` — Program Detail:**
- Programme header; list of audits with status badges; aggregate finding counts by type; create audit modal

**`/audits/{id}` — Audit Detail:**
- Audit header (edit scope/dates/status); findings table with type badges and status; "Add Finding" modal (clause picker with typeahead on title/number, finding type, description, evidence); "Raise Corrective Action" button on each Major/Minor finding (creates an `ActionItem` and links it); finding close modal

---

#### Integration with existing features

| Existing Feature | Integration |
|---|---|
| `ActionItem` (Phase 15) | Audit findings that require CA create an `ActionItem` (`SourceEntityType = AuditFinding`); finding auto-closes when action is `Verified`; overdue CA on an audit finding appears in `TeamActions` and `QualityScorecard` exactly like any other overdue action |
| `ManagementReview` (Phase 15) | Auto-populated inputs extended with count of open Major / Minor findings per standard, and whether last audit cycle is complete |
| QMS documents (Phase 13/14 seeds) | Auto-linked to their governing clauses on seeder run — zero manual configuration required for a new deployment |
| `CompetencyRecord` (Phase 16) | Each current competency record contributes an evidence link for clause 7.2; expiring or expired records are reflected in coverage status |
| `ControlPlan` rows | Auto-linked to clause 8.6 as evidence of inspection and testing controls |
| `MrbReview` rows | Auto-linked to clause 8.7 (Nonconformance Control) |
| MCP server | New tool `get_conformance_status` — returns clause coverage summary and open finding counts per standard; optionally filtered to a specific clause number |

---

#### Key design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Clause data as seed, not user-configurable | Fixed seed table | Standard clause numbers and titles do not change between audits; allowing user edits would create divergence from the published standard and undermine traceability |
| Auto-linking vs. manual | Primarily auto-linked, manual supplement | Manual linking of every evidence record is operationally unsustainable; auto-linking by known patterns covers 90% of the common clauses with zero user effort |
| Single `AuditFinding.ActionItemId` FK | One action item per finding | Audit findings are addressed by one corrective action; multiple tasks arising from a CA are sub-tasks of that action item, not separate findings |
| No separate CAR entity | Reuse `ActionItem` with `SourceEntityType = AuditFinding` | Every other quality event already generates `ActionItem` records; a separate CAR table would duplicate tracking and fragment the tiered accountability view |
| Coverage status computed at query time | Not persisted | Coverage status is a function of live evidence links and live finding statuses — persisting it would require invalidation logic on every related write; computing it on read is simpler and always current |

**New entities:** `StandardsClause`, `AuditProgram`, `Audit`, `AuditFinding`, `ClauseEvidenceLink`

**New enum values:** `Standard` (`Iso9001_2015` / `As9100RevD`), `AuditType` (`Internal` / `Surveillance` / `Recertification` / `SecondParty`), `FindingType` (`MajorNonconformance` / `MinorNonconformance` / `Observation` / `OpportunityForImprovement`), `AuditStatus` (`Planned` / `InProgress` / `Complete`), `AuditProgramStatus` (`Planning` / `Active` / `Closed`), `ClauseEvidenceEntityType`, `ClauseCoverageStatus`

**Existing entities extended:** `ActionItem.SourceEntityType` gains `AuditFinding`; `ManagementReview` auto-populated snapshot gains open-finding counts

**MCP tool:** `get_conformance_status` — returns per-standard clause coverage summary (covered / partial / gap counts), list of open Major findings with clause reference, and next planned audit date

**Status:** Designed — depends on Phase 15 (`ActionItem`) and Phase 14 (QMS document seeding) being complete. Both are built. ✅

---

### Phase 18 — 3D Model Viewer in Process Builder & Execution

**Goal:** Embed the interactive CAD viewer — already proven on the Kind detail page — directly into the process design and operator execution surfaces so that spatial context is available at every step.

**Status:** Designed — not yet built.

#### 18a — StepTemplate Model Attachment

Re-use the upload pipeline and `model-viewer.js` Three.js component from the Kind 3D viewer (`KindDetail.razor`) without modification. Only the storage association changes: instead of a model being attached to a Kind, it is attached to a StepTemplate.

**Key entity: `StepModel`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `StepTemplateId` | Guid | FK → `StepTemplate` |
| `FileName` | string | GUID-based storage filename (same scheme as `KindDocument`) |
| `OriginalFileName` | string | Display name shown to the user |
| `MimeType` | string | e.g. `model/stl`, `model/gltf+json`, `application/octet-stream` |
| `UploadedAt` | DateTime | |
| `UploadedByUserId` | string? | FK → `ApplicationUser` |

**`KindModelRef` (optional FK):** A step may optionally point at a Kind's already-uploaded model instead of uploading a separate file. When `KindModelRef` is set, the viewer streams the file from the Kind's model endpoint — no duplicate storage. Only one of `StepModel` or `KindModelRef` should be active per step at a time.

**Supported formats:** STL, OBJ, GLB, GLTF, STEP, IGES — the same set already handled by `model-viewer.js` (STLLoader, OBJLoader, GLTFLoader + OrbitControls).

**API changes:**
- `StepTemplatesController` gains `/api/step-templates/{id}/model` (GET stream, POST upload, DELETE) — mirrors `KindsController` 3D model endpoints exactly.

**`StepTemplateDetail.razor` changes:**
- New "3D Model" card below the content blocks section.
- Upload button (accept `.stl,.obj,.glb,.gltf,.step,.igs,.iges`), filename display, inline preview using `model-viewer.js`, delete button.
- If `KindModelRef` is set, the card shows "Using model from Kind: *{KindName}*" with a link to `KindDetail`.

---

#### 18b — ProcessBuilder Slide View Integration

When the ProcessBuilder is in slide view and the selected step has an attached `StepModel`, render the interactive viewer in the right-hand editor panel below the content blocks — same orbit/zoom/pan behaviour as `KindDetail`.

- Viewer is read-only in the builder (uploading/deleting is done via `StepTemplateDetail`, not the builder).
- A "No model attached" placeholder is shown when no model exists, with a link to the step template detail page.
- No new Razor components required — the existing `model-viewer.js` JS interop is reused; only a Blazor host element and an `@inject IJSRuntime` call are needed in `ProcessBuilder.razor`.

---

#### 18c — ExecutionWizard Step Prompt Phase

During job execution, the ExecutionWizard renders the interactive viewer in the prompts phase (Phase 3 of 5) when the active step has an attached model.

- Viewer renders in a collapsible side panel to the right of the prompt inputs so it does not obscure form fields on smaller screens.
- Collapse/expand state is persisted in component state for the session (not server-persisted).
- On mobile viewports the panel renders below the prompt inputs instead of beside them.
- The viewer is read-only — no upload/delete controls are shown to operators.

---

#### Key design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Reuse `model-viewer.js` unchanged | Yes | The Three.js component already handles all required formats and OrbitControls; duplicating it would create a maintenance burden |
| `StepModel` as a separate entity rather than extending `StepTemplate` directly | Separate entity | Keeps the nullable file-storage fields out of the core step schema; mirrors the `KindDocument` pattern already in use |
| `KindModelRef` optional FK | Opt-in | Avoids requiring two uploads when the part model is already on the Kind; falls back to per-step upload when no Kind model exists |
| Upload/delete in `StepTemplateDetail`, not `ProcessBuilder` | Design-time only | Keeps the builder focused on layout; file management belongs in the detail page |

**New entities:** `StepModel`

**Existing entities extended:** `StepTemplate` gains optional `StepModelId` FK and optional `KindModelRefId` FK

**EF migration:** `Phase18_StepModel`

**Status:** Implemented 2026-03-24 (version 3.12). See changelog for full details.

---

### Phase 19 — Warehouse Management ✅

**Goal:** Track where physical stock (Items) lives, automate material-pull picklists when jobs are created, and record consumption during execution — closing the loop between process design and inventory reality.

**Status:** Implemented.

**Core principle:** Only **Items** occupy inventory locations — never Kinds. A Kind is the *blueprint* (part definition, material specification); an Item is the *physical instance* of a Kind. Every Item references the Kind it was created from (via `Item.KindId`). When the system reports "10 units of WDG-001 in Raw Materials zone A1," that means 10 Items whose Kind is WDG-001 are stored there. This mirrors real-world warehousing: you store physical parts, not their drawings.

#### 19a — Storage Locations

A hierarchical location structure (Zone → Aisle → Bay → Bin) gives operators an unambiguous address for every stock item.

**Key entity: `StorageLocation`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Code` | string | Short unique code, e.g. "A1-B3-S2" |
| `Name` | string | Display name |
| `Zone` | string? | Highest-level grouping (e.g. "Raw Materials", "Finished Goods", "Quarantine") |
| `Aisle` | string? | |
| `Bay` | string? | |
| `Bin` | string? | Lowest-level position |
| `ParentId` | Guid? | FK → `StorageLocation` (self-referencing hierarchy) |
| `Description` | string? | |
| `IsActive` | bool | Soft-delete |

---

#### 19b — Inventory Transactions

All stock movements are recorded as immutable transaction events. On-hand quantities are computed by aggregating transactions — no mutable "stock level" field is stored. Every transaction references an **Item** (the physical thing moving) — the Kind is derived from `Item.KindId` at query time.

**Key entity: `InventoryTransaction`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `TransactionType` | enum | `Receipt` / `Issue` / `Transfer` / `Adjustment` / `PicklistConsumption` |
| `ItemId` | Guid | FK → `Item` — the physical item being moved (required; Kind is resolved via `Item.KindId`) |
| `FromLocationId` | Guid? | FK → `StorageLocation` — null for receipts |
| `ToLocationId` | Guid? | FK → `StorageLocation` — null for issues/consumption |
| `Quantity` | decimal | Signed positive value; direction determined by `TransactionType` |
| `ReferenceType` | enum? | `Job` / `PickList` / `ManualAdjustment` |
| `ReferenceId` | Guid? | FK to the referencing entity (JobId, PickListId) |
| `Notes` | string? | |
| `TransactedAt` | DateTime | Server UTC |
| `TransactedByUserId` | string | FK → `ApplicationUser` |

**On-hand view:** `GET /api/warehouse/on-hand?kindId=&locationId=` — joins `InventoryTransaction` → `Item` → `Kind` and aggregates `Quantity` grouped by `Item.KindId` + `ToLocationId` minus outbound transactions. Returned as `OnHandDto` (KindId, KindCode, KindName, LocationCode, LocationName, QuantityOnHand, UnitOfMeasure). The Kind's `UnitOfMeasure` is resolved from the Kind entity at query time, not stored on the transaction.

---

#### 19c — PickLists

When a Job is created from a Process whose steps have input ports referencing specific Kinds, the system auto-generates a `PickList` that identifies the required Items (by Kind) and their available source locations.

**Key entity: `PickList`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `JobId` | Guid | FK → `Job` |
| `Status` | enum | `Open` / `PartiallyPicked` / `Picked` / `Consumed` |
| `GeneratedAt` | DateTime | |
| `GeneratedByUserId` | string | |

**Key entity: `PickListLine`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `PickListId` | Guid | FK → `PickList` |
| `KindId` | Guid | FK → `Kind` — the type of Item required (blueprint reference for matching available Items) |
| `ItemId` | Guid? | FK → `Item` — assigned when operator picks a specific Item from stock; null until picked |
| `SourceLocationId` | Guid? | FK → `StorageLocation` — suggested location with available stock; null if no stock found |
| `RequiredQuantity` | decimal | From process input definition |
| `PickedQuantity` | decimal | Updated when operator confirms pick |
| `ConsumedQuantity` | decimal | Updated by ExecutionWizard close-out |
| `Status` | enum | `Pending` / `Picked` / `ShortShipped` / `Consumed` |
| `Notes` | string? | |

**PickList generation logic (on `POST /api/jobs`):**
1. Resolve all input Kinds declared by the Job's Process steps (via input port `KindId`).
2. For each Kind, query on-hand view to find Items of that Kind in locations with sufficient stock (best-fit: smallest sufficient quantity first).
3. Create `PickListLine` records with the Kind, suggested `SourceLocationId`, and `RequiredQuantity`. The specific `ItemId` is assigned when the operator confirms the pick (since the operator may select a different Item of the same Kind at the shelf).
4. Lines with no available Items are created with `SourceLocationId = null` and flagged `ShortShipped` for planner review.

---

#### 19d — ExecutionWizard Consumption Hook

During the ExecutionWizard close-out phase (Phase 5 of 5), if the Job has an associated `PickList`, the wizard presents the pick lines for the current step and asks the operator to confirm consumed quantities.

- Operator can adjust `ConsumedQuantity` (default = `PickedQuantity`) before confirming.
- On confirmation, the system fires `PicklistConsumption` `InventoryTransaction` records for each line's assigned Item (From = `SourceLocationId`, To = null, Quantity = ConsumedQuantity).
- `PickListLine.Status` → `Consumed`; when all lines are consumed, `PickList.Status` → `Consumed`.
- Short-shipped lines (quantity < required) remain open for manual reconciliation.

---

#### 19e — Warehouse Management UI

**New "Warehouse Management" nav tab** (visible to Admin and Engineer roles):

| Page | Route | Description |
|---|---|---|
| `InventoryDashboard.razor` | `/warehouse` | KPI cards (total locations, total Items on hand, low-stock alerts, recent transactions); on-hand grid by Zone (grouped by Kind); low-stock table (Items below reorder threshold — threshold stored on `Kind`); recent transactions feed |
| `LocationList.razor` | `/warehouse/locations` | Filterable table of all locations with Zone/Aisle/Bay/Bin columns and on-hand Item count badge; create/edit/deactivate modals |
| `LocationDetail.razor` | `/warehouse/locations/{id}` | Header (code, name, hierarchy breadcrumb); on-hand Item grid for this location (grouped by Kind); transaction history table; Manual Adjustment modal; Transfer Out modal |
| `PickListList.razor` | `/picklists` | All picklists with status filter and job link; shortshipped badge |
| `PickListDetail.razor` | `/picklists/{id}` | Header with job link; per-line table (Kind, assigned Item, source location, required/picked/consumed qty, status badge); "Confirm Pick" action per line (assigns specific Item); short-ship override |

**Inventory access from Job pages:**
- `JobDetail.razor` gains a "Picklist" summary card (status badge + line count + "View Picklist →" link) when a picklist exists for the job.

---

#### Key design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Items in locations, not Kinds | Items only | A Kind is a definition (blueprint); an Item is the physical thing. You store physical parts, not drawings. Every Item already has a `KindId` FK, so the Kind is always derivable without redundant storage. |
| Append-only transactions, computed on-hand | Event-sourced | Mutable stock levels are difficult to audit and reconcile; an immutable transaction log gives a full history at no extra cost |
| PickList generated on Job creation | Eagerly | Operators and planners need to know what to pull before execution starts; lazy generation at step time would create picking delays mid-job |
| PickListLine stores KindId + deferred ItemId | Late binding | The line knows *what kind* of material is needed (KindId) at generation time, but the specific Item is assigned at pick time — the operator at the shelf decides which physical unit to pull |
| PickListConsumption fires in ExecutionWizard close-out | Step close-out | Material is consumed when the step is completed — the most accurate timing signal available without custom consumption entry per sub-task |
| Reorder threshold on `Kind` | Extend Phase 1 entity | `Kind` already carries cost and lead-time fields (v3.13); a `ReorderThreshold` and `ReorderQuantity` field fits naturally |
| Transfer transactions independent of jobs | Yes | Item movements between locations (e.g. put-away after goods receipt) must be recordable without a job context |

**New entities:** `StorageLocation`, `InventoryTransaction`, `PickList`, `PickListLine`

**Existing entities extended:** `Kind` gains `ReorderThreshold` (decimal?) and `ReorderQuantity` (decimal?); `Item` gains `StorageLocationId` (Guid? FK — current location, denormalised from latest transaction for quick lookup); `Job` gains `PickListId` FK; `StepExecution` gains optional `PickListLineId` FK

**New enum values:** `InventoryTransactionType` (`Receipt`/`Issue`/`Transfer`/`Adjustment`/`PicklistConsumption`), `PickListStatus` (`Open`/`PartiallyPicked`/`Picked`/`Consumed`), `PickListLineStatus` (`Pending`/`Picked`/`ShortShipped`/`Consumed`), `InventoryReferenceType` (`Job`/`PickList`/`ManualAdjustment`)

**EF migration:** `Phase19_WarehouseManagement`

**MCP tool:** `get_inventory_status` — returns on-hand Item counts grouped by Kind (optional `locationId` filter and `lowStockOnly` flag); includes Kinds below their `ReorderThreshold`

**Status:** Designed — not yet built. Depends on Phase 1 (`Kind` with UnitOfMeasure, v3.13) being complete. ✅

---

### Phase 20 — AI Integration ✅

**Goal:** Transform the MCP server from AI-readable to AI-actionable with write tools, structured responses, audit logging, and webhook events.

**Status:** Implemented 2026-03-28 (version 3.20). See changelog for full details.

---

### Phase 21 — Automatic Inventory Tracking

**Goal:** Let workstations with barcode scanners (USB scanners on PCs or PLCs) call the REST API to move items between locations with a single scan — the scanned barcode identifies the item, the API key identifies the destination.

**Status:** ✅ Implemented 2026-05-04 (version 3.36). See changelog for full details.

**Core principle:** A workstation is a fixed physical point (assembly cell, receiving dock, shipping lane) that has one barcode scanner and one bound storage location. The operator scans an item barcode; the system resolves the item from the barcode, the destination from the workstation's fixed location, and the source from the item's current `StorageLocationId`. One scan = one transfer. No UI interaction required — external clients (PLC software, barcode-scanner apps) consume the REST API directly.

---

#### 21a — API Key Authentication

Long-lived API keys replace JWT for machine-to-machine authentication. Each key is scoped to exactly one workstation, so the key alone determines the caller's identity and fixed location.

**Key entity: `ApiKey`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `KeyHash` | string | SHA-256 hash of the raw key (raw key shown once at creation, never stored) |
| `KeyPrefix` | string(8) | First 8 characters of raw key, stored in plain text for admin identification (e.g. "pk_a3f8...") |
| `Name` | string | Human-readable label (e.g. "Assembly Cell 3 Scanner") |
| `WorkstationId` | Guid | FK → `Workstation` — each key bound to exactly one workstation |
| `CreatedByUserId` | string | FK → `ApplicationUser` — admin who created the key |
| `IsActive` | bool | Soft-revoke toggle |
| `CreatedAt` | DateTime | |
| `LastUsedAt` | DateTime? | Updated on each authenticated request |
| `ExpiresAt` | DateTime? | Null = never expires; if set, key is rejected after this date |

**Authentication flow:**

1. External client sends `X-Api-Key: <raw-key>` header with every request.
2. `ApiKeyAuthenticationHandler` (secondary scheme alongside JWT) hashes the incoming key with SHA-256, queries `ApiKeys` table by hash.
3. If found, active, and not expired: set `ClaimsPrincipal` with claims `workstation_id`, `workstation_code`, `fixed_location_id`, `api_key_id`. Update `LastUsedAt`. Authenticate succeeds.
4. If not found, inactive, or expired: 401 Unauthorized.
5. If `X-Api-Key` header is present, API key auth is used; otherwise JWT auth is used. Both schemes are valid for `[Authorize]` endpoints.

**Admin endpoints (JWT-authenticated, Admin role only):**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/admin/api-keys` | List all API keys (paginated, filterable by workstationId, active) — returns `KeyPrefix` + metadata, never the full key |
| `POST` | `/api/admin/api-keys` | Create new key — returns the raw key **once** in the response body; caller must save it |
| `GET` | `/api/admin/api-keys/{id}` | Get key metadata |
| `PATCH` | `/api/admin/api-keys/{id}` | Update Name, IsActive, ExpiresAt |
| `DELETE` | `/api/admin/api-keys/{id}` | Hard-delete (revokes permanently) |

---

#### 21b — Workstations

A workstation represents a physical scanning station bound to a specific storage location. Scanning at this station means "transfer item TO this location."

**Key entity: `Workstation`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Code` | string | Unique short code (e.g. "WS-ASSY-03", "WS-RECV-01") |
| `Name` | string | Display name (e.g. "Assembly Cell 3") |
| `Description` | string? | |
| `FixedLocationId` | Guid | FK → `StorageLocation` — the location items are transferred TO when scanned |
| `IsActive` | bool | Soft-delete |

**Admin endpoints (JWT-authenticated, Admin role only):**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/admin/workstations` | List workstations (paginated, search by code/name, filter by active) |
| `POST` | `/api/admin/workstations` | Create — validates `FixedLocationId` exists and is active |
| `GET` | `/api/admin/workstations/{id}` | Get with linked location details and API key count |
| `PUT` | `/api/admin/workstations/{id}` | Update (cannot change `Code` after creation) |
| `DELETE` | `/api/admin/workstations/{id}` | Deactivate (soft-delete); rejects if active API keys exist |

---

#### 21c — Barcode Fields

New barcode fields enable lookup-by-scan distinct from existing serial number and location code fields. Barcodes may be vendor-assigned, use different encoding formats (Code 128, QR, Data Matrix), or follow internal labelling schemes.

**Entity extensions:**

| Entity | New Field | Type | Notes |
|---|---|---|---|
| `Item` | `Barcode` | string? | Unique index (nullable). The value physically encoded on the item's label — may differ from `SerialNumber`. If null, scan endpoint falls back to `SerialNumber`. |
| `StorageLocation` | `Barcode` | string? | Unique index (nullable). Encoded on location labels for optional two-scan workflows (future). |
| `Kind` | `Barcode` | string? | Unique index (nullable). UPC/EAN/GTIN for the product type — enables future receiving workflows where a Kind barcode identifies what is being received. |

**Barcode resolution order for the scan endpoint:** The scan endpoint resolves a barcode string to an Item using this precedence:
1. `Item.Barcode` — exact match
2. `Item.SerialNumber` — exact match (fallback for sites that encode serial numbers directly)

If neither matches, the scan fails with a descriptive error logged to `ScanEvent`.

---

#### 21d — Scan Endpoint

The primary new API endpoint. Designed for maximum simplicity — the caller sends one barcode string, and the API key provides all other context.

**Endpoint: `POST /api/warehouse/scan`**

**Authentication:** `X-Api-Key` header required (API key auth scheme).

**Request body:**

| Field | Type | Required | Notes |
|---|---|---|---|
| `barcode` | string | Yes | The scanned barcode value |

**Processing logic:**

1. **Resolve caller context** from API key claims: `workstation_id`, `fixed_location_id`.
2. **Resolve Item** from barcode using the resolution order defined in 21c.
3. **Validate Item state:**
   - Item must exist → 404 `{ error: "unknown_barcode", barcode: "..." }`
   - Item must have status `Available` or `InProcess` → 409 `{ error: "invalid_item_status", status: "Consumed" }`
   - Item's current `StorageLocationId` must not already be the workstation's `FixedLocationId` → 200 no-op with `{ result: "already_at_location" }` (idempotent — not an error, logged as informational)
4. **Create transaction:**
   - `TransactionType` = `Transfer` (or `Receipt` if `Item.StorageLocationId` is null — item has never been located)
   - `FromLocationId` = `Item.StorageLocationId` (null for Receipt)
   - `ToLocationId` = workstation's `FixedLocationId`
   - `Quantity` = 1
   - `ReferenceType` = `Workstation` (new enum value on `InventoryReferenceType`)
   - `ReferenceId` = workstation's Id
   - `TransactedByUserId` = `"apikey:{KeyPrefix}"` — synthetic identifier since API keys have no user identity
5. **Update Item** `StorageLocationId` to workstation's `FixedLocationId`.
6. **Log ScanEvent** (see 21e).
7. **Fire webhook** `inventory.scan` (see 21f).

**Success response (200):**

```json
{
  "result": "transferred",
  "transactionId": "...",
  "item": {
    "id": "...",
    "barcode": "WDG-001-0042",
    "serialNumber": "SN-0042",
    "kindCode": "WDG-001",
    "kindName": "Widget Type A"
  },
  "fromLocation": { "id": "...", "code": "RAW-A1-B3" },
  "toLocation": { "id": "...", "code": "ASSY-03" },
  "workstation": { "id": "...", "code": "WS-ASSY-03" },
  "transactedAt": "2026-03-28T14:30:00Z"
}
```

**Error responses:**

| Status | `error` code | When |
|---|---|---|
| 401 | (standard) | Missing or invalid API key |
| 404 | `unknown_barcode` | Barcode not found in Item.Barcode or Item.SerialNumber |
| 409 | `invalid_item_status` | Item is Consumed, Completed, or Scrapped |
| 400 | `workstation_inactive` | Workstation or its fixed location is deactivated |

---

#### 21e — Scan Event Logging

A dedicated scan event log captures every scan attempt — including failures — for diagnostics, throughput tracking, and integration monitoring. Separate from `InventoryTransaction` (which only records successful movements).

**Key entity: `ScanEvent`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `WorkstationId` | Guid | FK → `Workstation` |
| `ApiKeyId` | Guid | FK → `ApiKey` |
| `ScannedBarcode` | string | The raw barcode string received |
| `ItemId` | Guid? | FK → `Item` — null if barcode was not resolved |
| `TransactionId` | Guid? | FK → `InventoryTransaction` — null if no transaction created |
| `Result` | ScanResult enum | `Transferred` / `AlreadyAtLocation` / `UnknownBarcode` / `InvalidItemStatus` / `WorkstationInactive` / `Error` |
| `ErrorMessage` | string? | Descriptive message for failed scans |
| `ScannedAt` | DateTime | Server UTC |

**No BaseEntity inheritance** — append-only log with own `Id` + `ScannedAt`.

**Query endpoint (JWT-authenticated, Admin/Engineer role):**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/warehouse/scan-events` | Paginated list with filters: `workstationId`, `result`, `dateFrom`, `dateTo`, `barcode` (partial match) |

---

#### 21f — Webhook & MCP Integration

Scan events fire through the existing Phase 20 webhook system. Subscribers can filter on `inventory.scan` or use the wildcard `inventory.*`.

**New webhook event type:** `inventory.scan`

**Payload shape:**

```json
{
  "eventType": "inventory.scan",
  "timestamp": "2026-03-28T14:30:00Z",
  "data": {
    "result": "transferred",
    "scanEventId": "...",
    "transactionId": "...",
    "workstation": { "id": "...", "code": "WS-ASSY-03" },
    "item": { "id": "...", "barcode": "WDG-001-0042", "kindCode": "WDG-001" },
    "fromLocationCode": "RAW-A1-B3",
    "toLocationCode": "ASSY-03"
  }
}
```

Failed scans also fire `inventory.scan` with the `result` field set to the failure reason — subscribers decide whether to act on failures.

**New MCP tool:** `get_workstation_status` — returns all active workstations with their fixed locations, API key count, and last scan time (from latest ScanEvent). Useful for AI assistants monitoring scanner health.

---

#### Key design decisions

| Decision | Choice | Rationale |
|---|---|---|
| API key auth, not JWT | API keys with `X-Api-Key` header | Workstations are unattended machines; JWT refresh is impractical for PLCs and simple scanner apps. API keys are long-lived, revocable, and scoped to a single workstation. |
| Key scoped to workstation, not user | 1:1 ApiKey→Workstation | The key identifies the machine, not the person. Operator identity is not captured at scan time. If operator tracking is needed later, it can be layered via a badge scan. |
| SHA-256 hash, raw key shown once | Hash-only storage | Same pattern as GitHub PATs — raw key never stored. If lost, admin revokes and reissues. KeyPrefix allows identification in admin UI. |
| Separate Barcode from SerialNumber | Item.Barcode nullable unique | Barcodes may be vendor-assigned (GS1, Code 128) and differ from internal serial numbers. Fallback to SerialNumber keeps backward compatibility. |
| Kind.Barcode for future receiving | Nullable, not used by scan endpoint | Enables future "receive by scanning product barcode" workflow without a schema change. |
| ScanEvent separate from InventoryTransaction | Dedicated append-only table | InventoryTransaction only records successful movements. ScanEvent captures failures (unknown barcodes, status errors) which are critical for diagnostics and monitoring scanner health. |
| Idempotent "already at location" | 200 no-op, not an error | Barcode scanners sometimes double-fire. Treating a re-scan as an error would confuse operators. The scan is logged but no transaction is created. |
| Receipt fallback for unlocated items | Transfer if item has location, Receipt if null | Items that have never been warehouse-located get their first location via Receipt, not Transfer. Scan endpoint handles both transparently. |

**New entities:** `ApiKey`, `Workstation`, `ScanEvent`

**Existing entities extended:** `Item.Barcode`, `StorageLocation.Barcode`, `Kind.Barcode` (all string?, unique index)

**New enum values:** `InventoryReferenceType.Workstation`, `ScanResult` enum (6 values)

**EF migration:** `Phase21_AutomaticInventoryTracking`

**Swagger/OpenAPI:** `X-Api-Key` registered as a SecurityDefinition (type: ApiKey, in: Header) alongside existing JWT Bearer definition.

---

### Phase 22 — Factory Design Suite

**Goal:** Provide an all-in-one visual factory layout tool under the Production tab that lets engineers design, configure, and optimise production floor plans — including room geometry, workstation placement and sizing, inventory location placement, utility routing, and intelligent material-flow analysis driven by process input requirements.

**Status:** ✅ Implemented. Depends on Phase 19 (Warehouse / StorageLocation) ✅, Phase 11 (Equipment) ✅, Phase 1 (Kind) ✅, Phase 3 (Process / ProcessStep / Port) ✅, and Phase 12 (OrgUnit) ✅. All dependencies are met.

**Blazor page:** `/factory-design` under the Production NavMenu section.

**Key architectural decisions:**

| Decision | Choice | Rationale |
|---|---|---|
| Rendering engine | HTML5 Canvas via JS interop (custom `factory-canvas.js`) | Three.js is already loaded for 3D model viewer; Canvas gives pixel-level control for 2D top-down rendering with optional 3D perspective toggle. Avoids heavy third-party diagram libraries. |
| Persistence model | JSON-serialised layout documents stored as `FloorPlan` entities | Floor plans are design documents, not transactional data — blob storage with versioning is simpler than normalising every rectangle and line into relational tables. |
| Coordinate system | Metric (millimetres internally, displayed as metres) | Manufacturing floors are measured in metric; mm precision avoids floating-point rounding. |
| Material-flow analysis | Server-side computation via API endpoint | Pathfinding and nearest-location queries involve spatial distance + inventory availability — too complex for client-side. |

---

#### 22a — Data Model

**New entity: `FloorPlan`**

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `Code` | string | Unique identifier (e.g. "FP-ASSY-01") |
| `Name` | string | Display name (e.g. "Assembly Hall — Building 2") |
| `Description` | string? | Purpose, scope |
| `Version` | int | Auto-incrementing on save (optimistic concurrency) |
| `Status` | FloorPlanStatus enum | `Draft` / `Published` / `Archived` |
| `LayoutJson` | string (text) | Full serialised layout document (see schema below) |
| `ThumbnailBase64` | string? | Auto-generated PNG thumbnail for list view |
| `IsActive` | bool | Soft-delete |
| `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` | — | From BaseEntity |

**New entity: `FloorPlanWorkstation`** (junction — links visual placement to domain entities)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `FloorPlanId` | Guid | FK → `FloorPlan` |
| `PlacementId` | string | Matches the `id` field of the workstation element in `LayoutJson` |
| `EquipmentId` | Guid? | FK → `Equipment` — optional link to physical equipment asset |
| `OrgUnitId` | Guid? | FK → `OrgUnit` — optional link to responsible work area |
| `StorageLocationId` | Guid? | FK → `StorageLocation` — the workstation's associated storage location for inventory |

**New entity: `FloorPlanWorkstationProcess`** (processes performed at a workstation)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `FloorPlanWorkstationId` | Guid | FK → `FloorPlanWorkstation` |
| `ProcessId` | Guid | FK → `Process` — a process performed at this workstation |
| `SortOrder` | int | Display ordering |

**New entity: `FloorPlanWorkstationTool`** (tooling/fixtures at a workstation)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `FloorPlanWorkstationId` | Guid | FK → `FloorPlanWorkstation` |
| `KindId` | Guid | FK → `Kind` — the tool/fixture type (e.g. torque wrench, caliper, fixture) |
| `Quantity` | int | How many of this tool at the station (default 1) |
| `Notes` | string? | Calibration ID, location on bench, etc. |

**New entity: `FloorPlanInventoryLocation`** (junction — links visual placement to StorageLocation)

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid | PK |
| `FloorPlanId` | Guid | FK → `FloorPlan` |
| `PlacementId` | string | Matches `id` in `LayoutJson` |
| `StorageLocationId` | Guid | FK → `StorageLocation` |

**New enums:**

- `FloorPlanStatus`: `Draft`, `Published`, `Archived`
- `FloorPlanElementType`: `Room`, `Workstation`, `InventoryLocation`, `UtilityLine`, `Annotation`, `Wall`, `Door`, `Aisle`
- `UtilityType`: `Power`, `Data`, `CompressedAir`, `Water`, `Vacuum`, `Exhaust`, `Gas`

---

#### 22b — LayoutJson Schema

The `LayoutJson` field stores the full visual state of the floor plan. This is the document that the canvas editor reads and writes.

```jsonc
{
  "canvasWidth": 50000,        // mm — total canvas dimensions
  "canvasHeight": 30000,
  "gridSize": 500,             // mm — snap grid increment
  "backgroundColor": "#f5f5f5",
  "elements": [
    {
      "id": "room-1",
      "type": "Room",
      "label": "Assembly Hall",
      "x": 0, "y": 0,         // top-left corner in mm
      "width": 20000,          // mm
      "height": 15000,
      "rotation": 0,           // degrees
      "fill": "#ffffff",
      "stroke": "#333333",
      "strokeWidth": 150,      // wall thickness in mm
      "locked": false,
      "zIndex": 0
    },
    {
      "id": "ws-1",
      "type": "Workstation",
      "label": "Assembly Cell 1",
      "x": 2000, "y": 3000,
      "width": 3000,           // scaled to real dimensions
      "height": 2000,
      "rotation": 0,
      "fill": "#e3f2fd",
      "stroke": "#1565c0",
      "icon": "bi-gear-wide",
      "zIndex": 10
    },
    {
      "id": "inv-1",
      "type": "InventoryLocation",
      "label": "RAW-A1-B3",
      "x": 1000, "y": 1000,
      "width": 2000,
      "height": 1000,
      "fill": "#fff3e0",
      "stroke": "#e65100",
      "icon": "bi-box-seam",
      "zIndex": 10
    },
    {
      "id": "util-1",
      "type": "UtilityLine",
      "utilityType": "Power",
      "points": [              // polyline waypoints in mm
        { "x": 0, "y": 5000 },
        { "x": 10000, "y": 5000 },
        { "x": 10000, "y": 8000 }
      ],
      "stroke": "#f44336",
      "strokeWidth": 50,
      "dashPattern": [],       // solid line; [100, 50] for dashed
      "zIndex": 5
    },
    {
      "id": "annot-1",
      "type": "Annotation",
      "label": "Fire exit →",
      "x": 18000, "y": 14000,
      "fontSize": 200,
      "color": "#d32f2f",
      "zIndex": 20
    }
  ]
}
```

---

#### 22c — API Endpoints

**FloorPlanController** (JWT-authenticated, Admin/Engineer roles):

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/floor-plans` | List all floor plans (paginated, search, filter by status/active) — returns summary DTOs with thumbnail |
| `POST` | `/api/floor-plans` | Create new floor plan with default empty layout |
| `GET` | `/api/floor-plans/{id}` | Get full floor plan including `LayoutJson` and linked workstations/inventory |
| `PUT` | `/api/floor-plans/{id}` | Update metadata (Name, Description, Status) |
| `PUT` | `/api/floor-plans/{id}/layout` | Save layout JSON (auto-increments Version, generates thumbnail server-side) |
| `DELETE` | `/api/floor-plans/{id}` | Soft-delete (set IsActive = false) |
| `POST` | `/api/floor-plans/{id}/publish` | Transition Draft → Published (validates all workstations have at least one Process) |
| `POST` | `/api/floor-plans/{id}/archive` | Transition Published → Archived |

**FloorPlanWorkstationController** (nested under floor plan):

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/floor-plans/{id}/workstations` | List all workstations with their processes and tools |
| `POST` | `/api/floor-plans/{id}/workstations` | Link a visual element to domain entities (EquipmentId, OrgUnitId, StorageLocationId) |
| `PUT` | `/api/floor-plans/{id}/workstations/{wsId}` | Update linkages |
| `DELETE` | `/api/floor-plans/{id}/workstations/{wsId}` | Remove linkage |
| `GET` | `/api/floor-plans/{id}/workstations/{wsId}/processes` | List processes at workstation |
| `POST` | `/api/floor-plans/{id}/workstations/{wsId}/processes` | Add a Process to workstation |
| `DELETE` | `/api/floor-plans/{id}/workstations/{wsId}/processes/{procId}` | Remove a Process |
| `GET` | `/api/floor-plans/{id}/workstations/{wsId}/tools` | List tools (Kinds) at workstation |
| `POST` | `/api/floor-plans/{id}/workstations/{wsId}/tools` | Add a Kind as tooling |
| `PUT` | `/api/floor-plans/{id}/workstations/{wsId}/tools/{toolId}` | Update quantity/notes |
| `DELETE` | `/api/floor-plans/{id}/workstations/{wsId}/tools/{toolId}` | Remove tool |

**FloorPlanInventoryLocationController:**

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/floor-plans/{id}/inventory-locations` | Link visual element to StorageLocation |
| `DELETE` | `/api/floor-plans/{id}/inventory-locations/{locId}` | Remove linkage |

**Material-Flow Analysis Endpoint:**

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/floor-plans/{id}/analyse-material-flow` | Compute nearest inventory source for each workstation's process inputs; returns flow lines with distances |

---

#### 22d — Material-Flow Analysis Engine

When enabled, the analysis engine examines each workstation's assigned processes, extracts the input material ports (Port where Direction = Input and PortType = Material), and finds the nearest inventory location on the floor plan that holds that Kind.

**Algorithm:**

1. For each `FloorPlanWorkstation`:
   a. Collect all assigned `Process` → `ProcessStep` → `StepTemplate` → `Port` where `Direction = Input` and `PortType = Material`.
   b. Extract the unique `KindId` values — these are the materials this workstation needs.
2. For each required `KindId`:
   a. Query all `FloorPlanInventoryLocation` entries on this floor plan.
   b. For each inventory location, check current on-hand quantity for this Kind (via the existing warehouse on-hand aggregation: `Item` where `StorageLocationId` = location and `KindId` = Kind and Status = Available, grouped by quantity).
   c. Filter to locations that have stock > 0 (or optionally include all locations that *could* store this Kind).
   d. Compute Euclidean distance from the workstation's centre point to each candidate location's centre point (using `x + width/2`, `y + height/2` from LayoutJson).
   e. Rank by distance; select nearest.
3. Return a list of `MaterialFlowLine` objects:

```jsonc
{
  "flows": [
    {
      "workstationPlacementId": "ws-1",
      "workstationLabel": "Assembly Cell 1",
      "kindId": "...",
      "kindCode": "WDG-100",
      "kindName": "Widget Body",
      "sourceLocationPlacementId": "inv-1",
      "sourceLocationLabel": "RAW-A1-B3",
      "sourceLocationCode": "RAW-A1-B3",
      "onHandQuantity": 42,
      "distanceMm": 5830,
      "distanceM": 5.83,
      "fromPoint": { "x": 2000, "y": 1500 },
      "toPoint": { "x": 3500, "y": 4000 }
    }
  ],
  "unresolved": [
    {
      "workstationPlacementId": "ws-2",
      "kindId": "...",
      "kindCode": "FST-M6",
      "kindName": "M6 Fastener",
      "reason": "no_inventory_location_with_stock"
    }
  ]
}
```

The Blazor canvas renders these flows as directional arrows (animated dashed lines) from source inventory locations to destination workstations, colour-coded by material type. Unresolved flows are highlighted in red with a warning icon.

---

#### 22e — Blazor UI: Factory Design Suite Page

**Route:** `/factory-design` (list) and `/factory-design/{id}` (editor)

**List page (`FactoryDesignList.razor`):**
- Card grid showing floor plan thumbnails, name, status badge, version, last modified
- Create new, duplicate, archive/delete actions
- Status filter (Draft / Published / Archived)

**Editor page (`FactoryDesignEditor.razor`):**
- `@rendermode InteractiveServer`
- Full-viewport canvas with toolbar, properties panel, and element palette

**Editor layout:**

```
┌──────────────────────────────────────────────────────────────┐
│  Toolbar: Save │ Undo/Redo │ Zoom │ Grid │ Analyse │ Export │
├────────┬─────────────────────────────────────────┬───────────┤
│ Palette│                                         │ Properties│
│        │          Canvas (top-down view)          │   Panel   │
│ Room   │                                         │           │
│ Station│     ┌───────────┐    ┌──────┐           │ Name:___  │
│ Inv Loc│     │ Assembly  │    │ RAW  │           │ Width:___ │
│ Utility│     │  Cell 1   │───→│ A1B3 │           │ Height:___│
│ Wall   │     └───────────┘    └──────┘           │ Equipment │
│ Door   │                                         │ Processes │
│ Annot  │                                         │ Tools     │
│        │                                         │           │
├────────┴─────────────────────────────────────────┴───────────┤
│  Status bar: Zoom 100% │ Grid: 500mm │ Elements: 24 │ v3    │
└──────────────────────────────────────────────────────────────┘
```

**Canvas interactions (JS interop via `factory-canvas.js`):**
- **Drag from palette** to create new elements
- **Click** to select; shows resize handles and rotation grip
- **Drag** to move; snap-to-grid with visual guides
- **Resize** by dragging corner/edge handles; dimensions update in real-time in properties panel
- **Right-click** context menu: duplicate, delete, lock/unlock, bring forward/send back
- **Scroll** to zoom; middle-drag to pan
- **Multi-select** with Shift+click or rectangle marquee
- **Utility lines**: click to place waypoints; double-click to finish polyline
- **Keyboard**: Delete to remove, Ctrl+Z/Y undo/redo, Ctrl+S save, G toggle grid snap

**Properties panel (right sidebar):**
- Changes based on selected element type
- **Room**: Label, dimensions (width × height in metres), wall thickness, fill colour
- **Workstation**: Label, dimensions, rotation; linked Equipment (dropdown from existing Equipment); linked OrgUnit (dropdown); linked StorageLocation (dropdown); Processes list (add/remove from existing Processes); Tools list (add Kind with quantity)
- **Inventory Location**: Label, dimensions; linked StorageLocation (dropdown); shows current on-hand summary
- **Utility Line**: Utility type (dropdown: Power, Data, CompressedAir, Water, Vacuum, Exhaust, Gas); colour auto-assigned by type; line style
- **Annotation**: Text, font size, colour

**Material-flow overlay:**
- Toggle button "Analyse Material Flow" in toolbar
- Calls `POST /api/floor-plans/{id}/analyse-material-flow`
- Renders animated directional arrows on canvas layer
- Shows distance labels on each arrow
- Red warning markers for unresolved materials
- Summary panel listing all flows with distances and quantities

---

#### 22f — JS Interop Module (`factory-canvas.js`)

ES module loaded via Blazor JS interop. Manages the HTML5 Canvas rendering loop and user interactions.

**Exported functions:**

| Function | Description |
|---|---|
| `init(canvasId, dotNetRef)` | Initialise canvas, attach event listeners, accept .NET callback reference |
| `loadLayout(layoutJson)` | Parse and render all elements from saved layout |
| `getLayout()` | Serialise current canvas state back to LayoutJson format |
| `setTool(toolName)` | Switch active tool: `select`, `room`, `workstation`, `inventory`, `utility`, `wall`, `door`, `annotation` |
| `setZoom(level)` | Programmatic zoom (0.25–4.0) |
| `setGridSnap(enabled, size)` | Toggle grid snap and grid size |
| `selectElement(id)` | Programmatically select an element |
| `updateElement(id, propsJson)` | Update element properties from Blazor (e.g. label, dimensions, colour) |
| `deleteSelected()` | Remove selected element(s) |
| `undo()` / `redo()` | Command history |
| `renderFlowOverlay(flowsJson)` | Draw material-flow arrows on overlay layer |
| `clearFlowOverlay()` | Remove flow arrows |
| `exportPng()` | Render canvas to PNG data URL for thumbnail/export |
| `destroy()` | Clean up event listeners, animation frame |

**Callbacks to .NET:**

| Callback | Description |
|---|---|
| `OnElementSelected(id, type)` | Notify Blazor to show properties for selected element |
| `OnElementMoved(id, x, y)` | Position changed (after drag) |
| `OnElementResized(id, w, h)` | Dimensions changed |
| `OnElementCreated(elementJson)` | New element placed on canvas |
| `OnElementDeleted(id)` | Element removed |
| `OnCanvasChanged()` | Any mutation — triggers dirty flag for save prompt |

---

#### 22g — Implementation Steps

| Step | Description | Scope |
|---|---|---|
| **22g-1** | **Domain entities & migration** | `FloorPlan`, `FloorPlanWorkstation`, `FloorPlanWorkstationProcess`, `FloorPlanWorkstationTool`, `FloorPlanInventoryLocation` entities; enums; `Phase22_FactoryDesignSuite` EF migration; DbContext registrations |
| **22g-2** | **DTOs & API controller** | `FloorPlanController` with CRUD + layout save + publish/archive; `FloorPlanWorkstationController` with process/tool management; `FloorPlanInventoryLocationController`; all DTOs in `Phase22Dtos.cs` |
| **22g-3** | **Canvas JS module** | `factory-canvas.js` — rendering engine, element creation/manipulation, grid snap, zoom/pan, undo/redo, selection, event callbacks to .NET |
| **22g-4** | **List page** | `FactoryDesignList.razor` — card grid, status filters, create/duplicate/delete |
| **22g-5** | **Editor page — core canvas** | `FactoryDesignEditor.razor` — toolbar, canvas mount, palette sidebar, element creation via drag, save/load round-trip |
| **22g-6** | **Properties panel** | Right sidebar with per-element-type property editors; Equipment/OrgUnit/StorageLocation/Process/Kind picker dropdowns; real-time dimension editing |
| **22g-7** | **Utility lines** | Polyline drawing tool, utility type colour coding, dash patterns, waypoint editing |
| **22g-8** | **Material-flow analysis** | `POST /api/floor-plans/{id}/analyse-material-flow` endpoint; Euclidean distance computation; on-hand inventory lookup; flow arrow overlay rendering |
| **22g-9** | **MCP tool** | `get_floor_plan_summary` — returns floor plan metadata, workstation count, process assignments, material-flow distances for AI assistant queries |
| **22g-10** | **Integration tests** | FloorPlan CRUD, workstation linkage, process/tool assignment, material-flow analysis with seeded inventory data, layout JSON round-trip validation |

---

#### Key design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Layout stored as JSON blob, not normalised tables | `LayoutJson` text field on `FloorPlan` | Visual layout elements (coordinates, colours, z-order) are tightly coupled and always loaded/saved together. Normalising every rectangle/line into separate tables adds join complexity with no querying benefit. Junction tables (`FloorPlanWorkstation`, `FloorPlanInventoryLocation`) bridge visual elements to domain entities where relational queries are needed. |
| Workstation is a floor-plan concept, not a standalone entity | `FloorPlanWorkstation` junction with optional `EquipmentId` FK | The Phase 21 `Workstation` entity represents a barcode-scanning station; the Factory Design Suite workstation is a visual placement that *may* link to Equipment, OrgUnit, and StorageLocation. Keeping them separate avoids overloading either concept. |
| Material-flow computed on demand, not persisted | API endpoint returns flow lines; not stored in DB | Inventory levels change constantly — persisted flows would be immediately stale. On-demand computation ensures flows always reflect current stock. |
| Canvas via JS interop, not a Blazor component library | Custom `factory-canvas.js` | Blazor's DOM diffing is not suited for high-frequency canvas operations (drag, zoom, pan at 60fps). JS handles rendering; Blazor handles data binding and API calls. Same pattern as the existing Three.js model viewer. |
| Euclidean distance, not pathfinding | Straight-line distance between centre points | Floor plan obstacles (walls, other stations) would require A* pathfinding through a navigation mesh — significant complexity for marginal accuracy improvement. Euclidean distance gives a useful approximation for layout optimisation. Walking-path distance can be added in a future iteration. |

**New entities:** `FloorPlan`, `FloorPlanWorkstation`, `FloorPlanWorkstationProcess`, `FloorPlanWorkstationTool`, `FloorPlanInventoryLocation`

**New enums:** `FloorPlanStatus`, `FloorPlanElementType`, `UtilityType`

**EF migration:** `Phase22_FactoryDesignSuite`

**New JS module:** `wwwroot/js/factory-canvas.js`

**New Blazor pages:** `FactoryDesignList.razor`, `FactoryDesignEditor.razor`

**NavMenu:** Added under Production section as "Factory Design"

**MCP tool:** `get_floor_plan_summary`

---

### Phase 23 — BOM-Aware Process Validation

**Status:** Implemented — extends the existing `GET /api/processes/{processId}/validate` endpoint with Bill-of-Materials coverage checks.

**Motivation.** Phase 21 (type-system work) made Kinds first-class assemblies by giving each Kind a collection of `BomLine`s. Phase 3 (Process composition) independently models step inputs and outputs via typed, quantified Material ports. The two models met but did not cross-check: a released Process could produce an assembly Kind while consuming the wrong components, wrong quantities, or missing components entirely. This phase closes that gap at validation time — before release or execution.

**Rule.** For every distinct effective **output** Kind in a Process that has a non-empty Bill of Materials, the sum of effective **input** port quantities across all steps (matched by `ComponentKindId`) must satisfy every BomLine's required quantity.

**Effective-port resolution.** Each ProcessStep's port values come from the underlying template `Port` merged with any `ProcessStepPortOverride` (null override = keep template default). The validator operates on this merged view, so `KindIdOverride`, `QtyRuleModeOverride`, and `QtyRuleNOverride` all participate correctly.

**How each port contributes to the sum.**

| `QtyRuleMode` | Min contribution | Max contribution |
|---|---|---|
| `Exactly` | `QtyRuleN` | `QtyRuleN` |
| `ZeroOrN` | `0` (conditional) | `QtyRuleN` |
| `Range` | `QtyRuleMin` | `QtyRuleMax` |
| `Unbounded` | `QtyRuleMin` | unbounded (∞) |

Contributions for a given component Kind are accumulated across all Material input ports in the process, yielding a `[totalMin, totalMax?]` interval.

**Emitted diagnostics.**
- **Error** — `Output Kind 'X' requires component 'Y' (qty N) but no input port consumes it.` when no input port matches a BomLine's `ComponentKindId`.
- **Error** — `Inputs for component 'Y' sum to [min..max] which does not cover required BOM quantity Q for output Kind 'X'.` when the required quantity falls outside the aggregated interval.
- **Warning** — `Component 'Y' coverage for 'X' depends on a conditional (ZeroOrN) input port; execution may not deliver the BOM quantity.` when at least one contributing port is `ZeroOrN` and total coverage is only reached if the conditional flow fires.
- **Warning** — `Output Kind 'X' is marked Make but has no Bill of Materials — input coverage cannot be verified.` when a Make-sourced output has no BomLines at all.

**Out of scope for this phase.**
- Multi-level BOM explosion (recursing into a component Kind's own BOM). Component Kinds are treated as leaves even when they are themselves assemblies.
- Parameter / Characteristic / Condition ports — only Material ports participate.
- No UI changes: the existing **Validate** button in `ProcessBuilder.razor` already renders both error and warning lists.

**Key code.**
- Controller: `ProcessesController.AppendBomValidation` + `ResolveEffectivePorts` (private helpers). Called at the end of `Validate` (`src/ProcessManager.Api/Controllers/ProcessesController.cs`).
- Tests: `tests/ProcessManager.Tests/ProcessBomValidationTests.cs` — 11 integration tests covering matching, missing, under-quantity, summed-across-steps, Range coverage, Range under, port override, multi-output BOMs, ZeroOrN warning, no-BOM skip, Make-without-BOM warning.

---

### Phase 24+ — Integrations (future)

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

782 integration tests across all phases. Tests run against an in-memory SQLite database spun up per test run. Test files cover all controllers including Analytics, Alerts, Reports, Document Library filters, SPC, Workstations, API Keys, Suppliers, CAPA, Calibration, and Gage Studies (MSA/GR&R).

### Blazor UI Pages (43 total)

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
| WarehouseDashboard | KPI cards (total locations, items on hand, low-stock count), on-hand inventory table, recent transactions feed |
| LocationList / LocationDetail | Storage location CRUD, zone/active/search filters, on-hand items, child locations, transaction history, manual adjustment/transfer modals |
| PickListList / PickListDetail | Pick list management with status filter, line-level pick confirmation, consume confirmation, short-ship remaining |
| Admin / UserList | User management: add users (with Display Name), edit Display Name + Role, delete users |
| Admin / AiAuditLog | MCP tool call audit trail with date/tool/user/status filters, expandable detail rows showing request payload and response summary |
| Admin / WebhookList | Webhook subscription CRUD, delivery log viewer, test event sender, HMAC secret management |
| Admin / WorkstationList | Workstation CRUD with location picker, inline API key panel (generate/deactivate/delete), scan event log viewer, role-gated (Admin), search and active/inactive filter |
| SpcDashboard / SpcChartDetail | SPC chart list with Cpk badges, chart detail with data points and calculation results |
| FactoryDesignList | Card grid of floor plans with status badges, workstation/location counts, status filter, create modal |
| FactoryDesignEditor | Full-viewport canvas editor with toolbar (save/publish/archive), element palette sidebar (7 element types), HTML5 Canvas rendering via `factory-canvas.js` JS interop, properties panel, status bar (version/counts) |

### Known Limitations / Next Steps

#### Completed phases (all implemented and tested)

- **Phase 8 — Process Maturity & Guided Execution** ✅ ContentCategory enum, NominalValue/IsHardLimit/AcknowledgmentRequired fields, MaturityScoringService (8 rules), maturity badges across list/detail views, NonConformance entity + disposition workflow, 5-phase ExecutionWizard at `/execute/{id}`
- **Phase 9 — Process Change Control & Approval** ✅ ProcessStatus lifecycle (Draft→PendingApproval→Released→Superseded→Retired), ApprovalRecord entity, PFMEA staleness tracking, job-level process version pinning, Submit/Approve/Reject/NewRevision/Retire endpoints, ApprovalQueue page, status badges across all list/detail views, NavMenu pending badge
- **Phase 7c — Control Plan Builder** ✅ ControlPlan + ControlPlanEntry entities, CharacteristicType enum, ControlPlansController (CRUD + entries + CSV export + staleness), EF migration, ControlPlanList/Detail Blazor pages, staleness integration with ProcessesController.Approve, MCP tools `get_control_plan`/`list_critical_characteristics`
- **Phase 10 — Root Cause Analysis & Material Review** ✅ Phase 10a: RootCauseLibrary with 7M categories, typeahead search, UsageCount tracking, MCP tool. Phase 10b+c: Ishikawa diagrams (fishbone card grid, per-category causes, library typeahead) and branching 5 Whys (recursive tree, incomplete-leaf warning, RCA open/close). Phase 10d: MrbReview + MrbParticipant entities, NC escalation, SCAR flag, RCA linkage gate, MrbList/Detail pages, MCP tool
- **Phase 14 — Document Control & QMS** ✅ ProcessRole enum (5 values), DocumentApprovalRequest entity, revision metadata on Process (RevisionCode, ChangeDescription, EffectiveDate, ParentProcessId, ApprovalProcessId), approval-as-process architecture, DocumentList page with submit-for-approval + admin-release modals, Document Library nav section, MCP `list_qms_documents` tool
- **Phase 15 — Tiered Accountability & Action Tracking** ✅ ActionItem entity (two-step close/verify, anti-self-certification), ManagementReview entity (ISO 9001 clause 9.3, auto-populated snapshot inputs), MyActions/TeamActions/QualityScorecard pages, ManagementReviewList/Detail pages, Accountability nav section with overdue badge, MCP `get_management_review_status` tool
- **Phase 16 — Training & Competency Management** ✅ ProcessRole.Training, CompetencyRecord + ProcessTrainingRequirement entities, CompetencyExpiryDays/CompetencyTitle on Process, job-creation enforcement hook, competency matrix view, TrainingList page (launch modal), CompetencyMatrix page, training compliance in QualityScorecard and ManagementReview snapshot, MCP `get_competency_status` tool
- **Phase 2 enhancement — `LongText` and `UserPicker` prompt types** ✅ Both added to PromptType enum; UserPicker renders as Identity-backed user dropdown in ExecutionWizard (originally stored display name, later upgraded to store user Id — see separate entry), used for instructor capture, witness, and handoff signatory; LongText renders as textarea for multi-line instructions
- **Phase 13 (partial) — Seeded content library** ✅ 21 ISO 9001:2015 QMS documents (QMS-001–QMS-021) and 12 system onboarding training courses (TRN-SYS-001–TRN-SYS-012) seeded with full step content; all served as live user documentation via the Document Library and Training Catalogue
- **Phase 11 — Production Management** ✅ EquipmentCategory + Equipment entities (catalog with location, manufacturer, model, serial); DowntimeRecord (Planned/Unplanned, open/close with resolver); MaintenanceTrigger (time/usage-based, advance notice, auto-advance NextDueAt on task completion); MaintenanceTask (lifecycle: Upcoming→Due→Overdue→InProgress→Completed/Cancelled, 4 task types); StepTemplate.ExpectedDurationMinutes + RequiredEquipmentCategoryId; Job.DueDate + PlannedStartDate; StepExecution.EquipmentId; Phase11_ProductionManagement migration; EquipmentController + ProductionController (WIP board, bottlenecks); 4 Blazor pages (ProductionDashboard, EquipmentList, EquipmentDetail, MaintenanceTaskList); Production NavMenu section; 3 MCP tools (get_production_status, list_equipment_downtime, list_overdue_maintenance); MCP v2.1
- **Phase 12 — Workflow Execution & Department Assignment (incl. 12f Participant Portal)** ✅ OrgUnit entity + membership management; AssigneeId on WorkflowProcess with workflow builder UI; GradeBased link routing in ProgressWorkorder; MyWork OrgUnit-based job filtering; WorkflowSchedule background scheduler (6 recurrence types, interval validation, token resolution, EndDate expiry); WorkflowJob execution record (WorkflowNodeStatus enum, NodeStatus-driven WorkorderDetail display); Participant Portal: `ParticipantLayout` + `ParticipantNavMenu` (minimal sidebar), Portal pages (`/portal` redirect, `/portal/my-work`, `/portal/execute/{id}`), `RedirectToPortal` on unauthorized Participant access, OrgUnit membership picker on user edit form in UserList (load memberships, add, remove)
- **Phase 13 — System Content Flag + Copy to My Library** ✅ `IsSystemContent` bool on Process and StepTemplate; Phase13_SystemContent migration; DataSeeder marks all QMS documents, training courses, and shared step templates (DOC-SECT-01, TRN-MOD-01) as system content; ProcessesController + StepTemplatesController Update/Delete return 400 for system content; `POST /api/processes/{id}/copy` deep-clone endpoint (steps, port overrides, content blocks, flows with ID remapping; copy is Draft, not system content); ProcessCopyDto + CopyProcessToMyLibraryAsync ApiClient method; response DTOs extended with IsSystemContent; ProcessList "Library" badge + "Copy to My Library" modal replacing Edit/Delete; ProcessDetail "System Content" badge hiding edit/lifecycle/delete buttons; StepTemplateList "Library" badge + lock icon
- **Phase 2 enhancement — `UserPicker` stores user Id** ✅ UserPicker now stores ASP.NET Identity user Id (not display name) in `ResponseValue`; `ResolvedDisplayName` nullable field added to `PromptResponseDto` for render-time resolution; `StepExecutionsController.GetPromptResponses` batch-resolves user Ids via `_db.Users`; ExecutionWizardContent option value changed to `user.Id`; StepExecutionDetail upgraded from text input to dropdown with legacy display-name fallback
- **Phase 18 — 3D Model Viewer in Process Builder & Execution** ✅ `StepModel` entity (GUID-based file storage for STL/OBJ/GLB/GLTF); `KindModelRefId` optional FK on StepTemplate (inherit Kind's model without re-upload); `Phase18_StepModel` EF migration; StepTemplatesController model upload/download/delete + kind-model-ref endpoints; StepTemplateDetail 3D Model panel (upload/replace/delete, inline Three.js viewer); ProcessBuilder slide view inline viewer; ExecutionWizard Phase 4 collapsible 3D model side panel; 12 integration tests in StepModelTests
- **Phase 19 — Warehouse Management** ✅ `StorageLocation` entity (self-referencing zone/aisle/bay/bin hierarchy); `InventoryTransaction` immutable event log (Receipt/Issue/Transfer/Adjustment/PicklistConsumption); `PickList` + `PickListLine` entities (late-binding ItemId at pick time); `Item.StorageLocationId` + `Kind.ReorderThreshold`/`ReorderQuantity` + `Job.PickListId` extensions; `Phase19_WarehouseManagement` EF migration; `WarehouseController` (10 endpoints: location CRUD, on-hand aggregation, transactions, dashboard, receive-from-job); `PickListsController` (5 endpoints: list, detail, pick, consume, short-ship); Job creation auto-generates PickList from input material ports; ExecutionWizard Phase 5 material consumption hook; 16 ApiClient methods; 5 Blazor pages (WarehouseDashboard, LocationList, LocationDetail, PickListList, PickListDetail); NavMenu Warehouse section; MCP `get_inventory_status` tool; MCP server version 2.2
- **Phase 20 — AI Integration** ✅ 6 MCP write tools (`create_nonconformance`, `create_action_item`, `complete_action_item`, `create_job`, `record_inventory_transaction`, `transition_job`) in partial class `McpController.WriteTools.cs`; `McpAuditLog` append-only entity with Stopwatch/try/finally wrapper on all tool calls; `list_mcp_audit_log` MCP tool + `GET /mcp/audit` REST endpoint; `AiAuditLog.razor` Blazor page with filters and expandable detail rows; structured JSON responses via auto-injected `format` parameter (`markdown` default, `json` returns structured envelope); webhook event system with `WebhookEventQueue` (bounded Channel), `WebhookDeliveryService` (HMAC-SHA256 signing, 3-retry exponential backoff), `WebhooksController` (CRUD + delivery log + test), events fired from all write tools; `WebhookList.razor` at `/webhooks`; `Phase20_AiIntegration` migration; MCP v3.0 with 28 tools total

- **Phase 22 — Factory Design Suite** ✅ `FloorPlan` entity (Code, Name, Version, Status, LayoutJson, ThumbnailBase64); `FloorPlanWorkstation` junction linking visual placements to Equipment/OrgUnit/StorageLocation with `FloorPlanWorkstationProcess` and `FloorPlanWorkstationTool` sub-entities; `FloorPlanInventoryLocation` junction to StorageLocation; `FloorPlanStatus` enum (Draft/Published/Archived); `Phase22_FactoryDesignSuite` EF migration; `FloorPlansController` (CRUD + layout save with version auto-increment + publish/archive lifecycle + workstation process/tool management + inventory location linkage + material-flow analysis via Euclidean distance to nearest stocked inventory location); 25 integration tests; `FactoryDesignList.razor` (card grid, status filter, create modal); `FactoryDesignEditor.razor` (toolbar, palette sidebar, HTML5 Canvas, properties panel, status bar); `factory-canvas.js` ES module (~580 lines: 7 element types, select/draw tools, grid snap, zoom/pan, resize handles, keyboard shortcuts, HiDPI, Blazor JS interop callbacks); 7 ApiClient methods; NavMenu Factory Design under Production

- **Phase 17 — Standards Conformance Management** ✅ `StandardsClause` seed table (37 ISO 9001:2015 + 10 AS9100 Rev D clauses); `ClauseEvidenceLink` many-to-many with auto-linking for QMS documents; `AuditProgram`/`Audit`/`AuditFinding` entities with ActionItem FK for CA tracking; `StandardsClausesController` with dashboard endpoint (coverage aggregation + clause-level heatmap data); `AuditProgramsController` (CRUD, activate/close lifecycle); `AuditsController` (CRUD, start/complete lifecycle, findings CRUD, raise-CA with ActionItem integration, close-finding with CA verification gate); `ConformanceDashboard.razor` (/conformance — KPI cards, clause-coverage heatmap); `ClauseList.razor` (/conformance/clauses — coverage table, detail modal, evidence link management); `AuditProgramList.razor` + `AuditProgramDetail.razor` (/audit-programs — full audit and finding management); MCP `get_conformance_status` tool; 21 integration tests

- **Phase 24 — SPC & Capability Analysis** ✅ `SpcChart` + `SpcDataPoint` entities; `SpcChartType` enum (7 chart types); `ControlLimitSource` enum; `OutOfControlRule` enum (4 Nelson rules); `SpcCalculationService` (A2/D3/D4/d2 constants, X-bar/R chart limits, Cp/Cpk/Pp/Ppk computation, 4 Nelson rules OOC detection); `SpcController` (paginated CRUD, data points, calculate, dashboard); `SpcDashboard.razor` + `SpcChartDetail.razor` Blazor pages; NavMenu SPC Charts link under Quality; MCP `get_spc_status` + `get_process_capability` tools; MCP v3.1 with 30 tools; 25 integration tests

- **Phase 21 — Automatic Inventory Tracking** ✅ `ApiKey` entity (SHA-256 hashed key, KeyPrefix, workstation-scoped, CreatedByUserId, IsActive, ExpiresAt); `Workstation` entity (Code, Name, FixedLocationId FK → StorageLocation); `ScanEvent` append-only entity (not BaseEntity — own Id + ScannedAt, TenantId, WorkstationId, ApiKeyId, ScannedBarcode, ItemId, TransactionId, Result, ErrorMessage); `ScanResult` enum (6 values); `InventoryReferenceType.Workstation`; `Item.Barcode`/`StorageLocation.Barcode`/`Kind.Barcode` unique nullable barcode fields; `Phase17_F7_BarcodeAndScanTables` EF migration; `ApiKeyAuthenticationHandler` (SHA-256 hash authentication, dual-scheme with JWT via combined DefaultPolicy); `WorkstationsController` (Admin CRUD with code uniqueness, location validation, active-key-guarded soft-delete, paginated list, last-scan-time); `ApiKeysController` (Admin CRUD, raw key returned once on creation, PATCH for name/active/expiry, hard-delete); `POST /api/warehouse/scan` (API key auth, barcode→Item resolution with SerialNumber fallback, item status validation, idempotent already-at-location, Transfer/Receipt creation, ScanEvent audit, webhook fire); `GET /api/warehouse/scan-events` (paginated, filtered by workstationId/result/barcode/date range); `WorkstationList.razor` (Admin page at `/admin/workstations` — workstation CRUD with location picker modal, inline expandable API key panel with generate/deactivate/delete, scan event log viewer, search and active/inactive filter, NavMenu link under Admin section); 10 ApiClient methods; MCP `get_workstation_status` tool; MCP v3.2 with 31 tools; 38 integration tests in Phase21Tests + WorkstationAdminTests

- **Phase 27 — CAPA Workflow** ✅ `CapaRecord` entity (Code CAPA-YYYY-NNN, Type Corrective/Preventive, SourceType 7 values, ProblemStatement, ContainmentAction, RCA link, PermanentCorrectiveAction, PreventiveAction, VerificationMethod/DueDate, VerifiedByUserId/At, EffectivenessReviewDate/VerifiedByUserId/At, Status 7-stage lifecycle Open→Containment→RCA→Implementation→Verification→EffectivenessReview→Closed, OwnerUserId/DisplayName, TeamMemberIds, ClosedAt); `CapaStep` entity (StepType, CompletedBy, Notes, AttachmentFileName); `CapaStatus`/`CapaType`/`CapaSourceType` enums; `ActionItemSourceType.Capa` added; `Phase27_CapaWorkflow` EF migration; `CapaController` (paginated list with status/type/sourceType/search filters, CRUD, linear lifecycle transitions with gate checks, verify with anti-self-certification, verify-effectiveness, close with effectiveness-verified gate, link-rca with Ishikawa/FiveWhys validation, steps CRUD, action items CRUD, dashboard with aggregates); NavMenu CAPA link under Quality; MCP `get_capa_status` tool; MCP v3.4 with 33 tools; 31 integration tests in CapaTests

- **Phase 25 — Supplier Quality Management (Basic)** ✅ `Supplier` entity (Code, Name, Status with lifecycle enum Pending/Approved/Conditional/Suspended/Inactive, ContactName/Email/Phone, Address, Notes, ApprovedDate, LastEvaluationDate, IsActive); `SupplierEvaluation` entity (SupplierId, EvaluationDate, QualityScore/DeliveryScore/ResponsivenessScore 0–100, computed OverallScore, EvaluatedByUserId); `SupplierStatus` enum with valid state-machine transitions; `Phase25_SupplierQualityManagement` EF migration; `SuppliersController` (paginated list with search/status/active filters, CRUD, status transition with validation, evaluations CRUD, quality dashboard endpoint with KPIs + top performers + at-risk suppliers); `Phase25Dtos` (SupplierResponseDto/SummaryDto, CreateSupplierDto/UpdateSupplierDto/UpdateSupplierStatusDto, SupplierEvaluationResponseDto/CreateDto, SupplierQualityDashboardDto); `SupplierList.razor` (/suppliers — KPI cards, status/search filter, paginated table, create/edit modals, status transition, evaluations panel); NavMenu Suppliers link under Quality section; ApiClient 9 supplier methods; MCP `get_supplier_quality_status` tool (status breakdown, supplier table with scores, at-risk list); MCP v3.3 with 32 tools; 22 integration tests in SupplierTests

- **Phase 26 — Measurement System Analysis (MSA/GR&R)** ✅ `GageStudy` entity (Name, StudyType enum GRR_Range/GRR_ANOVA/Linearity/Bias/Stability, EquipmentId FK → Equipment, ProcessId FK → Process, CharacteristicName, Tolerance, LSL, USL, NumberOfParts/NumberOfOperators/NumberOfTrials, Status enum Draft/InProgress/Complete, GrrPercent, Ndc, AcceptanceDecision); `GageStudyMeasurement` entity (GageStudyId, PartNumber, OperatorId, TrialNumber, MeasuredValue, unique composite index); `GageStudyType`/`GageStudyStatus` enums; `GrrCalculationService` (Range method with d₂/K1/K2/K3 AIAG constants for subgroup sizes 2–10, EV/AV/GRR/PV/TV decomposition, %GRR of total variation and tolerance, ndc calculation, three-tier assessment); `GageStudyController` (paginated list with status/equipment filters, CRUD, bulk measurement add with bounds validation, auto-transition Draft→InProgress, calculate with completion, dashboard); NavMenu Gage Studies link under Quality; MCP `get_msa_status` tool; MCP v3.6 with 35 tools; 23 integration tests in GageStudyTests

- **Phase 28 — Calibration Management** ✅ `CalibrationRecord` entity (EquipmentId FK → Equipment, CalibrationType enum Internal/External/Vendor, CalibrationDate, NextDueDate, CertificateNumber/FileName, Result enum Pass/Fail/Limited, PerformedBy, StandardsUsed, TemperatureHumidity, AsFound/AsLeftReading, Uncertainty, Notes); `CalibrationSchedule` entity (EquipmentId unique, IntervalDays, IntervalAdjustmentMethod enum Fixed/ReliabilityBased, ConsecutivePassCount, MaxIntervalDays, MinIntervalDays, ExtensionPercent); reliability-based interval adjustment (pass extends by ExtensionPercent capped at MaxIntervalDays, fail resets to MinIntervalDays); `CalibrationResult`/`CalibrationType`/`IntervalAdjustmentMethod` enums; `Phase28_CalibrationManagement` EF migration; `CalibrationController` (records CRUD with equipment/result filters, equipment history, schedules CRUD with duplicate guard, dashboard with due/overdue recall detection); NavMenu Calibration link under Production; MCP `get_calibration_status` tool; MCP v3.5 with 34 tools; 26 integration tests in CalibrationTests

#### Partially built
*(none at this time)*

#### Not yet built
- **Phase 23 — Supplier Quality Management (SQM)** — Extends the existing Phase 10d MRB + SCAR workflow into a full supplier quality programme. `Supplier` entity (Code, Name, ApprovedStatus, QualityRating computed from incoming inspection pass rate and SCAR closure timeliness, CertificateExpiryDate, RiskTier enum Low/Medium/High/Critical); `SupplierCertificate` entity (CertificateType enum ISO9001/AS9100/IATF16949/NADCAP/Custom, IssueDate, ExpiryDate, DocumentFileName); `IncomingInspection` entity linked to `InventoryTransaction.Receipt` (InspectionResult enum Accept/ConditionalAccept/Reject, SamplingPlan, LotSize, SampleSize, DefectsFound); automatic `NonConformance` creation on Reject; automatic SCAR assignment to supplier on recurring rejects (configurable threshold); `SupplierScorecard` aggregation endpoint (acceptance rate, average lead time vs quoted, SCAR count/closure rate, on-time delivery %); `SupplierList.razor` (/suppliers — approved supplier registry with risk tier badges and expiring-cert alerts); `SupplierDetail.razor` (scorecard dashboard, certificate management, incoming inspection history, linked SCARs); `IncomingInspectionList.razor` (/incoming-inspection — lot-by-lot results table with pass/fail trend); MCP `get_supplier_scorecard` tool; MCP `list_supplier_quality_alerts` tool (expiring certs, trending rejects, overdue SCARs). **Market fit:** regulated manufacturers (aerospace, medical, automotive) are required to maintain an Approved Supplier List and demonstrate incoming inspection rigour — this is table-stakes for AS9100 clause 8.4 and IATF 16949 clause 8.4.2 compliance and is a major gap in many QMS-only platforms.
- **Phase 25 — Customer Portal & FAIR/PPAP Document Package** — A read-only external portal for customer quality representatives to view conformance evidence without system access. (moved to Not yet built below)
- **Phase 26 — Measurement System Analysis (MSA/GR&R)** ✅ *(implemented — see completed section above)*
- **Phase 27 — CAPA Workflow** ✅ *(implemented — see completed section above)*
- **Phase 28 — Calibration Management** ✅ *(implemented — see completed section above)*

- **Phase 25 — Customer Portal & FAIR/PPAP Document Package** — A read-only external portal for customer quality representatives to view conformance evidence without system access. `CustomerPortal` entity (CustomerName, ContactEmail, AccessToken hashed, ExpiresAt, AllowedProcessIds scope); `DocumentPackage` entity (ProcessId, PackageType enum FAIR/PPAP/CoC/CustomAuditPack, GeneratedAt, PdfBlobPath, IncludedSections JSON list); `FairGenerator` service producing an AS9102 First Article Inspection Report PDF from process execution data (balloon drawings linked to Control Plan characteristics, measured values from prompt responses, pass/fail disposition, traceability to job/serial/lot); `PpapGenerator` service producing an AIAG PPAP submission package (Process Flow Diagram from process model, PFMEA PDF, Control Plan PDF, dimensional results from SPC/prompt data, material certs from Kind documents, MSA/GR&R placeholder); `CustomerPortalController` (token-based access: GET portal landing, GET allowed processes, GET document packages, GET package PDF download); `CustomerPortalSetup.razor` (Admin page: create portal link, select processes, generate token, set expiry); `DocumentPackageList.razor` (generate FAIR/PPAP packages for a process, view history); integration with Phase 17 conformance data (FAIR can reference clause evidence); integration with Phase 24 SPC data (capability reports included in PPAP). **Market fit:** aerospace (AS9102 FAIR is mandatory for every new part), automotive (PPAP is mandatory for production part approval), and contract manufacturers all need to produce formal quality evidence packages for customers. Today this is a painful manual assembly of spreadsheets, PDFs, and inspection reports — automating it from live system data is a high-value differentiator that directly reduces the cost of new-part qualification.

- **Phase 29 — OEE (Overall Equipment Effectiveness) Dashboard** — Real-time OEE tracking per workstation and equipment asset, computed from existing execution and downtime data. `OeeSnapshot` transient DTO (EquipmentId, ShiftDate, ShiftNumber, AvailabilityPct, PerformancePct, QualityPct, OeePct); `ShiftDefinition` entity (Code, Name, StartTime, EndTime, IsActive — defines shift boundaries for time-bucketing); `OeeCalculationService` computes from existing data: **Availability** = (ShiftDuration − PlannedDowntime − UnplannedDowntime) / ShiftDuration (from `DowntimeRecord` entities), **Performance** = (IdealCycleTime × TotalPiecesProduced) / RunTime (from `StepExecution` durations and `PortTransaction` counts), **Quality** = GoodPieces / TotalPieces (from `NonConformance` scrap/rework counts subtracted from total output); `OeeDashboardController` (GET `/api/oee/dashboard` with date range and equipment filters, GET `/api/oee/trend` for time-series OEE by shift, GET `/api/oee/losses` for Pareto of loss categories); `OeeDashboard.razor` (/oee — live OEE gauge per equipment, 7-day trend sparklines, Pareto chart of top loss categories, drill-down to shift-level breakdown); `ShiftList.razor` (/admin/shifts — shift definition CRUD); integration with Phase 11 equipment/downtime (DowntimeRecord feeds availability calculation); integration with Phase 8c NonConformance (scrap/rework feeds quality calculation); MCP `get_oee_status` tool. **Market fit:** OEE is the universal manufacturing KPI — every lean manufacturing initiative, TPM programme, and continuous improvement effort tracks it. Most manufacturers compute OEE manually in spreadsheets or use dedicated OEE hardware systems (Vorne XL, Redzone) disconnected from their quality system. Embedding OEE into the process/quality platform means availability, performance, and quality losses are traceable to specific root causes, NCRs, and CAPAs — closing the loop between "what happened" and "what do we do about it." This is the #1 feature request from manufacturing operations teams evaluating QMS/MES platforms.

- **Phase 30 — Electronic Batch Record (EBR) & Device History Record (DHR)** — Formal batch-level documentation package for regulated industries (pharma, medical devices, food). `BatchRecord` entity (BatchId FK → Batch, ProcessId, ProductCode, LotNumber, Status enum InProgress/PendingReview/Approved/Released, ReviewedBy, ReviewedAt, ReleasedBy, ReleasedAt, DeviationCount); `BatchRecordSection` entity (BatchRecordId, SectionType enum MaterialVerification/InProcessChecks/EnvironmentalMonitoring/YieldReconciliation/PackagingVerification/FinalRelease, Data JSON, SignedBy, SignedAt); auto-population: when a batch-tracked Job completes, the system generates a `BatchRecord` from all execution data (prompt responses, port transactions, non-conformances, deviations, operator sign-offs, environmental readings); `BatchRecordController` (GET list with status filter, GET detail with all sections, POST review with electronic signature, POST release with dual-signature requirement, GET PDF export with 21 CFR Part 11 compliant audit trail); `BatchRecordList.razor` (/batch-records — pending review queue with deviation count badges, released batches archive); `BatchRecordDetail.razor` (/batch-records/{id} — section-by-section review with approve/reject per section, yield reconciliation calculator, deviation summary, electronic signature capture); `DhrGenerator` service producing an FDA-compliant Device History Record PDF from batch record data; integration with Phase 19 warehouse (material lot traceability from receipt through consumption); integration with Phase 8d ExecutionWizard (prompt responses feed batch record sections). **Market fit:** FDA 21 CFR 211 (pharma) and 21 CFR 820.184 (medical devices) require complete batch/device history records for every production lot. Most manufacturers maintain these as paper binders or disconnected electronic forms — the #1 FDA 483 observation category is incomplete or missing batch records. Automating EBR/DHR from live execution data eliminates this compliance risk and reduces batch release cycle time from days to hours.

- **Phase 31 — Predictive Quality Analytics & ML-Driven Alerts** — Machine learning layer that analyses historical execution data to predict quality issues before they occur. `PredictiveModel` entity (Id, Name, ModelType enum DriftDetection/FailurePrediction/AnomalyDetection, TargetContentBlockId FK → StepTemplateContent, TrainingDataPoints int, LastTrainedAt, ModelBlobPath, IsActive, AccuracyMetric decimal); `PredictiveAlert` entity (ModelId, StepExecutionId, AlertType enum DriftWarning/FailureProbability/Anomaly, Confidence decimal 0–1, PredictedValue decimal, ActualValue decimal, Acknowledged bool); `DriftDetectionService` (monitors running mean and variance of NumericEntry prompt responses over a sliding window, fires alert when drift exceeds configurable sigma threshold — catches process drift before SPC control limits are breached); `FailurePredictionService` (logistic regression trained on historical prompt response vectors → NonConformance outcomes, predicts probability of NC for the current execution based on upstream prompt responses already captured); `AnomalyDetectionService` (isolation forest or z-score ensemble on multivariate prompt response profiles, flags executions whose measurement pattern is statistically unusual even if individual values are in-spec); `PredictiveController` (GET `/api/predictive/models` CRUD, POST `/api/predictive/models/{id}/train` triggers background training job, GET `/api/predictive/alerts` paginated with confidence threshold filter, POST `/api/predictive/alerts/{id}/acknowledge`); `PredictiveAlertsDashboard.razor` (/predictive — active model summary cards, alert feed with confidence badges, trend charts showing predicted vs actual, model accuracy over time); integration with Phase 24 SPC (drift detection supplements Nelson rules with earlier warning); integration with Phase 8d ExecutionWizard (inline warning banner when failure prediction exceeds threshold during active execution); MCP `get_predictive_alerts` tool. **Market fit:** traditional SPC is reactive — it detects out-of-control conditions after they happen. Industry 4.0 and smart manufacturing initiatives demand predictive quality: catching drift and predicting failures before scrap is produced. This differentiates the platform from both legacy QMS tools (which have no analytics) and standalone SPC packages (which lack the process context to build predictive models). Manufacturers investing in digital transformation specifically seek platforms that offer "quality intelligence" — actionable predictions derived from their own production data, not just charts of what already went wrong.

- **Phase 32 — Change Management & Engineering Change Order (ECO) Workflow** — Formal change control process linking process/document revisions to a structured approval workflow with impact analysis. `ChangeOrder` entity (Code auto-generated ECO-YYYY-NNN, Type enum DesignChange/ProcessChange/DocumentChange/SupplierChange/DeviationRequest, Priority enum Routine/Urgent/Emergency, Status enum Draft/ImpactAnalysis/Approval/Implementation/Verification/Closed/Rejected, Title, Description, Justification, RequestedByUserId, RequestedAt, TargetImplementationDate); `ChangeOrderImpact` entity (ChangeOrderId, AffectedEntityType enum Process/StepTemplate/ControlPlan/Pfmea/Kind/Supplier/Equipment, AffectedEntityId, ImpactDescription, MitigationPlan); `ChangeOrderApprover` entity (ChangeOrderId, UserId, Role, Decision enum Pending/Approved/Rejected/Abstained, DecidedAt, Comments); `ChangeOrderTask` entity (ChangeOrderId, Title, AssigneeId, DueDate, Status, CompletedAt — implementation tasks); auto-impact detection: when a Process with active Jobs is modified, system requires an ECO; Process revision creation (`NewRevision` action) optionally links to an ECO for traceability; Control Plan and PFMEA staleness trigger ECO recommendations; `ChangeOrderController` (CRUD, lifecycle transitions, impact items CRUD, approver management, tasks CRUD, bulk-approve for emergency changes, dashboard with open ECOs by status/priority/age); `ChangeOrderList.razor` (/change-orders — filterable table with priority badges, ECO status timeline); `ChangeOrderDetail.razor` (/change-orders/{id} — impact analysis panel, approver matrix with decision status, implementation tasks checklist, linked revisions); integration with Phase 9 change control (Process lifecycle transitions reference ECO); integration with Phase 15 action items (implementation tasks are ActionItems); MCP `get_change_order_status` tool; MCP `create_change_order` write tool. **Market fit:** AS9100 Rev D clause 8.5.6 and IATF 16949 clause 8.5.6 mandate formal change management for any modification to product or process. Most manufacturers run change control in Excel, SharePoint, or email — leading to unapproved changes reaching the shop floor, incomplete impact analysis, and audit nonconformances. An integrated ECO workflow that automatically identifies affected artifacts (control plans, PFMEAs, supplier requirements) and routes approvals to the right stakeholders eliminates the most common cause of AS9100/IATF audit major findings. This is consistently cited as the #1 gap in lightweight QMS platforms vs. enterprise PLM systems like Windchill or Teamcenter.

- **Phase 33 — Real-Time Shop Floor Display & Andon System** — Large-format display mode showing live production status, designed for wall-mounted monitors at workstations and production meeting areas. `DisplayLayout` entity (Code, Name, Type enum ProductionBoard/AndonBoard/QualityBoard/KpiBoard, RefreshIntervalSeconds, LayoutConfig JSON defining widget placement); `AndonEvent` entity (WorkstationId or EquipmentId, EventType enum Help/Quality/Material/Maintenance/Safety, Status enum Active/Acknowledged/Resolved, RaisedByUserId, RaisedAt, AcknowledgedByUserId, AcknowledgedAt, ResolvedAt, ResponseTimeSeconds computed); `AndonController` (raise event via simple POST suitable for physical Andon button integration, acknowledge, resolve, active events list, response time analytics); `DisplayController` (GET `/api/displays/{code}/data` returns computed widget data for a display layout, suitable for polling or SSE; layouts CRUD); `ShopFloorDisplay.razor` (/display/{code} — full-screen kiosk mode with auto-refresh, no sidebar/nav, large typography, color-coded status, auto-cycling between configured widgets); display widgets: WIP board (from Phase 11 production data), Andon status (active calls with escalation timer), OEE gauges (from Phase 29 when built, placeholder until then), quality alerts (open NCs and SPC OOC), schedule adherence (jobs on-time vs late), shift production counter; `AndonBoard.razor` (/andon — Andon event management for supervisors: active events, response metrics, raise/acknowledge/resolve); `DisplayAdmin.razor` (/admin/displays — layout configuration, widget selection, assignment to physical screens); physical integration: REST API compatible with Raspberry Pi kiosk browsers, industrial PCs, and Android tablets; `AndonEventDto`, `RaiseAndonEventDto`, `DisplayLayoutDto`; MCP `get_andon_status` tool. **Market fit:** every lean manufacturing facility has visual management boards — traditionally whiteboards or expensive proprietary Andon systems (Werma, Banner Engineering, FactoryTalk). Modern manufacturers want digital boards driven by live data, not manually updated. The Andon response-time metric is a key lean KPI tracked in Toyota Production System implementations. Providing this as a built-in feature (rather than requiring a separate manufacturing intelligence platform like Tulip or L2L) dramatically reduces the total cost of digital shop floor transformation. This is the feature that makes the platform visible to every person on the factory floor, not just engineers at desks.

- **Phase 34 — Customer Complaint Management & Voice of Customer (VoC) Integration** — Structured customer complaint intake, investigation, and resolution workflow with trending analysis. `CustomerComplaint` entity (Code auto-generated CC-YYYY-NNN, CustomerName, CustomerReference, ProductCode FK → Kind, LotNumber, ComplaintDate, ReceivedDate, Category enum ProductDefect/Packaging/Delivery/Documentation/Service/Regulatory, Severity enum Critical/Major/Minor/Cosmetic, Description, QuantityAffected, Status enum New/UnderInvestigation/ContainmentInPlace/RootCauseIdentified/CorrectiveActionImplemented/ResponseSent/Closed, OwnerId FK → User, ResponseDueDate, ResponseSentAt, CustomerSatisfied bool?); `ComplaintInvestigation` entity (ComplaintId, InvestigationType enum InitialAssessment/LabAnalysis/ProcessReview/SupplierInvestigation, Findings, InvestigatedBy, InvestigatedAt); `ComplaintResponse` entity (ComplaintId, ResponseType enum Acknowledgment/InterimUpdate/FinalResponse/CreditNote/Replacement, Content, SentBy, SentAt); auto-linkage: complaint can generate NonConformance (Phase 8c), trigger CAPA (Phase 27 when built), link to Supplier (Phase 25), and create ActionItems (Phase 15); `CustomerComplaintController` (CRUD, lifecycle transitions, investigations CRUD, responses CRUD, trending endpoint with Pareto by category/product/customer, repeat-complaint detection, regulatory-complaint flag for FDA MDR/MedWatch reporting); `ComplaintList.razor` (/complaints — priority queue with severity badges, response-due countdown, aging indicators); `ComplaintDetail.razor` (/complaints/{id} — investigation timeline, linked NCRs/CAPAs, response drafting, customer satisfaction tracking); `ComplaintDashboard.razor` (/complaints/dashboard — KPI cards: open/overdue/avg-days-to-close/customer-satisfaction-rate, Pareto chart of top complaint categories, repeat complaint trending, product-level complaint rate heat map); integration with Phase 25 Supplier (supplier-caused complaints escalate to supplier quality); 8D report generation from complaint data; MCP `get_complaint_status` tool; MCP `create_complaint` write tool. **Market fit:** ISO 9001 clause 9.1.2 requires organizations to monitor customer satisfaction and handle complaints systematically. FDA 21 CFR 820.198 mandates complaint files for medical devices with specific investigation and trending requirements. IATF 16949 clause 10.2.5 requires warranty management and complaint analysis. Most manufacturers track complaints in CRM systems (Salesforce, HubSpot) completely disconnected from their quality system — meaning investigation findings don't flow back into process improvement, and the same failure mode generates repeated complaints without triggering CAPA. An integrated complaint-to-CAPA-to-process-improvement pipeline is the defining feature of a mature quality system and the key differentiator between companies that genuinely improve and those that repeatedly firefight the same issues.

- **Phase 35 — Cost of Quality (CoQ) Tracking & Financial Impact Analysis** — Attaches financial costs to quality events (scrap, rework, warranty, inspection) and provides executive-level reporting on quality economics. `QualityCost` entity (SourceType enum Scrap/Rework/Warranty/InspectionLabor/ExternalFailure/PreventionCost/AppraisalCost, SourceEntityId FK → NonConformance/CapaRecord/CustomerComplaint/MaintenanceTask, Amount decimal, Currency string, CostCategory enum Prevention/Appraisal/InternalFailure/ExternalFailure per PAF model, KindId FK → Kind optional for product-level attribution, JobId FK → Job optional, Description, RecordedByUserId, RecordedAt); `QualityCostRule` entity (TriggerEvent enum NcCreated/NcScrapped/NcReworked/CapaOpened/ComplaintReceived/InspectionPerformed, DefaultCostFormula text — e.g. `quantity * unit_cost` or fixed amount, IsActive); auto-cost creation: when a NonConformance disposition is set to Scrap, system auto-creates a QualityCost entry using the Kind's unit cost × quantity affected; rework costs computed from step execution duration × labor rate; `QualityCostController` (CRUD, manual cost entry, auto-cost configuration, CoQ dashboard endpoint with PAF breakdown, trend over time, cost-per-unit-shipped, top cost drivers Pareto, product-level CoQ ranking); `CoqDashboard.razor` (/quality-costs — executive KPI cards: total CoQ this month/quarter/year, CoQ as % of revenue configurable target, PAF category breakdown donut chart, 12-month trend line, top 10 cost drivers by Kind/Process/FailureMode, cost per unit shipped trend); `CoqDetail.razor` (/quality-costs/entries — individual cost entries with filters by category/source/product/date, manual entry for costs not captured automatically); `QualityCostRules.razor` (/admin/quality-cost-rules — configure auto-cost triggers and default amounts/formulas); integration with Phase 8c NonConformance (scrap/rework costs); integration with Phase 27 CAPA (CAPA implementation costs); integration with Phase 34 complaints (warranty and external failure costs); integration with Phase 15 Management Review (CoQ summary as management review input); MCP `get_cost_of_quality` tool (PAF breakdown, trend, top drivers). **Market fit:** Cost of Quality is the language executives understand — while engineers focus on PPM and Cpk, leadership makes investment decisions based on the financial impact of poor quality. ISO 9004 recommends CoQ analysis, and IATF 16949 clause 6.1.2.1 specifically requires the organization to include warranty cost in quality objectives. The #1 challenge quality managers face is justifying improvement investments — CoQ data provides the ROI calculation. Most QMS platforms track quality events but not their financial impact, forcing quality managers to maintain separate spreadsheets for management presentations. Embedding CoQ into the platform means every NCR, CAPA, and complaint automatically accumulates financial evidence for improvement business cases.

- **Phase 36 — Skill-Based Work Assignment & Intelligent Scheduling** — Matches operator competencies, certifications, and availability to workstation/process requirements for optimal job routing. `OperatorAvailability` entity (UserId, ShiftDefinitionId FK → ShiftDefinition, Date, AvailabilityType enum Available/PTO/Training/Modified, StartOverride TimeOnly?, EndOverride TimeOnly?); `ProcessSkillRequirement` entity (ProcessId FK → Process, SkillType enum Certification/Training/Experience/PhysicalCapability, SkillCode FK → CompetencyRecord or custom, MinimumLevel enum Awareness/Competent/Proficient/Expert, IsHardRequirement bool — hard requirements block assignment, soft requirements generate warnings); `WorkAssignment` entity (JobId FK → Job, StepExecutionId FK → StepExecution, AssignedUserId, AssignedAt, AssignedByMethod enum Manual/AutoBestFit/AutoRoundRobin/AutoLeastLoaded, Score decimal — fitness score explaining why this operator was chosen, OverrideReason text — if supervisor overrode auto-assignment); `AssignmentEngine` service (scores candidate operators by: competency match weighted × proficiency level, workstation proximity from floor plan, current workload balance, recent error rate on this process, shift availability; returns ranked list with explanation); `CapacityView` DTO (per-shift per-workstation view: required competencies × available qualified operators × planned job demand → capacity sufficiency indicator with bottleneck identification); `WorkAssignmentController` (GET `/api/assignments/suggest` returns ranked operator list for a job with fitness scores, POST `/api/assignments` create assignment, GET `/api/capacity` shift-level capacity view, GET `/api/capacity/gaps` identifies understaffed shifts/skills); `WorkAssignment.razor` (/assignments — supervisor assignment board: unassigned jobs queue, operator availability grid, drag-and-drop or auto-assign, competency-mismatch warnings); `CapacityPlanner.razor` (/capacity — weekly/monthly capacity calendar with color-coded sufficiency, drill-down to skill gaps, training needs identification); integration with Phase 16 Training (expired competencies block assignment); integration with Phase 12 OrgUnit (team membership defines assignment pool); integration with Phase 22 Factory Design (workstation proximity weighting); integration with Phase 11 Production (scheduling constraints from equipment availability); MCP `get_capacity_gaps` tool. **Market fit:** manufacturing scheduling is the #1 operational pain point — supervisors spend hours daily matching operators to workstations based on tribal knowledge of who is trained for what. When a key operator calls in sick, the scramble to find qualified replacements delays production. Workforce management in manufacturing is currently handled by paper skill matrices and supervisor memory. Skill-based auto-routing is the bridge between MES (which tracks what happens) and APS (which plans what should happen) — it's the feature that turns the platform from a quality/compliance tool into an operational intelligence system. Companies with 50+ operators cite this as their #1 need when evaluating digital manufacturing platforms.

- **Phase 37 — Process Digital Twin & What-If Simulation** — Monte Carlo simulation engine that models process variability and predicts quality/throughput outcomes for proposed changes before implementation. `SimulationScenario` entity (Name, Description, BaseProcessId FK → Process, Status enum Draft/Running/Complete/Failed, CreatedByUserId, CreatedAt, CompletedAt, Iterations int default 10000); `SimulationOverride` entity (ScenarioId, TargetType enum StepDuration/PromptValue/RejectRate/EquipmentDowntime/OperatorSpeed, TargetStepId FK → ProcessStep optional, Distribution enum Normal/Uniform/Triangular/Exponential/Historical, Param1 decimal — mean or min, Param2 decimal — stddev or max, Param3 decimal — mode for triangular); `SimulationResult` entity (ScenarioId, MetricType enum Throughput/CycleTime/FirstPassYield/Cpk/WipLevel/BottleneckUtilization/CostPerUnit, P5 decimal, P25 decimal, Median decimal, P75 decimal, P95 decimal, Mean decimal, StdDev decimal); `SimulationStepResult` entity (ScenarioId, ProcessStepId, AvgDuration decimal, UtilizationPct decimal, QueueTimePct decimal, RejectRatePct decimal); `SimulationEngine` service (discrete-event Monte Carlo: models each process step as a stochastic activity with configurable distributions derived from historical prompt response data and step execution durations; models serial/parallel step flows from process graph; models reject/rework loops from historical NC rates; models equipment downtime from Phase 11 DowntimeRecord distributions; outputs throughput, cycle time, yield, WIP, and per-step utilization distributions); `HistoricalDistributionFitter` service (fits Normal/Lognormal/Weibull to historical execution data for a given process, returns best-fit distribution parameters — used as defaults for simulation overrides); `SimulationController` (CRUD scenarios, add overrides, run simulation as background job, get results, compare two scenarios side-by-side); `SimulationList.razor` (/simulations — scenario cards with status badges, create from process with auto-fitted distributions, clone for variant comparison); `SimulationDetail.razor` (/simulations/{id} — override editor per step with distribution visualizer, run button, results dashboard with histograms and confidence intervals, step-level utilization heatmap, comparison mode with baseline vs proposed change highlighting); integration with Phase 32 ECO (change order impact analysis can run simulation to predict quality/throughput effect of proposed change); integration with Phase 29 OEE (simulation results predict OEE impact of changes); integration with Phase 24 SPC (historical Cpk data feeds simulation distributions); MCP `run_simulation` tool (create and execute a quick scenario, return key metrics). **Market fit:** process engineers today make changes based on experience and hope — they tighten a tolerance, add an inspection step, or change a supplier, then wait weeks to see if quality improved or throughput dropped. Digital twin simulation is the killer feature of Industry 4.0 that lets engineers predict outcomes before committing changes to the shop floor. Enterprise platforms (Siemens Tecnomatix, Dassault DELMIA) offer this but cost $100K+ per seat and require dedicated simulation engineers. Providing lightweight what-if simulation embedded in the process model — using the system's own historical data for distribution fitting — democratizes simulation for the 95% of manufacturers who can't afford enterprise digital twin platforms. This is the feature that transforms the platform from "quality system that records what happened" to "operations intelligence platform that predicts what will happen."

- **Phase 38 — Environmental Monitoring & Clean Room Compliance** — Continuous environmental condition tracking for temperature, humidity, particulate counts, and differential pressure — critical for medical device, semiconductor, and pharmaceutical manufacturing. `EnvironmentalSensor` entity (Code, Name, SensorType enum Temperature/Humidity/Particulate/DifferentialPressure/Vibration, LocationId FK → StorageLocation, EquipmentId FK → Equipment optional, Unit, LSL, USL, IsActive, PollingIntervalSeconds); `EnvironmentalReading` append-only entity (SensorId, Value, ReadingAt, IsOutOfSpec computed, AcknowledgedByUserId, AcknowledgedAt); `CleanRoomClass` entity (Name, Standard enum ISO14644/FedStd209E, Class string, MaxParticleCount per size, TemperatureRange, HumidityRange); `EnvironmentalExcursion` entity (SensorId, StartedAt, EndedAt, PeakValue, Duration, Status enum Active/Acknowledged/Investigated/Closed, LinkedNonConformanceId, LinkedCapaId); auto-excursion detection: when a reading exceeds sensor LSL/USL, an excursion opens and optionally auto-creates a NonConformance (configurable per sensor); auto-close when readings return in-spec; `EnvironmentalController` (sensor CRUD, readings ingestion endpoint suitable for IoT gateway POST, readings query with time range, excursion list with status filter, dashboard with real-time sensor grid, configurable alerting thresholds); `EnvironmentalDashboard.razor` (/environmental — live sensor status grid with green/amber/red indicators, 24h sparkline per sensor, active excursion alerts, clean room compliance status per area); `SensorDetail.razor` (/environmental/sensors/{id} — readings time-series chart, excursion history, configuration); IoT integration: REST API compatible with MQTT gateways (AWS IoT, Azure IoT Hub) via simple HTTP POST with API key auth; integration with Phase 19 Warehouse (monitor storage conditions for material with shelf-life requirements); integration with Phase 30 EBR (environmental readings as batch record evidence); integration with Phase 17 Conformance (links to ISO 14644 clause evidence); MCP `get_environmental_status` tool (sensor summary, active excursions, out-of-spec alerts). **Market fit:** FDA 21 CFR 211.46 (pharma), ISO 14644 (clean rooms), and ISO 13485 (medical devices) mandate documented environmental monitoring with defined action limits. Most manufacturers use standalone data loggers (Vaisala, Rotronic, Dickson) that produce paper charts or CSVs disconnected from their quality system — when an excursion occurs, the link between environmental deviation and affected product batches is manual and error-prone. Embedding environmental monitoring into the process/quality platform creates automatic traceability from environmental excursions to affected batches, NCRs, and CAPAs. This is a high-value differentiator for any manufacturer in a regulated clean room environment, where a missed excursion can result in full lot rejection or FDA warning letters.

- **Phase 39 — Layered Process Audit (LPA) & Gemba Walk Digitization** — Structured observation-based audit system where leaders at all levels verify process adherence at the point of work. `LpaChecklist` entity (ProcessId FK → Process, Name, LayerLevel enum Operator/Supervisor/Manager/Director, Frequency enum PerShift/Daily/Weekly/Monthly, Questions JSON array of {question, expectedAnswer, isHighImpact}, IsActive); `LpaAudit` entity (ChecklistId, AuditorUserId, AuditedAreaId FK → OrgUnit, ScheduledDate, CompletedAt, Status enum Scheduled/InProgress/Complete/Missed, OverallResult enum Conforming/NonConforming/NotApplicable); `LpaResponse` entity (AuditId, QuestionIndex, Response enum Conforming/NonConforming/NA, Notes, PhotoFileName, LinkedActionItemId); auto-scheduling: system generates LPA audit assignments based on checklist frequency and layer level, distributes across qualified auditors in the OrgUnit, tracks completion rate; missed audit escalation: if an audit passes its scheduled date without completion, system creates escalation notification and marks as Missed; trend detection: repeated non-conformances on the same question across multiple audits auto-trigger a CAPA recommendation; `LpaController` (checklist CRUD, audit schedule generation, audit execution with response capture, layered completion dashboard, non-conformance trending by question/area/process, missed audit tracking); `LpaDashboard.razor` (/lpa — completion rate gauges per layer, this-week audit schedule with status, non-conformance heat map by area, trending issues, missed audit alerts); `LpaAuditForm.razor` (/lpa/audit/{id} — mobile-optimized checklist execution with one-tap conform/non-conform, photo capture for evidence, auto-create action items from non-conformances); `LpaAdmin.razor` (/admin/lpa — checklist builder, frequency/layer configuration, auditor pool management); integration with Phase 12 OrgUnit (audit assignments flow through department hierarchy); integration with Phase 27 CAPA (trending non-conformances trigger CAPA); integration with Phase 15 Action Items (non-conforming responses create action items); integration with Phase 33 Shop Floor Display (LPA completion status as display widget); MCP `get_lpa_status` tool (completion rates by layer, trending non-conformances, missed audits). **Market fit:** Layered Process Audits are a cornerstone of IATF 16949 (clause 9.2.2.4 requires manufacturing process audits) and are mandated by virtually every automotive OEM's customer-specific requirements (Ford, GM, Stellantis, Toyota all require documented LPA programs). The Gemba walk — leaders observing actual work at the point of production — is fundamental to lean manufacturing and Toyota Production System. Currently, 80%+ of manufacturers run LPA programs on paper clipboards or generic checklist apps (iAuditor, GoAuditor) completely disconnected from their quality system. This means non-conformances found during Gemba walks never flow into the corrective action system, and trending analysis is impossible. Digitizing LPA within the quality platform creates the direct link from observation → action item → CAPA → process improvement that lean manufacturing demands. This is the feature that demonstrates daily management discipline to automotive OEM auditors — "show me your last 30 days of LPA data" is a standard audit request.

- **Phase 40 — Risk Register & FMEA-Driven Risk Management** — Unified organizational risk register that aggregates risk signals from across the platform (PFMEA RPNs, SPC out-of-control events, overdue CAPAs, supplier quality degradation, environmental excursions) into a single prioritized view for management decision-making. `RiskRegister` entity (Code auto-generated RISK-NNN, Title, Description, Category enum ProcessRisk/ProductRisk/SupplierRisk/ComplianceRisk/EquipmentRisk/EnvironmentalRisk, Likelihood enum Rare/Unlikely/Possible/Likely/AlmostCertain with score 1–5, Consequence enum Negligible/Minor/Moderate/Major/Catastrophic with score 1–5, InherentRiskScore computed Likelihood×Consequence, MitigationPlan, ResidualLikelihood, ResidualConsequence, ResidualRiskScore computed, Status enum Open/Mitigated/Accepted/Closed, OwnerId FK → User, ReviewDueDate, LastReviewedAt, SourceType enum Manual/PfmeaHighRPN/SpcOOC/CapaRecurring/SupplierDegradation/CalibrationOverdue/EnvironmentalExcursion, SourceEntityId optional); `RiskMitigationAction` entity (RiskRegisterId, ActionItemId FK → ActionItem, TargetResidualScore, VerifiedEffective bool); `RiskAutoDetectionService` (BackgroundService that periodically scans for: PFMEA failure modes with RPN > configurable threshold and no completed action → creates ProcessRisk; SPC charts with multiple OOC events in 7 days → creates ProcessRisk; same root cause appearing in 3+ CAPAs → creates recurring risk; supplier evaluation score dropping below threshold → creates SupplierRisk; calibration overdue > 30 days → creates EquipmentRisk; creates draft risks for human review, never auto-closes); `RiskScoringMatrix` configurable 5×5 risk matrix with organization-defined color thresholds (Low/Medium/High/Extreme); `RiskController` (CRUD, risk assessment with likelihood/consequence update, add mitigation actions, close with residual score, auto-generated risks queue for review, aggregate dashboard with risk heatmap, top-10 open risks, risk trending over time); `RiskDashboard.razor` (/risks — interactive 5×5 risk heatmap matrix with risk count per cell, top open risks table sorted by inherent score, risk category breakdown, overdue-review alerts, auto-detected risks pending review queue); `RiskDetail.razor` (/risks/{id} — risk assessment panel with before/after scores, mitigation action plan linked to ActionItems, source traceability showing originating PFMEA/SPC/CAPA, review history); `RiskMatrix.razor` (/risks/matrix — configurable likelihood×consequence grid with threshold editor, drag risks between cells for re-assessment); integration with Phase 7 PFMEA (high-RPN failure modes auto-populate risk register); integration with Phase 24 SPC (recurring OOC patterns generate risk entries); integration with Phase 27 CAPA (recurring CAPAs indicate systemic risk); integration with Phase 25 Suppliers (score degradation triggers supplier risk); integration with Phase 28 Calibration (overdue calibrations → equipment risk); integration with Phase 15 Management Review (risk register is a required management review input per ISO 9001 clause 6.1); MCP `get_risk_status` tool (risk heatmap summary, top risks, auto-detected pending review, overdue reviews). **Market fit:** ISO 9001:2015 made risk-based thinking a foundational requirement (clauses 6.1, 4.4.1(f)), and AS9100 Rev D strengthened it further with explicit risk management requirements (clause 8.1.1 operational risk assessment). IATF 16949 requires specific risk analysis for product and process (clause 6.1.2.1, 6.1.2.2). Most manufacturers maintain risk registers in Excel or SharePoint, manually updated quarterly for management review — by the time a risk is documented, the damage is done. The platform already captures the raw signals (PFMEA scores, SPC data, CAPA patterns, supplier scores, calibration status) that constitute manufacturing risk. Automatically surfacing these as actionable risk entries — without requiring manual data gathering — transforms risk management from a compliance checkbox into a genuine early warning system. This is the feature that connects operational data to strategic decision-making, answering the executive question "where should we invest to prevent the next quality failure?" with data-driven evidence rather than gut feel.

#### To Do

- **Migrate file storage to cloud blob storage (S3/Azure Blob)** — Currently, uploaded files (Kind 3D models, Kind documents, StepTemplate 3D models) are stored on local disk under `wwwroot/uploads/`. This means files are tied to the specific server instance and are lost when cloning the database to another environment (e.g., restoring the Render production database locally results in 404s for all model/document files). Migrate to a shared cloud blob storage provider so files are accessible from any environment. Affected areas:
  - Kind model uploads/downloads (`uploads/kind-models/`)
  - Kind document uploads/downloads (`uploads/kind-documents/`)
  - StepTemplate model uploads/downloads (`uploads/step-models/`)
  - 3D model viewer on KindDetail, StepTemplateDetail, ProcessBuilder (slide view), and ExecutionWizard (Phase 4 panel)
  - `IImageStorageService` / `LocalImageStorageService` abstraction should be extended or replaced with a cloud-backed implementation

- **Server-side CAD to GLB conversion for faster 3D model loading** — STEP/STP/IGES/IGS files require an ~8 MB WASM module download and expensive client-side tessellation (boundary representation → triangle mesh), making them the slowest format to render. Add a server-side conversion pipeline: when a user uploads a STEP/IGES file, convert it to GLB on the API server and store the GLB alongside the original. The 3D viewer serves the pre-converted GLB for instant rendering while the original CAD file is preserved for download/engineering use. Affected areas:
  - API model upload endpoints for Kinds (`POST /api/kinds/{id}/model`) and StepTemplates (`POST /api/steptemplates/{id}/model`)
  - API model download/serve endpoints (serve GLB to viewer, original to download)
  - `model-viewer.js` — detect when a pre-converted GLB is available and use the fast GLTFLoader path instead of the OCCT WASM path
  - 3D viewer on KindDetail, StepTemplateDetail, ProcessBuilder (slide view), and ExecutionWizard (Phase 4 panel)
  - Consider using a .NET OpenCascade binding (e.g., `CadSharp`, `OpenCasCade.NET`) or a CLI tool (e.g., `FreeCAD` headless, `assimp`) for server-side conversion

#### Ongoing limitations

- Multi-tenancy deferred until a second SaaS tenant is onboarded (database-per-tenant approach selected — see Architecture Decision above)
- Email notifications for out-of-range alerts not yet implemented (webhook notifications are available via Phase 20)
- MCP server uses short-lived JWT tokens; Phase 21 introduces long-lived API keys for workstation/PLC integration — this could also be extended to MCP service accounts
