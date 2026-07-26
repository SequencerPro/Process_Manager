# Sequencer — MVP Implementation Plan

This document operationalizes the five MVP goals defined in [mvp-market-analysis.md](mvp-market-analysis.md) into sequenced engineering phases with concrete tasks, test requirements, and completion criteria.

## Version History

| Version | Date       | Notes |
|---------|------------|-------|
| 0.1     | 2026-04-20 | Initial plan — 5 MVP phases (M1–M5), sequencing rationale, task breakdown, test strategy |
| 0.2     | 2026-04-20 | M1 Multi-Tenant Isolation complete (project-plan v3.27) — progress tracker updated |
| 0.3     | 2026-04-21 | M2 Onboarding Wizard complete (project-plan v3.28) — public signup + 5-step wizard + feature flags + sample seeding; progress tracker updated; 3 post-MVP market-fit features appended |
| 0.4     | 2026-04-21 | M3 Execution Wizard Polish partial (project-plan v3.29) — batch prompt responses endpoint, HoldConfirmButton, touch target CSS, unsaved changes guard; 8 tests; 3 new market-fit features appended |
| 0.5     | 2026-04-22 | M4 PFMEA & Control Plan PDF Export complete (project-plan v3.30) — QuestPDF generators, branding controller, PDF endpoints, 12 tests; 3 new market-fit features appended |
| 0.6     | 2026-04-22 | M5 Billing Infrastructure complete (project-plan v3.31) — Stripe.net integration, TenantSubscription/UsageMetric/BillingEvent entities, BillingController, StripeWebhookController, TenantSuspensionMiddleware, 14 tests; 3 new market-fit features appended |
| 0.7     | 2026-04-22 | M3 Execution Wizard Polish complete (project-plan v3.32) — service worker, IndexedDB offline queue, SyncStatus component, barcode scanner with camera + keyboard-wedge fallback, photo capture with client-side compression, signature canvas with quadratic curve stroke smoothing, 11 new OfflineQueueTests; 3 new market-fit features appended |
| 0.8     | 2026-04-22 | F10 Plan-Gated Feature Tiers & Usage Enforcement complete (project-plan v3.33) — PlanEnforcementService, UsageMeteringService, plan limit checks on user/process/job creation, usage metering on job completion and PDF export, feature flag auto-sync on plan change, 14 tests; 3 new market-fit features appended |

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
| M3 Execution Wizard Polish | ✅ Complete | 3.32 (2026-04-22) | 19 tests: 8 `ExecutionWizardUxTests` + 11 `OfflineQueueTests` (569 total, all green) | Batch prompt responses endpoint with ClientId idempotency, `HoldConfirmButton.razor` component, touch-target CSS audit (`@media (pointer: coarse)` rules), `beforeunload` unsaved-changes guard, phase-navigation dirty-check, `operator-sw.js` service worker (static asset caching, network-first with cache fallback), `operator-sync.js` IndexedDB offline queue (enqueue/flush/sync-on-reconnect, grouped batch submission by stepExecutionId), `SyncStatus.razor` component (online/offline/pending/syncing states in wizard header), `BarcodeScannerModal.razor` (native BarcodeDetector API camera scanning + keyboard-wedge manual entry fallback), `PhotoCapture.razor` (camera capture with Canvas-based client-side JPEG compression, configurable max dimensions/quality), `SignatureCanvas.razor` (pointer-event drawing with quadratic curve interpolation for smooth strokes, clear/export/load support) |
| M4 PDF Export | ✅ Complete | 3.30 (2026-04-22) | 12 `PdfExportTests` (544 total, all green) | QuestPDF 2024.12.3 Community license, `TenantBranding` entity + `Phase_MVP04_PdfExport` migration, `PfmeaPdfGenerator` (AIAG-4 columns, RPN color coding, DRAFT banner, multi-action rows), `ControlPlanPdfGenerator` (AIAG columns), `GET /api/pfmeas/{id}/pdf` + `GET /api/controlplans/{id}/pdf` endpoints, `TenantBrandingController` (CRUD + logo upload/delete with 2 MB/PNG/JPG/SVG validation), PdfPig-based content assertions |
| M5 Billing Infrastructure | ✅ Complete | 3.31 (2026-04-22) | 14 `BillingTests` (558 total, all green) | Stripe.net 46.0.0 NuGet package; `TenantSubscription` entity (StripeCustomerId, StripeSubscriptionId, PlanCode, Status, TrialEndsAt, CurrentPeriodEnd, GraceEndsAt, FailedPaymentCount); `UsageMetric` entity (MetricType, Count, PeriodStart, PeriodEnd); `BillingEvent` audit entity (StripeEventId, EventType, Description, RawPayload); `SubscriptionPlan` enum (Trial/Starter/Professional/Enterprise); `SubscriptionStatus` enum (Trial/Active/PastDue/Suspended/Cancelled); `Phase_MVP05_BillingInfrastructure` EF migration; `IStripeService` abstraction with `StripeService` implementation (customer creation, trial subscription, billing portal session, webhook verification); `BillingController` (dashboard, subscription, portal-session, usage, events endpoints — Admin-only); `StripeWebhookController` (AllowAnonymous, signature verification, idempotent event processing, payment succeeded/failed/subscription deleted/updated handlers with grace period logic); `TenantSuspensionMiddleware` (402 Payment Required for suspended tenants on API endpoints, exempts /api/billing + /api/auth/logout + webhook); signup flow creates `TenantSubscription` with 30-day trial on every new tenant; `TestStripeService` stub for integration tests |
| F10 Plan-Gated Feature Tiers | ✅ Complete | 3.33 (2026-04-22) | 14 `PlanEnforcementTests` (583 total, all green) | `PlanEnforcementService` with code-defined `PlanLimits` per plan (Trial: 3 users/1 process/50 executions/mo, Starter: 25 users/1 site, Professional: 100 users/3 sites/advanced modules, Enterprise: unlimited); `UsageMeteringService` for monthly metric tracking; `BillingController` extended with plan/usage/check endpoints; user/process/job creation gated by plan limits (402 with upgrade prompt); job completion + PDF export increment usage metrics; feature flags auto-synced on plan change via `SyncFeatureFlagsForPlan`; legacy tenants without subscriptions bypass enforcement |

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

