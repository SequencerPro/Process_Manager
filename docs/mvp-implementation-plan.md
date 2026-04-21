# Sequencer — MVP Implementation Plan

This document operationalizes the five MVP goals defined in [mvp-market-analysis.md](mvp-market-analysis.md) into sequenced engineering phases with concrete tasks, test requirements, and completion criteria.

## Version History

| Version | Date       | Notes |
|---------|------------|-------|
| 0.1     | 2026-04-20 | Initial plan — 5 MVP phases (M1–M5), sequencing rationale, task breakdown, test strategy |

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
| M2 Onboarding Wizard | ⬜ Not started | — | — | — |
| M3 Execution Wizard Polish | ⬜ Not started | — | — | — |
| M4 PDF Export | ⬜ Not started | — | — | — |
| M5 Billing Infrastructure | ⬜ Not started | — | — | — |

Legend: ⬜ Not started · 🟨 In progress · ✅ Complete
