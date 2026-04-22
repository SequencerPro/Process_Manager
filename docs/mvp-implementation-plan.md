# Sequencer — MVP Implementation Plan

This document operationalizes the five MVP goals defined in [mvp-market-analysis.md](mvp-market-analysis.md) into sequenced engineering phases with concrete tasks, test requirements, and completion criteria.

## Version History

| Version | Date       | Notes |
|---------|------------|-------|
| 0.1     | 2026-04-20 | Initial plan — 5 MVP phases (M1–M5), sequencing rationale, task breakdown, test strategy |
| 0.2     | 2026-04-20 | M1 Multi-Tenant Isolation complete (project-plan v3.27) — progress tracker updated |
| 0.3     | 2026-04-21 | M2 Onboarding Wizard complete (project-plan v3.28) — public signup + 5-step wizard + feature flags + sample seeding; progress tracker updated; 3 post-MVP market-fit features appended |
| 0.4     | 2026-04-21 | M3 Execution Wizard Polish partial (project-plan v3.29) — batch prompt responses endpoint, HoldConfirmButton, touch target CSS, unsaved changes guard; 8 tests; 3 new market-fit features appended |

---

## Sequencing Rationale

The five MVP goals are interdependent. Their delivery order is chosen to minimize rework and to allow each phase to be demoed to stakeholders as a standalone milestone.

| # | Phase | Goal | Why this position |
|---|-------|------|-------------------|
| M1 | Multi-Tenant Isolation | Goal 4 | **Foundational.** Every entity needs a `TenantId`. Retrofitting tenancy after feature work is a multi-week refactor; doing it first costs days. |
| M2 | Onboarding Wizard | Goal 1 | **Proves the tenancy.** Cannot be built until tenants can be provisioned. Exercises the full sign-up-to-first-job flow. |
| M3 | Execution Wizard Polish | Goal 2 | **Core daily UX.** The operator surface is the primary reason the product gets used or abandoned after onboarding. |
| M4 | PDF Export | Goal 3 | **Sales artifact.** Shareable quality documents unlock conversations with quality managers and auditors. Build after execution flow is solid so the data is real. |
| M5 | Billing Infrastructure | Goal 5 | **Commercial enablement.** Last because it must wrap a mature, demonstrable product. Requires tenancy (M1) to attach subscriptions. |

---

## M1 — Multi-Tenant Isolation

**Goal:** Hard data isolation between customers so the system can be offered as SaaS.

**Why now:** Every feature added without tenancy has to be refactored later. Touching every entity once is cheaper than touching every query twice.

### Scope

- `Tenant` entity (Id, Name, Subdomain, CreatedAt, Status: Active/Suspended/Trial)
- `TenantId` column on every domain entity (Kind, Grade, StepTemplate, Process, Workflow, Job, Item, Batch, StepExecution, PortTransaction, ExecutionData, NonConformance, ActionItem, and all 40+ other entities)
- `ITenantContext` service populated from JWT `tenant_id` claim
- EF Core global query filter: `modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.CurrentTenantId)`
- Tenant-stamping save interceptor: sets `TenantId` on insert from current context
- `ApplicationUser` extended with `TenantId` and `IsPlatformAdmin` flag
- JWT issuance includes `tenant_id` and `tenant_subdomain` claims
- Tenant provisioning endpoint: `POST /api/platform/tenants` (platform admin only)
- Tenant invitation flow: invite email → new user sign-up under existing tenant

### Tasks

1. Add `Tenant` entity + `Phase_MVP01_MultiTenancy` EF migration
2. Add `TenantId` FK to every existing domain entity via migration (default all existing data to a "Legacy" tenant)
3. Implement `ITenantContext` + `TenantContextMiddleware` (reads JWT, resolves subdomain, throws on missing context for tenant-scoped endpoints)
4. Apply global query filter in `AppDbContext.OnModelCreating` via reflection over all `ITenantOwned` entities
5. Add `SaveChangesInterceptor` that stamps `TenantId` on insert
6. Extend `AuthController` login to embed `tenant_id` claim; add tenant resolution by subdomain on login
7. Add `PlatformTenantsController` for tenant CRUD (platform-admin only, bypasses tenant filter)
8. Add tenant invitation endpoint + email integration (SendGrid or SMTP)
9. Mark `ApplicationUser.IsPlatformAdmin` — only platform admins can cross tenant boundaries
10. Update `TestWebApplicationFactory` to seed a default tenant and issue tenanted JWTs

### Test Requirements

Create `MultiTenancyTests.cs`:

- `Kind_CreatedInTenantA_NotVisibleFromTenantB` — insert as Tenant A, read as Tenant B → 404
- `Job_CreatedInTenantA_CannotBeUpdatedFromTenantB` — cross-tenant PATCH → 404 (not 403, to prevent enumeration)
- `Query_ReturnsOnlyCurrentTenantRows` — seed both tenants, GET /api/kinds returns only current tenant's
- `SaveChangesInterceptor_StampsTenantIdOnInsert` — unit test on interceptor
- `PlatformAdmin_CanListAllTenants` — platform admin bypass works
- `RegularUser_CannotCallPlatformEndpoints` — 403 on /api/platform/*
- `Login_IssuesTokenWithTenantClaim` — JWT contains `tenant_id`
- `TenantInvite_CreatesUserUnderInvitingTenant` — accepted invite → user inherits tenant
- `MissingTenantClaim_ReturnsUnauthorized` — requests without tenant context → 401

Target: 12+ tests, all green, existing tests updated to use tenanted factory.

### Done when

- All existing integration tests pass under new tenancy model
- Cross-tenant data leak test suite green
- Platform admin can provision new tenant via API
- Invited user can sign up and lands in correct tenant

**Estimated effort:** 4–6 developer-days

---

## M2 — Onboarding Wizard

**Goal:** A new tenant goes from sign-up to executing their first real job in under 30 minutes, without external training.

**Why now:** Validates the tenancy from M1 and exercises the most critical UX path. If onboarding is broken, nothing else matters.

### Scope

- Public sign-up page at `/signup` (tenant name, subdomain, admin user details)
- Post-signup redirect to `/onboarding` wizard
- Five-step wizard:
  1. **Welcome** — industry selection (CNC, PCBA, Medical, General) → pre-selects vocabulary
  2. **First Kind** — create your first part/material (with guided fields)
  3. **First Step** — define one step with I/O ports (pre-filled template based on industry)
  4. **First Process** — compose the step into a single-step process and release
  5. **First Job** — launch a job, preview the execution wizard
- Sample process generation: "Create a sample Widget Inspection process" one-click option
- Reduced NavMenu: MVP-only sections visible by default; advanced modules behind a `/settings/modules` toggle
- "Skip onboarding" escape hatch that still seeds the sample process

### Tasks

1. Add `TenantOnboardingState` entity (TenantId, CurrentStep, CompletedAt)
2. Build `/signup` Blazor page + `POST /api/public/signup` endpoint (creates tenant + admin user + default vocabulary)
3. Build `OnboardingWizard.razor` with 5 steps and progress indicator
4. Build `DataSeeder.SeedSampleProcessAsync(Guid tenantId, string industry)` — creates Widget Inspection or equivalent per industry
5. Add `TenantFeatureFlags` entity (TenantId, ShowAdvancedModules bool, default false)
6. Update `NavMenu.razor` to hide advanced sections (MRB, Factory Design, Webhooks, AI Audit, Management Reviews, Five Whys, Ishikawa, Floor Plans) when `ShowAdvancedModules = false`
7. Build `/settings/modules` page for toggling advanced features
8. Add telemetry: record time from signup → first job completed

### Test Requirements

Create `OnboardingTests.cs`:

- `Signup_CreatesTenantAndAdminUser` — full signup → tenant exists, admin can log in
- `Signup_SeedsDefaultVocabularyForIndustry` — CNC signup → "Part" vocabulary active
- `Onboarding_ProgressPersistsAcrossSessions` — leave mid-wizard, log back in → resumes at same step
- `SampleProcess_Generated_IsExecutable` — sample process → launch job → wizard opens
- `SkipOnboarding_StillSeedsSample` — skip button → sample process exists in tenant
- `NavMenu_AdvancedModulesHidden_WhenFeatureFlagOff` — reflection/component test
- `ModulesToggle_PersistsAcrossReload` — toggle on → reload → still on
- `Subdomain_Conflict_Rejected` — duplicate subdomain → 409

Create `OnboardingUxTests.cs` (Blazor component tests):
- Each wizard step renders correctly
- Next button disabled until required fields filled
- Progress indicator reflects current step

Target: 10+ tests.

### Done when

- End-to-end: fresh signup → first job completed in < 30 minutes by untrained user (verified via stopwatch dogfood session)
- Sample processes exist for 4 industries (CNC, PCBA, Medical, General)
- NavMenu reduced to MVP surface by default

**Estimated effort:** 5–7 developer-days

---

## M3 — Execution Wizard Polish

**Goal:** The operator experience is production-ready on tablets, survives poor connectivity, and feels faster than paper.

**Why now:** Operators are the highest-volume users. If this surface is clunky, adoption dies at the floor.

### Scope

- Full tablet UX audit (iOS Safari, Android Chrome, iPadOS)
- Touch target audit — all interactive elements minimum 44×44 px
- Offline-capable measurement capture:
  - Service worker registered at `/operator-sw.js`
  - IndexedDB queue for prompt responses + port transactions
  - Sync-on-reconnect with conflict detection
  - Clear offline indicator in UI
- Confirmation patterns to prevent accidental step advancement:
  - "Complete Step" button requires hold-to-confirm (500ms) OR second confirmation tap
  - Back-navigation warning when uncommitted data exists
- Large-input numeric pad for tablet measurement entry
- Barcode scanner integration via camera (`@zxing/browser`) with keyboard-wedge fallback
- Photo capture: direct camera access, automatic resize/compress client-side before upload
- Signature capture: larger canvas, stroke smoothing

### Tasks

1. CSS audit — add `min-h-[44px] min-w-[44px]` (or equivalent) to all wizard buttons
2. Implement service worker + IndexedDB queue in `operator-sync.js`
3. Add `SyncStatus` component to wizard header (online/offline/pending count)
4. Wire `POST /api/step-executions/{id}/prompt-responses/batch` for queued syncs (idempotent via client-generated IDs)
5. Implement hold-to-confirm button component `HoldConfirmButton.razor`
6. Add `@zxing/browser` barcode scanner modal
7. Replace photo upload with `<input type="file" capture="environment">` + client-side `browser-image-compression`
8. Enlarge signature canvas, add stroke smoothing via quadratic curve interpolation
9. Add "Unsaved changes" guard on wizard navigation
10. Cross-device testing matrix (iPad 9th gen, Galaxy Tab A8, Surface Go)

### Test Requirements

Create `ExecutionWizardUxTests.cs`:

- `BatchPromptResponses_IdempotentOnRetry` — same client ID twice → one record (integration)
- `BatchPromptResponses_ValidatesAgainstLimits` — out-of-range value → validation error
- `HoldConfirmButton_RequiresSustainedPress` — component test: 200ms press → no fire; 600ms → fires
- `OfflineQueue_FlushesInOrder` — enqueue 3 items → reconnect → all written in order (JS unit test via Jest or equivalent)
- `BarcodeInput_AcceptsKeyboardWedge` — keydown simulation → correct value captured
- `SignatureCanvas_PersistsAcrossReloads` — draw → reload → still visible

Manual test checklist (`docs/qa/execution-wizard-tablet-checklist.md`):
- iPad Safari: complete a 5-step job offline, reconnect, verify all data synced
- Android Chrome: photo capture, orientation change mid-step
- Surface: touch vs. mouse hybrid input

Target: 8+ automated tests + documented manual QA pass.

### Done when

- 5-step job runnable fully offline on tablet, syncs cleanly on reconnect
- Zero accidental step completions in 20-job dogfood test
- Photo upload time < 3s on 4G for a typical 3MP image

**Estimated effort:** 6–8 developer-days

---

## M4 — PFMEA & Control Plan PDF Export

**Goal:** Quality engineers produce audit-ready, customer-shareable PFMEA and Control Plan PDFs from the system.

**Why now:** Concrete, shareable artifacts drive sales conversations. "Here's the PDF our system generates" is the single most effective feature demo.

### Scope

- PFMEA PDF: AIAG-4 standard column order (Function, Failure Mode, Effects, Severity, Causes, Occurrence, Current Controls, Detection, RPN, Recommended Action, Responsibility, Target Date, Actions Taken, New S/O/D/RPN)
- Control Plan PDF: AIAG standard format (Part Number, Operation, Machine/Device, Characteristic, Specification/Tolerance, Measurement Technique, Sample Size/Frequency, Control Method, Reaction Plan)
- Header block: company logo (uploaded per tenant), process name, revision, effective date, approvers with e-signature capture dates
- Footer: page numbers, document ID, generation timestamp, tenant name
- Landscape orientation, tabular layout with proper column widths
- Server-side generation via QuestPDF (MIT licensed, native .NET)
- Tenant branding: upload logo, set brand color, set approver title labels
- Watermark option for Draft status

### Tasks

1. Add QuestPDF NuGet package; configure community license in `Program.cs`
2. Add `TenantBranding` entity (TenantId, LogoFileName, PrimaryColorHex, CompanyName, FooterText)
3. Build `PfmeaPdfGenerator` service — consumes `PfmeaResponseDto`, produces PDF bytes
4. Build `ControlPlanPdfGenerator` service — consumes `ControlPlanResponseDto`, produces PDF bytes
5. Add `GET /api/pfmeas/{id}/pdf` endpoint (returns `application/pdf`)
6. Add `GET /api/control-plans/{id}/pdf` endpoint
7. Add "Download PDF" button to `PfmeaDetail.razor` and `ControlPlanDetail.razor`
8. Add `TenantBrandingController` + `/settings/branding` Blazor page
9. Draft watermark overlay when PFMEA/Control Plan status != Released
10. PDF preview modal in-app (embedded iframe or PDF.js)

### Test Requirements

Create `PdfExportTests.cs`:

- `GeneratePfmeaPdf_ReturnsValidPdf` — response starts with `%PDF-`, content-type correct
- `GeneratePfmeaPdf_ContainsAllFailureModes` — parse PDF (via `PdfPig`) → verify row count matches DTO
- `GeneratePfmeaPdf_RendersTenantLogo` — tenant with logo → PDF byte-match of embedded image present
- `GeneratePfmeaPdf_DraftStatusShowsWatermark` — status=Draft → "DRAFT" text extractable from PDF
- `GenerateControlPlanPdf_MatchesAiagColumnOrder` — extracted text → column sequence validated
- `PdfExport_RequiresAuthentication` — unauthenticated → 401
- `PdfExport_RespectsT enantIsolation` — cross-tenant ID → 404
- `PdfExport_LargePfmeaOver100Rows_GeneratesUnder5Seconds` — performance test

Target: 8+ tests. Use `PdfPig` for assertion-level PDF parsing.

### Done when

- PFMEA PDF visually matches AIAG-4 template (spot-checked by quality SME)
- Control Plan PDF accepted as valid by a reference supplier audit (or internal equivalent)
- Both PDFs generate in < 3 seconds for typical process sizes

**Estimated effort:** 4–5 developer-days

---

## M5 — Billing Infrastructure

**Goal:** Sell the product. Tenants can sign up on a trial, be charged monthly, and be suspended on payment failure.

**Why now:** Last because billing wraps a finished product. Building billing first means building speculation.

### Scope

- Stripe integration (Stripe.net SDK)
- Subscription plans:
  - **Trial**: 30 days, 1 process, 50 executions/month, 3 users — free
  - **Starter**: $300/mo — 25 users, unlimited processes/executions, 1 site
  - **Professional**: $600/mo — 100 users, 3 sites, priority support, advanced modules
  - **Enterprise**: Custom — quoted, self-hosted option
- Tenant lifecycle hooks:
  - Trial expiration → 7-day grace → suspend
  - Payment failure → 3-day grace → suspend
  - Suspended tenant → login blocked with billing page only
- Usage metering: record job executions per tenant per month (for transparency, not billing yet)
- Billing portal: `/billing` page showing current plan, next invoice, payment method, invoice history (via Stripe Customer Portal link)
- Webhook endpoint: `POST /api/platform/stripe-webhook` (payment.succeeded, payment.failed, subscription.deleted)

### Tasks

1. Add `Stripe.net` NuGet package
2. Add `TenantSubscription` entity (TenantId, StripeCustomerId, StripeSubscriptionId, PlanCode, Status, CurrentPeriodEnd, TrialEndsAt)
3. Add `UsageMetric` entity (TenantId, MetricType, Count, PeriodStart, PeriodEnd)
4. Configure Stripe products/prices in Stripe dashboard; store price IDs in config
5. Update signup flow to create Stripe customer + trial subscription
6. Build `/billing` page with plan info + "Manage Billing" button → Stripe portal session
7. Implement `StripeWebhookController` (signature verification, idempotent event handling)
8. Add `TenantSuspensionMiddleware` — blocks suspended tenants from all endpoints except `/billing` and `/auth/logout`
9. Add background job: nightly check for trials ending in 7 days → email notification
10. Add background job: monthly usage rollup (job executions count per tenant)

### Test Requirements

Create `BillingTests.cs`:

- `Signup_CreatesStripeCustomerAndTrialSubscription` — mock Stripe → verify calls
- `StripeWebhook_PaymentSucceeded_UpdatesSubscriptionStatus` — post webhook → DB reflects Active
- `StripeWebhook_PaymentFailed_EntersGracePeriod` — post webhook → Status=PastDue, GraceEndsAt set
- `StripeWebhook_InvalidSignature_Rejected` — bad signature → 400
- `StripeWebhook_DuplicateEvent_IdempotentlyIgnored` — same event_id twice → single effect
- `SuspendedTenant_CannotAccessApiEndpoints` — suspended → 402 Payment Required on /api/kinds
- `SuspendedTenant_CanStillAccessBillingPage` — suspended → 200 on /billing
- `TrialExpiration_MovesToSuspended_AfterGrace` — time travel 37 days → suspended
- `UsageMetrics_RecordedOnJobCompletion` — complete job → metric row inserted

Target: 10+ tests. Use Stripe's test mode + mocked webhook signatures.

### Done when

- Full signup-to-paid flow demonstrable: sign up → trial → add payment method → charged → stays active
- Payment failure path demonstrable: expire card → webhook → grace period → suspension
- Billing page live with plan info and Stripe portal integration

**Estimated effort:** 5–7 developer-days

---

## Cross-Cutting Concerns

### Test Strategy

- **All new controllers** have integration tests via `TestWebApplicationFactory`
- **All new services** have unit tests with mocked dependencies
- **All new Blazor pages** have at minimum a reflection-based render test
- **Regression gate:** existing ~380+ tests must pass before any MVP phase merges
- **Coverage target:** 75%+ line coverage on new code (measured via `coverlet`)

### Documentation Discipline

Each MVP phase, when complete, updates:
1. `project-plan.md` — append a new version row documenting what shipped
2. `mvp-market-analysis.md` — update the "New Goals & Tasks" section with progress markers (⬜ / 🟨 / ✅)
3. `data-model.md` — add any new entities with full schema documentation
4. This document — mark the phase status at the top of its section

### Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Multi-tenancy retrofit breaks existing tests | High | High | Do M1 first, before any other MVP work; maintain old test suite as regression gate |
| Offline sync conflicts are hard to resolve | Medium | Medium | Use client-generated IDs for idempotency; last-write-wins with user confirmation on true conflicts |
| PDF rendering differs across QuestPDF versions | Low | Low | Pin version; snapshot test a reference PDF |
| Stripe webhook delivery failures | Medium | Medium | Idempotency keys; nightly reconciliation job comparing DB state to Stripe API |
| Onboarding feels too long despite wizard | Medium | High | Dogfood with 3+ untrained testers before declaring M2 done |

---

## Timeline

At one full-time developer, ~6 weeks of focused work:

```
Week 1-2:  M1 Multi-Tenant Isolation         [========]
Week 3:    M2 Onboarding Wizard              [======]
Week 4:    M3 Execution Wizard Polish        [======]
Week 5:    M4 PDF Export                     [=====]
Week 6:    M5 Billing Infrastructure         [======]
Week 6.5:  MVP hardening + first customer demo prep
```

Parallelization potential: M3 and M4 can proceed concurrently once M1 is done (different subsystems, minimal overlap). M5 must follow M1 but can run in parallel with M3/M4 in its later stages.

---

## Progress Tracker

| Phase | Status | Version row added | Tests added | Notes |
|-------|--------|-------------------|-------------|-------|
| M1 Multi-Tenant Isolation | ✅ Complete | 3.27 (2026-04-20) | 14 `MultiTenancyTests` (508 total, all green) | `Tenant` entity, `TenantId` on BaseEntity, query filter via reflection, `TenantSaveChangesInterceptor`, `ITenantContext`, `TenantContextMiddleware`, JWT `tenant_id` claim, `PlatformTenantsController`, `Phase_MVP01_MultiTenancy` migration |
| M2 Onboarding Wizard | ✅ Complete | 3.28 (2026-04-21) | 16 `OnboardingTests` (524 total, all green) | `TenantOnboardingState` + `TenantFeatureFlags` entities, `OnboardingIndustry` enum, `PublicSignupController` + `OnboardingController`, `JwtTokenService` extraction, `DataSeeder.SeedSampleProcessAsync` (industry-specific sample content), `Phase_MVP02_Onboarding` migration, `Signup.razor` + `OnboardingWizard.razor` + `Settings/Modules.razor` Blazor pages, `FeatureFlagService` + `ModuleToggle` shared, NavMenu feature-flag gating |
| M3 Execution Wizard Polish | 🟨 In progress | 3.29 (2026-04-21) | 8 `ExecutionWizardUxTests` (532 total, all green) | Batch prompt responses endpoint with ClientId idempotency, `HoldConfirmButton.razor` component, touch-target CSS audit (`@media (pointer: coarse)` rules), `beforeunload` unsaved-changes guard, phase-navigation dirty-check. Remaining: service worker + IndexedDB offline queue, barcode scanner, photo capture, signature canvas improvements, cross-device testing |
| M4 PDF Export | ⬜ Not started | — | — | — |
| M5 Billing Infrastructure | ⬜ Not started | — | — | — |

Legend: ⬜ Not started · 🟨 In progress · ✅ Complete

---

## Post-MVP Market-Fit Feature Proposals

Three features selected to maximise conversion and retention after the five-phase MVP lands. Each is scoped to be deliverable in 1–2 weeks by a single engineer and directly addresses a top-three objection we've seen in the target segments defined in `mvp-market-analysis.md` (small-shop CNC, PCBA contract houses, medical-device OEMs, regulated manufacturing).

### F1 — Mobile-First Operator App (PWA)

**Pain it removes:** Operators on the shop floor often have no desk, no keyboard, and greasy hands. A tablet or phone-first execution surface materially lowers the barrier to adoption for the Participant role and is a recurring sales objection ("will this work on the floor?").

**Scope:**
- New installable PWA (`/portal/*` routes) served from the existing Blazor app with a manifest and service worker — reuses the existing `ParticipantLayout` so no new auth surface.
- Offline queue for `PromptResponse`, `PortTransaction` and `StepExecution.Complete` payloads — writes to IndexedDB, drains on reconnect via an `ExecutionSyncService`.
- Touch-first redesign of `PortalExecutionWizard.razor`: large hit-targets (≥ 48 px), single-panel per phase, swipe navigation, on-screen keyboard helpers for numeric/barcode prompts.
- Camera prompt type (`PromptType.Photo`) — captures in-browser, uploads to `/api/step-executions/{id}/attachments`, shows inline thumbnail in `StepExecutionDetail`.
- "Clock in / clock out" check-in per step for timing accuracy without requiring an Execution record.

**Entities / migrations:**
- `PromptType.Photo` enum value.
- `StepExecutionAttachment` entity (Id, StepExecutionId, FileName, MimeType, UploadedByUserId, UploadedAt, PromptDefinitionId optional).
- `Phase_Post_Mobile` EF migration.

**Tests:** `MobileExecutionTests.cs` — offline queue replay correctness, photo upload size guard, PWA manifest served, service worker registration, Participant role cannot escape `/portal/*`.

**Why it ships first:** Directly converts demo-to-trial bookings in CNC/PCBA segments where the evaluator immediately asks "can my guy on the mill do this on a tablet?"

---

### F2 — ROI Dashboard & Audit-Ready Evidence Pack

**Pain it removes:** Sales conversations stall when the buyer can't articulate the savings to their CFO, and renewals stall when quality managers have to prove value at budget review. Auditors (ISO 9001 / AS9100 / FDA 820) separately ask for a packaged evidence bundle that today requires days of screenshotting.

**Scope:**
- New `/roi` Blazor page (Admin/Engineer) computing on-the-fly ROI metrics over any date range:
  - Operator hours saved vs. paper (process median time × jobs completed).
  - Scrap $ avoided (NonConformance Quarantine/Scrap count × average item cost).
  - Quality-event MTTR improvement (NonConformance open→disposition median) vs. rolling 90-day baseline.
  - Training compliance % and Expiry-risk heatmap (reuses Phase 16 data).
- `POST /api/reports/evidence-pack` — generates a single ZIP with: active QMS documents (PDF), last N audit findings + corrective actions (PDF), PFMEA/Control Plan for the selected Process (PDF — reuses M4 renderer), sampled completed Job records (PDF), competency matrix export (CSV), a `manifest.json` describing what's inside and a SHA-256 of each file.
- Shareable read-only link (`/public/roi/{token}`) — short-lived signed token, renders the headline KPIs only (no drill-down). Lets champions share the number with their CFO without granting logins.
- `RoiSnapshot` entity captures a point-in-time copy on every Management Review — lets the Management Review page render "before vs. now" next to the current numbers.

**Entities / migrations:**
- `RoiSnapshot` entity (Id, TenantId, SnapshotAt, Metrics JSON).
- `EvidencePackRequest` entity (Id, TenantId, ProcessId nullable, DateFrom, DateTo, Status, DownloadUrl, CreatedByUserId, CreatedAt).
- `Phase_Post_ROI` EF migration.

**Tests:** `RoiReportTests.cs` — metric math correctness on seeded data, evidence-pack ZIP manifest integrity, shareable token expiry, RoiSnapshot auto-capture on Management Review close, cross-tenant isolation of shareable links.

**Why it matters:** Directly attacks the top cited non-technical reason for churn — "we can't prove the value internally." The evidence pack also halves audit-prep time for the regulated segments.

---

### F3 — AI Process Author (GPT-assisted Step Template / PFMEA Drafting)

**Pain it removes:** Authoring the first process, PFMEA, and Control Plan is the #1 reason trials stall after onboarding. Even with the M2 sample seed, the blank page for the second process is intimidating and pushes the champion back to Word.

**Scope:**
- New `/ai/author` Admin/Engineer page with three modes:
  - **From description** — free-text prompt ("a 5-axis milling op for a titanium bracket, 3 critical dimensions") → proposed Step Template (name, Description, Pattern, suggested ports, Setup block, numeric prompts with typical limits).
  - **From existing document** — drag-and-drop Word/PDF/image of a work instruction → extracted step structure + prompts.
  - **PFMEA assist** — select a Process → pre-fills failure modes for each step by querying a library of industry priors (CNC/PCBA/Medical) already seeded into `RootCauseEntry`.
- Output is always a **Draft proposal** inserted into the existing builder surfaces — never directly published. The author reviews, edits, and saves through the existing lifecycle.
- Reuses the existing `McpAuditLog` append-only table to log every generation with input, output, model, and token count — addresses the "who's auditing the AI?" regulated-segment objection.
- Provider-agnostic via an `IProcessAuthorClient` abstraction; initial implementation uses Anthropic Claude over the existing API key plumbing. OpenAI/Azure swap-in requires no controller changes.

**Entities / migrations:**
- `ProcessAuthorDraft` entity (Id, TenantId, Kind enum {StepTemplate, Pfmea, ControlPlan}, PayloadJson, Status {Draft/Applied/Discarded}, CreatedByUserId, CreatedAt, AppliedAt nullable).
- `Phase_Post_AiAuthor` EF migration.

**Tests:** `ProcessAuthorTests.cs` — golden-output unit tests against a stubbed `IProcessAuthorClient`, Draft→Applied state transitions, tenant isolation of drafts, RBAC (Participant cannot call author endpoints), audit-log entry written on every call, reject oversized uploads.

**Why it ships third:** Needs M1+M2 (tenancy and onboarding) and M4 (PFMEA surface area is stable) already shipped. Highest potential upside for trial-to-paid conversion in CNC and PCBA segments, where process authoring is the primary barrier to moving off Word docs.

---

## Additional Market-Fit Feature Proposals (2026-04-21)

Three features selected to address emerging gaps identified during M1–M3 development. Each targets a specific segment or retention driver.

### F4 — Supplier Quality Portal

**Pain it removes:** Contract manufacturers (PCBA, CNC) and medical-device OEMs spend significant time managing incoming inspection results, supplier corrective actions (SCARs), and approved vendor lists in spreadsheets. The existing MRB feature handles internal non-conformances but doesn't give suppliers a self-service surface to respond to SCARs, submit CoCs (Certificates of Conformance), or view their quality performance.

**Scope:**
- New `SupplierContact` entity linked to `Kind.VendorName` — email, portal access token, notification preferences.
- `/supplier-portal/{token}` read-only Blazor surface (no login required — signed token URL) showing: open SCARs assigned to them, required response fields (root cause, corrective action, evidence upload), submitted CoC history, their quality scorecard (PPM defect rate, on-time delivery %, response time).
- `SupplierScorecardService` computing PPM from `NonConformance` records where `Kind.SourceType == Buy` and `Kind.VendorName` matches, on-time from `InventoryTransaction.Receipt` timestamps vs. `Kind.LeadTimeDays`.
- Admin `/suppliers` page with vendor list, invite flow, scorecard overview, and approved/probation/disqualified status.
- `POST /api/suppliers/{id}/scar-response` — supplier-facing endpoint to submit SCAR responses (gated by token, not JWT).

**Entities / migrations:**
- `SupplierContact` (Id, TenantId, VendorName, Email, PortalToken, Status enum {Active/Probation/Disqualified}, InvitedAt, LastAccessAt).
- `ScarResponse` (Id, TenantId, MrbReviewId, RootCauseDescription, CorrectiveAction, EvidenceFileName, SubmittedAt).
- `Phase_Post_SupplierPortal` EF migration.

**Tests:** `SupplierPortalTests.cs` — token-based access works without JWT, expired token rejected, SCAR response creates record, scorecard PPM math, vendor filter on NonConformance, cross-tenant isolation of supplier tokens.

**Why it matters:** Supplier quality management is the #1 feature request from the medical-device segment (FDA 820 requires documented supplier controls). Also addresses CNC shops buying raw stock from multiple vendors with no centralized incoming inspection data.

---

### F5 — Real-Time Production Dashboard (SignalR Live Updates)

**Pain it removes:** Production managers and operations leads currently rely on periodic page refreshes to see job status, throughput, and downtime. On a busy shop floor, a 30-second delay between a machine going down and the dashboard reflecting it can cost real money. The existing Dashboard and Production Dashboard pages are static snapshots.

**Scope:**
- `ProductionHub` SignalR hub at `/hubs/production` broadcasting: job status transitions, step execution completions, downtime start/close events, alert threshold breaches, pick-list status changes.
- Event publishing via `IProductionEventPublisher` injected into `JobsController`, `StepExecutionsController`, `EquipmentController`, and `AlertsController` — fires hub messages on state transitions.
- Updated `ProductionDashboard.razor` with live-updating KPI cards (flash animation on change), auto-refreshing WIP board, live downtime timer, and real-time job completion feed.
- New `FloorStatus.razor` full-screen kiosk-mode page at `/floor-status` — designed for large-screen TVs on the shop floor. Shows: active jobs per workstation (from `FloorPlanWorkstation`), live throughput counter, current downtime events, shift progress bar. No sidebar, no navigation — pure display surface.
- Configurable refresh interval fallback for environments where WebSocket is blocked (HTTP long-polling via SignalR auto-negotiation).

**Entities / migrations:**
- No new entities — uses existing domain events. `Phase_Post_SignalR` migration adds optional `LastHeartbeatAt` to `Equipment` for stale-connection detection.

**Tests:** `ProductionHubTests.cs` — hub connection with tenant isolation (hub resolves tenant from JWT), job-status-change broadcasts to correct tenant group, downtime event triggers hub message, kiosk page renders without authentication errors, concurrent connections from same tenant receive same events.

**Why it matters:** "Can I put this on the TV on the floor?" is asked in nearly every demo. Real-time visibility converts Production Dashboard page-refreshers into always-on operational awareness — the stickiest feature for manufacturing environments.

---

### F6 — Multi-Site Process Deployment & Version Sync

**Pain it removes:** Companies with multiple manufacturing sites (common in medical, aerospace, and larger CNC operations) need to maintain consistent processes across locations. Today, a process revised at Site A must be manually recreated at Site B. There's no mechanism for a "golden" process library that propagates changes to satellite sites, nor a way to track which sites are running which version.

**Scope:**
- `Site` entity (Id, TenantId, Code, Name, Address, TimeZone, IsHeadquarters bool) — each tenant can define multiple sites.
- `ApplicationUser.SiteId` — users are assigned to a site; `OrgUnit.SiteId` — organizational structure is site-scoped.
- `ProcessDeployment` entity (Id, ProcessId, SiteId, DeployedVersion, DeployedAt, DeployedByUserId, Status enum {Pending/Active/Superseded/Withdrawn}) — tracks which version of a process is active at which site.
- `POST /api/processes/{id}/deploy` — creates deployment records for selected sites; if the process has a newer version than currently deployed, marks existing deployment as Superseded.
- `DeploymentDashboard.razor` at `/deployment` — matrix view (processes × sites) showing deployed version, color-coded for version currency (green = latest, amber = one behind, red = two+ behind or withdrawn).
- Process lifecycle integration: when a new process version is Released, sites with the previous version are flagged for deployment review. Admins can bulk-deploy or site-by-site.
- Job creation respects site context: only processes deployed to the user's site appear in the process picker.

**Entities / migrations:**
- `Site` (Id, TenantId, Code, Name, Address, TimeZone, IsHeadquarters, CreatedAt).
- `ProcessDeployment` (Id, TenantId, ProcessId, SiteId, DeployedVersion, DeployedAt, DeployedByUserId, Status).
- `SiteId` nullable FK on `ApplicationUser` and `OrgUnit`.
- `Phase_Post_MultiSite` EF migration.

**Tests:** `MultiSiteTests.cs` — deployment creates record, re-deploy supersedes previous, job creation filtered to site-deployed processes, deployment dashboard returns correct version matrix, cross-site isolation (site A user cannot see site B deployments), bulk deploy creates records for all selected sites, withdrawn deployment removes process from job picker.

**Why it matters:** Multi-site is the #1 differentiator between "team tool" ($300/mo Starter) and "enterprise platform" ($600+/mo Professional). It's also a natural expansion path — a customer that starts at one site and wants to roll out to two more is the highest-LTV growth motion for manufacturing SaaS.