---

## Additional Market-Fit Feature Proposals (2026-04-22)

Three features selected to close the most critical functional gaps identified during M4 implementation. Each targets a compliance requirement or revenue-driving capability that competitors in the manufacturing QMS space treat as table-stakes.

### F7 — Statistical Process Control (SPC) Charts & Capability Analysis

**Pain it removes:** The system collects numeric measurement data from operator prompts during execution, but offers no statistical analysis. Quality engineers must export to Minitab or Excel to produce X-bar/R charts and Cp/Cpk reports. Every IATF 16949, AS9100, and FDA 820 audit asks for evidence of statistical process control — without it, the system is a data collector, not a quality tool.

**Scope:**
- `SpcChart` entity (Id, TenantId, ProcessId, ContentBlockId FK → the numeric prompt, ChartType enum {XbarR, XbarS, IndividualMR, P, NP, C, U}, SubgroupSize, ControlLimitSource enum {Calculated, Manual}, UCL/LCL/CL override nullable, TargetCpk, IsActive).
- `SpcDataPoint` entity (Id, SpcChartId, StepExecutionId, Value decimal, SubgroupIndex, CapturedAt) — populated automatically when a prompt response is saved for a linked content block.
- Calculation engine: `SpcCalculationService` computing X-bar, R, sigma, UCL/LCL/CL (A2/D3/D4 constants), Cp, Cpk, Pp, Ppk from the latest N subgroups.
- `SpcController` — chart CRUD, data retrieval with date range, recalculate-limits endpoint, out-of-control detection (Western Electric rules: 1 point beyond 3σ, 2 of 3 beyond 2σ, 4 of 5 beyond 1σ, 8 consecutive on one side).
- `SpcDashboard.razor` at `/spc` — chart list with Cp/Cpk badges (green ≥ 1.33, amber ≥ 1.0, red < 1.0), inline chart rendering via Chart.js (X-bar and R on stacked axes), out-of-control alerts linked to `ActionItem` creation.
- `SpcChartDetail.razor` — interactive chart with date range slider, capability histogram overlay, data table, control limit editor, out-of-control point highlighting.
- Integration hook: `StepExecutionsController` prompt-response save auto-inserts `SpcDataPoint` when the content block has a linked `SpcChart`.

**Entities / migrations:**
- `SpcChart` (Id, TenantId, ProcessId, ContentBlockId, ChartType, SubgroupSize, ControlLimitSource, UCL, LCL, CL, TargetCpk, IsActive, CreatedAt, UpdatedAt).
- `SpcDataPoint` (Id, SpcChartId, StepExecutionId, Value, SubgroupIndex, CapturedAt).
- `Phase_Post_SPC` EF migration.

**Tests:** `SpcTests.cs` — Cp/Cpk calculation correctness on known datasets, control limit computation matches manual A2/D3/D4 values, out-of-control detection (Western Electric rule 1, rule 2, rule 3, rule 4), auto-insert data point on prompt response, chart CRUD, tenant isolation, empty chart returns zeros not errors.

**Why it matters:** SPC is the single most-requested quality feature in manufacturing QMS products. Without it, the system cannot compete for IATF 16949 (automotive) or AS9100 (aerospace) customers. It also makes the existing measurement data immediately actionable — turning the execution wizard from a data entry form into a process improvement tool.

---

### F8 — Calibration & Gauge Management

**Pain it removes:** ISO 9001 clause 7.1.5 requires documented control of monitoring and measuring resources. The existing Control Plan specifies *what* measurement technique to use, but there is no mechanism to track whether the measuring equipment is calibrated, when calibration expires, or which calibration certificates apply. Quality managers maintain separate spreadsheets or standalone calibration tools, creating a data silo that auditors probe in every external audit.

**Scope:**
- `GaugeType` entity (Id, TenantId, Name, Description, CalibrationIntervalDays, CalibrationMethod) — categories like "Micrometer 0-25mm", "CMM", "Torque Wrench".
- `Gauge` entity (Id, TenantId, GaugeTypeId, Code unique, SerialNumber, Manufacturer, Model, Location, Status enum {Active/OutForCalibration/Overdue/Retired}, LastCalibrationDate, NextCalibrationDue, AssignedToEquipmentId nullable FK → Equipment).
- `CalibrationRecord` entity (Id, GaugeId, CalibratedAt, CalibratedByUserId, CertificateNumber, ExternalLabName nullable, Result enum {Pass/Fail/Adjusted}, Notes, CertificateFileName nullable, NextDueDate).
- `ControlPlanEntry.GaugeTypeId` nullable FK — links "measurement technique" to a specific gauge type, enabling automatic validation that the gauge used in execution is calibrated.
- `GaugesController` — CRUD for types and gauges, calibration record management, overdue gauge list, calibration certificate upload/download.
- `CalibrationDashboard.razor` at `/calibration` — KPI cards (total gauges, due in 30 days, overdue, out-for-cal), gauge list with status badges and next-due countdown, create/edit modals, calibration history timeline.
- `GaugeDetail.razor` — calibration history, certificate viewer, linked Control Plan entries, equipment assignment.
- Background job: nightly check for gauges due within 7 days → creates `ActionItem` for responsible party.
- ExecutionWizard integration: when a Control Plan entry links a gauge type, the wizard shows gauge selection dropdown filtered to calibrated gauges of that type; selecting an overdue gauge triggers a warning.

**Entities / migrations:**
- `GaugeType` (Id, TenantId, Name, Description, CalibrationIntervalDays, CalibrationMethod).
- `Gauge` (Id, TenantId, GaugeTypeId, Code, SerialNumber, Manufacturer, Model, Location, Status, LastCalibrationDate, NextCalibrationDue, AssignedToEquipmentId).
- `CalibrationRecord` (Id, GaugeId, CalibratedAt, CalibratedByUserId, CertificateNumber, ExternalLabName, Result, Notes, CertificateFileName, NextDueDate).
- `GaugeTypeId` nullable FK on `ControlPlanEntry`.
- `Phase_Post_Calibration` EF migration.

**Tests:** `CalibrationTests.cs` — gauge CRUD, calibration record creation updates gauge status and NextDue, overdue detection, Control Plan gauge type linkage, ExecutionWizard gauge validation, certificate upload/download, gauge code uniqueness, tenant isolation.

**Why it matters:** Calibration management is a mandatory audit checkpoint for every ISO 9001 / AS9100 / FDA 820 certification. Integrating it with the existing Control Plan and execution flow eliminates a standalone tool and closes the audit loop — the system can prove not just *what* was measured, but that the *instrument was calibrated* when the measurement was taken.

---

### F9 — Customer Order Portal & Job Linkage

**Pain it removes:** Contract manufacturers (CNC shops, PCBA houses) receive customer purchase orders via email or ERP, then manually create jobs in the system with no traceability back to the customer order. When the customer asks "where is my order?" or "send me the inspection data for PO 12345," the quality engineer must manually correlate jobs to orders. There is no self-service surface for customers to view order status, download quality evidence, or submit new work requests.

**Scope:**
- `CustomerAccount` entity (Id, TenantId, Name, Code, ContactEmail, PortalToken, Status enum {Active/Inactive}, RequiredDocuments bitmask {PFMEA/ControlPlan/CoC/FirstArticle}).
- `CustomerOrder` entity (Id, TenantId, CustomerAccountId, OrderNumber, ReceivedAt, DueDate, Status enum {Received/InProgress/Shipped/Closed/Cancelled}, Notes).
- `CustomerOrderLine` entity (Id, CustomerOrderId, KindId, Quantity, UnitPrice nullable, JobId nullable FK — links to the manufacturing job).
- `CertificateOfConformance` entity (Id, TenantId, CustomerOrderId, GeneratedAt, GeneratedByUserId, PdfFileName, SignedByUserId nullable, SignedAt nullable) — auto-generated PDF summarizing inspection results for all jobs linked to the order.
- `/customer-portal/{token}` Blazor surface (no login required — signed token URL): order list with status, per-order drill-down with job progress, downloadable quality documents (PFMEA PDF, Control Plan PDF, CoC PDF) as configured by `RequiredDocuments`.
- `CustomerOrdersController` — CRUD, line management with job linkage, status transitions, CoC generation endpoint (composes PDF from linked job inspection data + tenant branding via existing QuestPDF infrastructure).
- `CustomerList.razor` at `/customers` — account management, invite flow (sends portal URL via email), required documents configuration.
- `CustomerOrderList.razor` at `/customer-orders` — order management, line-to-job linkage picker, CoC generation button, shipping status.
- Job creation integration: when creating a job from an order line, `JobsController` auto-populates KindId and links back to CustomerOrderLineId.

**Entities / migrations:**
- `CustomerAccount` (Id, TenantId, Name, Code, ContactEmail, PortalToken, Status, RequiredDocuments).
- `CustomerOrder` (Id, TenantId, CustomerAccountId, OrderNumber, ReceivedAt, DueDate, Status, Notes).
- `CustomerOrderLine` (Id, CustomerOrderId, KindId, Quantity, UnitPrice, JobId).
- `CertificateOfConformance` (Id, TenantId, CustomerOrderId, GeneratedAt, GeneratedByUserId, PdfFileName, SignedByUserId, SignedAt).
- `Phase_Post_CustomerPortal` EF migration.

**Tests:** `CustomerOrderTests.cs` — order CRUD, line-to-job linkage, CoC PDF generation returns valid PDF, portal token-based access works without JWT, expired token rejected, order status transitions, customer code uniqueness, tenant isolation, required documents configuration persists, portal shows only linked customer's orders.

**Why it matters:** Customer order management closes the commercial loop: the system now covers the full lifecycle from customer PO → process execution → quality evidence → delivery. The CoC auto-generation feature alone saves hours per shipment for contract manufacturers and directly leverages the M4 PDF infrastructure. The customer portal creates a self-service channel that reduces "where's my order?" support load and positions the product as a supplier quality platform, not just an internal tool.

---

## Additional Market-Fit Feature Proposals (2026-04-22, post-M5)

Three features selected to maximize commercial leverage now that the full MVP (M1–M5) is complete and billing infrastructure is live. Each targets a specific revenue or retention lever tied to the subscription tiers defined in M5.

### F10 — Plan-Gated Feature Tiers & Usage Enforcement

**Pain it removes:** The billing infrastructure (M5) defines four subscription plans (Trial/Starter/Professional/Enterprise) but nothing in the application enforces the limits. A Trial tenant can create unlimited processes, add 50+ users, and run thousands of jobs — there is no technical gate matching the pricing table. This creates a revenue leak where trials never convert because they have no functional reason to upgrade, and paying customers on Starter have no incentive to move to Professional.

**Scope:**
- `PlanLimits` configuration (code-defined, not DB — changes with product releases): per-plan caps on users (Trial: 3, Starter: 25, Professional: 100, Enterprise: unlimited), processes (Trial: 1, Starter: unlimited), sites (Starter: 1, Professional: 3), executions/month (Trial: 50, Starter/Professional/Enterprise: unlimited), advanced modules (Trial/Starter: off, Professional/Enterprise: on).
- `PlanEnforcementService` — checks current tenant's subscription plan against `PlanLimits` for a given resource action. Returns `PlanCheckResult` (Allowed, AtLimit with upgrade prompt, Blocked with reason).
- `PlanEnforcementMiddleware` or controller-level attribute `[RequiresPlan(SubscriptionPlan.Professional)]` — validates before resource creation actions (user invite, process create, job create).
- `UsageMeteringService` — increments `UsageMetric` row for `JobExecutions` on every `Job.Complete` transition, `ActiveUsers` on daily unique login, `PdfExports` on every PDF endpoint hit.
- Upgrade prompt UI: when a user hits a plan limit, show a contextual modal ("You've reached the 3-user limit on Trial. Upgrade to Starter for up to 25 users.") with a link to `/billing`.
- `TenantFeatureFlags` integration: Professional plan auto-enables `ShowAdvancedModules`, `ShowProductionTools`, `ShowWarehouseTools`, `ShowTrainingTools`.
- Admin `/settings/plan` page showing current plan limits, usage against limits, and upgrade button.

**Entities / migrations:**
- No new entities — extends `TenantSubscription` and `UsageMetric` from M5. `Phase_Post_PlanTiers` migration adds `MaxUsers` and `MaxProcesses` computed columns to `TenantSubscription` (nullable — null means unlimited).

**Tests:** `PlanEnforcementTests.cs` — Trial tenant blocked from creating 4th user, Starter tenant can create 25th user, Professional tenant auto-enables advanced modules, usage metering increments on job completion, upgrade prompt returned in 402 response body, Enterprise plan has no limits, plan change propagates feature flags.

**Why it matters:** Without enforcement, the pricing tiers are fictional and trials never convert. This feature is the single most direct revenue driver — it turns the billing infrastructure from a payment collector into a growth engine. Every manufacturing SaaS competitor gates features by tier; without it, the product appears either free or overpriced.

---

### F11 — Scheduled Reports & Email Digest (Automated Reporting)

**Pain it removes:** Quality managers and operations leads currently must log in to the system and navigate to specific dashboards to get status updates. In manufacturing environments, many decision-makers are in meetings, on the floor, or travelling — they need key metrics pushed to them, not pulled. Competitors like MasterControl and ETQ offer scheduled email reports, and the absence of this feature is flagged in every enterprise evaluation.

**Scope:**
- `ReportSchedule` entity (Id, TenantId, Name, ReportType enum {DailyDigest, WeeklyQualityReview, MonthlyKpiSummary, OverdueActionItems, TrialExpiryReminder}, RecipientEmails JSON array, CronExpression, IsActive, LastRunAt, NextRunAt, TimeZone, CreatedByUserId).
- `ReportTemplate` — code-defined templates for each report type, composed from existing controller data:
  - **Daily Digest**: open jobs count, completed yesterday, overdue action items, new non-conformances, equipment downtime.
  - **Weekly Quality Review**: NC trend (opened/closed), RPN histogram shift, overdue CAPA summary, training compliance delta.
  - **Monthly KPI Summary**: throughput, yield, DPMO, MTTR, top 5 root causes, SPC out-of-control count, management review action completion rate.
  - **Overdue Action Items**: list of all overdue items with assignee, days overdue, source.
  - **Trial Expiry Reminder**: days remaining, usage summary, upgrade CTA (sent to tenant admin 7 days and 1 day before trial ends).
- `ReportGenerationService` (BackgroundService) — runs every minute, checks for due schedules, generates HTML email body from template + live data, sends via `IEmailSender` abstraction.
- `IEmailSender` abstraction with `SmtpEmailSender` (default) and `SendGridEmailSender` implementations — configured via `Email:Provider` in appsettings.
- `ReportSchedulesController` — CRUD, activate/deactivate, preview (returns HTML body without sending), send-now.
- `ReportScheduleList.razor` at `/reports/schedules` — manage schedules, preview modal, recipient management.
- Integration with M5 billing: Trial expiry reminder auto-created on signup, sends 7-day and 1-day warnings to admin email.

**Entities / migrations:**
- `ReportSchedule` (Id, TenantId, Name, ReportType, RecipientEmails, CronExpression, IsActive, LastRunAt, NextRunAt, TimeZone, CreatedByUserId).
- `ReportDelivery` (Id, ReportScheduleId, SentAt, RecipientCount, Status enum {Sent/Failed}, ErrorMessage nullable).
- `Phase_Post_ScheduledReports` EF migration.

**Tests:** `ScheduledReportTests.cs` ��� schedule CRUD, daily digest contains expected sections, trial expiry reminder sent at correct intervals, deactivated schedule not executed, send-now delivers immediately, recipient validation, tenant isolation of schedules, preview returns HTML without sending, report delivery logged.

**Why it matters:** Scheduled reports are a table-stakes enterprise feature and the #1 request from operations managers who are the budget holders. The trial expiry reminder directly drives conversion by creating urgency. The weekly quality review email keeps the product top-of-mind for quality managers who might otherwise forget to log in — reducing passive churn.

---

### F12 — API Key Self-Service & Developer Portal

**Pain it removes:** The system has a rich REST API and MCP server, but external integration requires either sharing JWT credentials (insecure) or direct database access (unsupported). Contract manufacturers and larger shops need to connect the system to their ERP (SAP, Oracle, JobBoss), MES, or BI tools (Power BI, Tableau). Today, there is no self-service way to create API keys, no rate limiting, no usage tracking per key, and no documentation portal. Every competitor in the manufacturing QMS space offers API keys — without them, the system is a walled garden.

**Scope:**
- `TenantApiKey` entity (Id, TenantId, Name, HashedKey SHA-256, Prefix first 8 chars for identification, Scopes JSON array of allowed endpoint patterns, RateLimitPerMinute default 60, IsActive, LastUsedAt, CreatedByUserId, CreatedAt, ExpiresAt nullable).
- `ApiKeyAuthenticationHandler` — custom ASP.NET authentication scheme reading `X-Api-Key` header, validates hash against DB, checks scope, enforces rate limit via in-memory sliding window (per key), sets tenant context from key's TenantId.
- `ApiKeysController` (Admin-only) — create (returns plain key once, stores hash), list (shows prefix + last used + scopes), revoke, update scopes/rate limit.
- `/settings/api-keys` Blazor page — create key modal (shows key once with copy button and warning), key list with scope editor, revoke confirmation, usage stats (calls last 24h from `UsageMetric`).
- API documentation page at `/api-docs` — auto-generated from Swagger/OpenAPI spec, styled for external developers, includes authentication instructions, example curl commands, webhook setup guide.
- Rate limiting middleware for API key requests — returns 429 Too Many Requests with `Retry-After` header.
- `UsageMetric` integration: every API key request increments `ApiCalls` metric, tagged with key prefix for per-key breakdown.

**Entities / migrations:**
- `TenantApiKey` (Id, TenantId, Name, HashedKey, Prefix, Scopes, RateLimitPerMinute, IsActive, LastUsedAt, CreatedByUserId, CreatedAt, ExpiresAt).
- `Phase_Post_ApiKeys` EF migration.

**Tests:** `ApiKeyTests.cs` — key creation returns plain key, subsequent calls use hashed lookup, revoked key returns 401, expired key returns 401, rate limit returns 429, scope enforcement blocks out-of-scope endpoints, API key sets correct tenant context, key list does not expose hash, concurrent requests within rate limit succeed, usage metric incremented per request.

**Why it matters:** API keys are the gateway to the integration ecosystem that drives enterprise adoption and stickiness. Once a customer connects the system to their ERP, switching costs increase dramatically — this is the #1 retention lever for the Professional and Enterprise tiers. It also enables a partner ecosystem where system integrators can build on top of the platform, creating a network effect that no competitor in the small-shop manufacturing QMS space has achieved.

---

## Additional Market-Fit Feature Proposals (2026-04-22, post-M3 completion)

Three features selected to exploit the newly-completed offline-capable execution surface (M3) and maximize the competitive gap in shop-floor usability that no incumbent manufacturing QMS offers.

### F13 — Offline-First Operator Shift Handover

**Pain it removes:** At shift changes, outgoing operators verbally relay in-progress job state to incoming operators — which steps are done, which measurements were flagged, which materials were consumed. This verbal handover fails silently when details are forgotten, and the incoming operator discovers problems mid-step. The existing execution wizard tracks per-step state but provides no structured handover summary.

**Scope:**
- `ShiftHandover` entity (Id, TenantId, OutgoingUserId, IncomingUserId, ShiftEndAt, ShiftStartAt, Status enum {Draft/Submitted/Acknowledged}, Notes, CreatedAt).
- `ShiftHandoverItem` entity (Id, ShiftHandoverId, StepExecutionId, ItemType enum {InProgress/FlaggedValue/PendingNC/MaterialShortage/EquipmentIssue}, Summary auto-generated, AcknowledgedByIncoming bool).
- `POST /api/shift-handovers/generate` — auto-populates handover items from all in-progress step executions assigned to the outgoing user: open NCs, flagged measurements (out-of-range values), pending pick-list lines, equipment downtime events.
- `ShiftHandover.razor` at `/shift-handover` — outgoing operator reviews generated items, adds free-text notes, submits. Incoming operator sees handover on login, acknowledges each item with checkbox.
- IndexedDB persistence via `operator-sync.js` — handover can be drafted offline during shift overlap when connectivity is spotty.
- Push notification integration: incoming operator receives a browser notification when a handover is submitted for them.

**Entities / migrations:**
- `ShiftHandover` (Id, TenantId, OutgoingUserId, IncomingUserId, ShiftEndAt, ShiftStartAt, Status, Notes, CreatedAt).
- `ShiftHandoverItem` (Id, ShiftHandoverId, StepExecutionId, ItemType, Summary, AcknowledgedByIncoming).
- `Phase_Post_ShiftHandover` EF migration.

**Tests:** `ShiftHandoverTests.cs` — auto-generation populates correct items from in-progress work, handover acknowledges item-by-item, submitted handover visible to incoming user, offline-drafted handover syncs on reconnect, cross-tenant isolation, only assigned incoming user can acknowledge.

**Why it matters:** Shift handover is the #1 source of quality escapes in 24/7 manufacturing operations. No competitor offers a structured digital handover — this feature alone differentiates the execution wizard from paper-based and legacy QMS systems. It also drives daily active usage by making the system the mandatory touchpoint at every shift change.

---

### F14 — Voice-Activated Measurement Entry

**Pain it removes:** Operators wearing gloves (CNC coolant, cleanroom PPE, chemical handling) cannot type measurements on a tablet. They must remove gloves, enter the value, re-glove — adding 15–30 seconds per measurement on a step with 10+ prompts. The M3 touch-target improvements help but don't solve the gloved-hands problem. Voice entry eliminates the physical interaction entirely.

**Scope:**
- `VoiceInput.razor` shared component using the Web Speech API (`SpeechRecognition` interface) via JS interop.
- Integration into `ExecutionWizardContent.razor` Phase 4: each `NumericEntry` prompt gains a microphone button that activates continuous speech recognition.
- `voice-input.js` JS module: starts recognition, filters for numeric patterns (regex extraction of decimal numbers from spoken text), confirms the parsed value with a spoken readback via `SpeechSynthesis`, auto-submits to the prompt value on confirmation.
- Noise cancellation hint via `SpeechRecognition.continuous = false` and `interimResults = true` — shows live transcription so the operator can see what was heard before it's committed.
- Fallback: if Web Speech API is unavailable (Firefox, some Android WebViews), the mic button is hidden and standard input remains.
- Language configuration per tenant via `TenantBranding.SpeechLocale` (default `en-US`).
- Audit trail: `PromptResponse.EntryMethod` enum extension (Manual/Barcode/Voice) — stored alongside the response value for traceability.

**Entities / migrations:**
- `EntryMethod` enum (Manual/Barcode/Voice/Photo) on `PromptResponse`.
- `SpeechLocale` nullable string on `TenantBranding`.
- `Phase_Post_VoiceInput` EF migration.

**Tests:** `VoiceInputTests.cs` — EntryMethod persisted correctly on prompt response, SpeechLocale round-trip on TenantBranding, numeric extraction from spoken text (JS unit tests), fallback hides mic button when API unavailable, voice-entered values pass same validation as manual entry.

**Why it matters:** Hands-free data entry is the killer feature for shop-floor adoption in the CNC and medical-device segments where PPE is mandatory. No manufacturing QMS competitor offers voice input — this is a genuine first-mover advantage. It also dramatically reduces measurement entry time, making the system measurably faster than paper for the first time.

---

### F15 — Geo-Fenced Execution & Location-Aware Job Assignment

**Pain it removes:** Multi-site tenants (Professional plan) and large single-site operations need to ensure that operators execute jobs only when physically present at the correct workstation or area. Today, an operator could complete a step from the break room or even off-site — there is no location verification. For regulated environments (FDA 820, AS9100), location evidence strengthens the audit record. For operations managers, location data enables real-time workforce visibility.

**Scope:**
- `WorkstationGeofence` entity (Id, FloorPlanWorkstationId FK, Latitude, Longitude, RadiusMeters default 50, IsEnforced bool) — links to the existing Phase 22 `FloorPlanWorkstation`.
- `StepExecution.StartLatitude`/`StartLongitude`/`LocationAccuracyMeters` nullable fields — captured on step start via Geolocation API.
- `location-check.js` JS module: requests `navigator.geolocation.getCurrentPosition` with high accuracy, returns coordinates to Blazor via JS interop.
- ExecutionWizard integration: on step start, if the step's assigned workstation has an enforced geofence, verify the operator's location is within radius. If outside, show a warning (soft enforcement) or block execution (hard enforcement based on `IsEnforced`).
- `LocationDashboard.razor` at `/floor-status/locations` — real-time view of operator positions overlaid on the floor plan canvas (reuses `factory-canvas.js`), showing which operators are at which workstations.
- `JobAssignmentService` enhancement: when an operator requests work via MyWork/Portal, geo-filter to only show jobs assigned to workstations within their current proximity.
- Privacy controls: location is only captured during active step execution, never in background. `TenantFeatureFlags.EnableLocationTracking` toggle. Location data auto-purged after configurable retention period (default 90 days).

**Entities / migrations:**
- `WorkstationGeofence` (Id, FloorPlanWorkstationId, Latitude, Longitude, RadiusMeters, IsEnforced).
- `StartLatitude`, `StartLongitude`, `LocationAccuracyMeters` nullable on `StepExecution`.
- `EnableLocationTracking` on `TenantFeatureFlags`.
- `Phase_Post_Geofence` EF migration.

**Tests:** `GeofenceTests.cs` — geofence CRUD, location within radius passes check, location outside radius triggers warning/block, enforced vs. soft enforcement behavior, step execution captures coordinates, location data respects tenant isolation, feature flag toggle enables/disables location capture, privacy purge removes old location data.

**Why it matters:** Location verification is a growing regulatory expectation in pharmaceutical and medical-device manufacturing (FDA 21 CFR Part 11 electronic records). It's also the bridge between the existing Floor Plan (Phase 22) and real-time operations — turning the static floor plan into a live operational dashboard. For multi-site Professional-tier customers, geo-fencing ensures process discipline across locations without manual supervisor oversight.

---

## Additional Market-Fit Feature Proposals (2026-04-22, post-F10 completion)

Three features selected to maximize the commercial impact of the newly-enforced plan tiers (F10). Each directly drives trial-to-paid conversion, reduces churn, or unlocks a new revenue stream by leveraging the plan enforcement infrastructure.

### F16 — In-App Upgrade Flow & Self-Service Plan Management

**Pain it removes:** The billing infrastructure (M5) and plan enforcement (F10) can block users at limits and show upgrade prompts, but there is no in-app mechanism to actually change plans. Admins must contact support or navigate to the Stripe billing portal — a context switch that kills conversion momentum. Every SaaS product with self-service revenue needs a frictionless upgrade path directly from the limit-reached modal.

**Scope:**
- `PlanSelectionPage.razor` at `/billing/plans` — side-by-side plan comparison card layout (Trial/Starter/Professional/Enterprise) showing: feature matrix (users, processes, sites, executions, modules), current plan highlighted, "Current Plan" badge, upgrade/downgrade CTAs. Responsive for tablet.
- `UpgradeModal.razor` shared component — triggered by 402 plan-limit responses anywhere in the app. Shows the contextual limit ("You've reached 3 users on Trial"), the next plan's benefits, and a "Upgrade Now" button that navigates to `/billing/plans` or directly creates a Stripe Checkout session.
- `POST /api/billing/checkout-session` endpoint — creates a Stripe Checkout Session for the selected plan, returns the session URL for client-side redirect. Pre-fills tenant email and applies any active promotional coupon.
- `POST /api/billing/change-plan` endpoint — handles plan downgrades by updating the `TenantSubscription.PlanCode` at period end (via Stripe subscription schedule), immediately syncs feature flags via `SyncFeatureFlagsForPlan`.
- `BillingController` extended with plan change history endpoint (`GET /api/billing/plan-changes`) for audit trail.
- Downgrade safeguard: when switching to a lower plan, check if current usage exceeds the target plan's limits (e.g., 50 users on Professional → Starter allows 25). Show a warning listing what will be affected but don't block — enforce at next resource creation.
- Promotional coupon support: `TenantSubscription.CouponCode` nullable field, applied at checkout session creation.

**Entities / migrations:**
- `CouponCode` nullable string on `TenantSubscription`.
- `PlanChangeLog` entity (Id, TenantId, FromPlan, ToPlan, ChangedAt, ChangedByUserId, Reason nullable).
- `Phase_Post_UpgradeFlow` EF migration.

**Tests:** `UpgradeFlowTests.cs` — checkout session creation returns valid URL, plan change updates subscription, downgrade warning triggered when usage exceeds target limits, feature flags synced on plan change, coupon code applied at checkout, plan change logged in audit, non-admin cannot change plan, concurrent plan changes are idempotent.

**Why it matters:** The upgrade flow is the revenue bridge between F10's enforcement gates and actual payment. Without it, every 402 response is a dead end that drives frustration instead of conversion. Self-service plan changes reduce support load and enable impulse upgrades — the user hits the wall, sees the upgrade modal, and converts in under 60 seconds. This is the single highest-ROI feature for post-MVP revenue.

---

### F17 — Tenant Admin Dashboard & Usage Analytics

**Pain it removes:** Tenant admins (the buyer persona) have no visibility into how their team uses the system. They can't answer "are we getting value from this?" at renewal time, can't identify underutilized features to drive adoption, and can't proactively manage their plan limits before hitting a wall. The existing Dashboard page shows operational KPIs (jobs, throughput) but nothing about subscription health, user activity, or feature adoption.

**Scope:**
- `TenantDashboard.razor` at `/admin/dashboard` (Admin-only) — single-page admin overview with:
  - **Plan & Usage section**: current plan badge, usage bars for each metered resource (users/processes/executions) against plan limits (from `PlanEnforcementService`), days remaining in trial/billing period, payment status indicator.
  - **User Activity section**: active users last 7/30 days (from daily `ActiveUsers` usage metric), last-login list (top 10 most recent + top 10 most stale), role distribution pie chart.
  - **Feature Adoption section**: feature flags enabled vs. available for current plan, module-level usage (which NavMenu sections are visited — tracked via lightweight `PageView` metric), recommendation cards ("You have Production Tools enabled but haven't created any Equipment — get started →").
  - **Quick Actions**: invite user, upgrade plan, manage billing, toggle modules.
- `GET /api/admin/dashboard` endpoint — aggregates subscription, usage metrics, user activity, and feature flag data into a single `TenantAdminDashboardDto`.
- `UsageMetricType.PageViews` enum extension — lightweight client-side tracking via a `PageViewService` that increments a metric on each Blazor navigation (grouped by module: Quality/Production/Warehouse/Training/Reports/Admin).
- `UserActivitySummary` computed from `ApplicationUser.LastLoginAt` (new nullable field) — updated on each successful login in `AuthController.Login`.

**Entities / migrations:**
- `LastLoginAt` nullable DateTime on `ApplicationUser`.
- `PageViews` value added to `UsageMetricType` enum.
- `Phase_Post_TenantDashboard` EF migration.

**Tests:** `TenantDashboardTests.cs` — dashboard returns correct plan limits, user count matches reality, active user count computed from login timestamps, feature adoption recommendations generated for unused modules, page view metric incremented, non-admin cannot access dashboard, cross-tenant isolation of dashboard data.

**Why it matters:** The admin dashboard is the retention surface — it's what the buyer sees when deciding whether to renew. By showing concrete usage data and adoption metrics, it arms the internal champion with the evidence they need to justify the subscription at budget review. The feature adoption recommendations also drive engagement by surfacing unused capabilities, reducing the "we only use 20% of it" churn objection.

---

### F18 — Team & Role-Based Seat Management

**Pain it removes:** The current user management is a flat list — admins can create users with roles (Admin/Engineer/Participant) but there's no concept of seat allocation, team grouping, or role-based billing. When a Starter plan allows 25 users, the admin can't see how seats are distributed across roles, can't reserve seats for specific roles, and has no visibility into which users are actually active vs. consuming a seat but never logging in. The OrgUnit structure (Phase 12) provides organizational hierarchy but doesn't connect to billing seats.

**Scope:**
- `SeatAllocation` entity (Id, TenantId, Role string, AllocatedCount int, UsedCount computed) — per-role seat pools within the overall plan user limit. Admin allocates e.g. 5 Admin + 5 Engineer + 15 Participant = 25 on Starter.
- `UserStatus` enum extension on `ApplicationUser`: Active/Invited/Deactivated. Deactivated users don't count toward seat limit but retain data. Invited users count toward limit but haven't accepted yet.
- `SeatManagement.razor` at `/admin/seats` — visual seat allocation interface showing: total seats (from plan), allocated per role (editable sliders), used per role, available per role, list of deactivated users (re-activate button), list of invited-but-pending users.
- `POST /api/admin/seats/allocate` — sets role-based seat allocations. Validates total doesn't exceed plan limit.
- `PATCH /api/auth/users/{id}/deactivate` — sets `UserStatus = Deactivated`, revokes active sessions (by blacklisting JWT jti), frees the seat. User data preserved.
- `PATCH /api/auth/users/{id}/reactivate` — checks seat availability for user's role before reactivating.
- `AuthController.Register` extended: checks role-specific seat allocation (not just total user count) before creating user.
- `PlanEnforcementService.CheckAsync(PlanResource.Users)` enhanced: considers both total plan limit AND per-role seat allocation.
- Seat utilization report: `GET /api/admin/seats/utilization` — returns per-role breakdown with last-login data, enabling admin to identify and deactivate dormant seats.

**Entities / migrations:**
- `SeatAllocation` (Id, TenantId, Role, AllocatedCount).
- `UserStatus` enum (Active/Invited/Deactivated) on `ApplicationUser` (default Active).
- `Phase_Post_SeatManagement` EF migration.

**Tests:** `SeatManagementTests.cs` — seat allocation respects plan limit, role-specific seat check blocks over-allocated role, deactivation frees seat, reactivation checks availability, deactivated user cannot login, seat utilization report returns correct counts, total allocation cannot exceed plan limit, cross-tenant seat isolation.

**Why it matters:** Seat management transforms the flat user limit from a blunt gate into a flexible resource that admins can actively manage. It directly reduces "I hit the user limit but half my users are inactive" support tickets — the #1 billing-related complaint in SaaS products with per-seat pricing. It also creates a natural upsell trigger: when seat utilization is high across all roles, the admin sees the constraint and is primed to upgrade. For Professional-tier customers with 100 seats, role-based allocation is a governance feature that IT departments expect.
