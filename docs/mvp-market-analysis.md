# Sequencer — Market Need, MVP Definition & Target Customers

---

## 1. The Problem Being Solved

### The Core Pain

Manufacturing operations run on process knowledge that is almost universally stored in the wrong places: Word documents, Excel spreadsheets, tribal knowledge, sticky notes, and aging paper travelers. The result is a set of compounding problems that are expensive, dangerous, and invisible until something goes wrong.

**Process designs are disconnected from execution.** A process engineer documents a procedure; a floor operator follows a different version. There is no structural link between "what we designed" and "what is actually happening."

**Quality tools are islands.** PFMEA, Control Plans, and Cause & Effect matrices are maintained by different teams in different files. They reference each other in theory but drift apart in practice. When a process changes, no one knows which quality documents are stale.

**WIP is tracked by memory or whiteboard.** Operators, supervisors, and schedulers have no live view of where parts are in the process, what their disposition is, or what needs to happen next. Decisions are made on stale information.

**Compliance requires evidence that does not exist in structured form.** ISO 9001, AS9100, IATF 16949, and FDA 21 CFR Part 820 all require documented procedures, objective evidence of execution, and demonstrable control. Manual record-keeping creates enormous audit-prep burden.

**Operators have no guided execution experience.** Workers are handed a procedure document and expected to perform the job correctly. There is no system that walks them through the steps, enforces data collection, prevents out-of-sequence work, or surfaces the right information at the right moment.

### What Sequencer Does

Sequencer replaces the disconnected stack — the Word SOPs, the Excel trackers, the paper travelers, the siloed quality files — with a single, coherent system where:

- **Process designs are first-class objects.** Steps, ports, flows, and routing decisions are structured data, not prose.
- **Quality tools are derived from process data.** PFMEA failure modes, C&E matrix variables, and Control Plan entries are generated from and linked to the process definition — not maintained separately.
- **Execution is guided and gated.** Operators work through a step-by-step wizard. Data is collected with validation. Steps cannot be completed out of order. Work-in-progress is always traceable.
- **Every part has a traceable history.** Every Item and Batch carries a full record of every step executed, every measurement taken, every operator who touched it.
- **Management has a live operational picture.** Dashboards, alerts, process timing reports, and bottleneck analysis replace manual status meetings.

Sequencer's unique positioning is that it treats the **process model** as the authoritative source of operational truth and derives everything else — execution, quality engineering, compliance evidence, analytics, training — from that model.

---

## 2. Minimum Viable Product

The MVP is the smallest deployable version that delivers enough core value to win an initial paying customer and validate the fundamental product thesis: that a structured process model can replace disconnected documentation and generate demonstrable ROI.

### MVP Scope

#### Must Have (MVP)

**Process Design (the model)**
- Kind/Grade type system — catalog what things are and what condition they carry
- Step Template library with typed Input/Output ports (Material, Parameter, Characteristic, Condition)
- Linear Process composition: sequence steps, define flows between ports
- Process version tracking and release/approval lifecycle (Draft → Released)

**Guided Operator Execution**
- Job creation against a released Process
- Step-by-step execution wizard: one step at a time, enforces sequence, shows work instructions
- Structured data collection (numeric measurements, pass/fail, barcode scan, photos) with validation against limits
- WIP tracking: every Item flows through typed ports with grade transitions recorded

**Basic Quality Integration**
- PFMEA builder linked to step/port definitions
- Control Plan builder with characteristics derived from process ports
- Out-of-limit alerting on recorded measurements

**Operator & Engineer Roles**
- Role separation: Participant (execute only) vs. Engineer (design + execute) vs. Admin
- Simple org structure for assigning work areas to process nodes

**Minimal Dashboard**
- Job status counts (Created / In Progress / Completed)
- Recent completions
- Active alerts count

#### Out of Scope for MVP

| Feature | Rationale |
|---|---|
| Workflow graphs (multi-process routing) | Linear processes cover most SMB use cases; defer branching/rework workflows |
| Warehouse Management (inventory, pick lists) | Adds significant complexity; not needed to demonstrate core value |
| Equipment Catalog & Maintenance | Important but separable; add in V1.1 |
| Audit Programs & Standards Conformance | Derived value; requires established processes first |
| Training & Competency Management | Phase 2 feature once operators are executing in the system |
| Factory Design Suite (floor plan canvas) | Significant engineering investment; not core to MVP value prop |
| AI Integration & MCP Server | Premium feature for established customers |
| Webhooks & External Integrations | Post-MVP expansion |
| Material Review Board | Requires mature NC process first |
| Scheduled Workflow Execution | Complexity without base execution established |
| 3D Model Viewer | Nice to have; not a decision driver |
| Analytics Chart Builder | Standard dashboard sufficient for MVP |

### MVP Success Criteria

1. A process engineer can define a complete manufacturing process (5–20 steps) in under 2 hours, with no training beyond onboarding documentation.
2. An operator can execute a job against that process on a tablet without paper, and the system captures all required measurements.
3. A supervisor can view live job status and completed job history from a browser.
4. The system generates a PFMEA from the process definition with less than 30 minutes of additional input.
5. The system can demonstrate traceability of a single serialized part through its complete process history on demand.

### MVP Target Price Point

- **SaaS:** $300–600 / month per site (unlimited processes, unlimited executions, up to 25 users) — positions below MES/ERP integrations and above spreadsheets
- **Per-seat alternative:** $25–40 / user / month for larger operations

---

## 3. New Goals & Tasks

Based on the MVP definition above, the following represent the prioritized gaps to close before first customer delivery.

**Implementation plan:** See [mvp-implementation-plan.md](mvp-implementation-plan.md) for sequencing, task breakdown, and test requirements.

**Progress Summary** (updated as phases ship):

| Phase | Goal | Status |
|-------|------|--------|
| M1 | Multi-Tenant Isolation (Goal 4) | ⬜ Not started |
| M2 | Onboarding Wizard (Goal 1) | ⬜ Not started |
| M3 | Execution Wizard Polish (Goal 2) | ⬜ Not started |
| M4 | PFMEA/Control Plan PDF Export (Goal 3) | ⬜ Not started |
| M5 | Billing Infrastructure (Goal 5) | ⬜ Not started |

### Goal 1: Simplify Onboarding to 30 Minutes

**Why:** The product has significant depth. A first-time user encountering all 22+ phases simultaneously will be lost. MVP needs a guided setup flow.

**Tasks:**
- Build a first-run wizard: create first Kind → create first Step → build first Process → run first Job
- Pre-populate a 3–5 step sample process on new tenant creation (e.g., "Widget Inspection")
- Reduce NavMenu to MVP features only; gate advanced modules behind a settings toggle

### Goal 2: Polish the Execution Wizard for Field Use

**Why:** The operator experience is the primary daily touchpoint. It must work reliably on tablets and survive intermittent connectivity.

**Tasks:**
- End-to-end tablet UX audit on iOS Safari and Android Chrome
- Offline-capable data capture for measurements (queue and sync on reconnect)
- Large touch targets on all wizard controls (minimum 44px)
- Step completion confirmation pattern that prevents accidental advancement

### Goal 3: Produce a Shareable PFMEA/Control Plan PDF

**Why:** Quality engineers need to share these documents with customers and auditors. A system-generated, properly formatted PDF is a differentiator.

**Tasks:**
- PFMEA PDF export (tabular, AIAG-standard column order)
- Control Plan PDF export
- PDF header/footer with process name, revision, effective date, approver

### Goal 4: Multi-Tenant Isolation

**Why:** SaaS requires hard data boundaries between customers.

**Tasks:**
- Tenant ID on all entities with row-level security enforced at the repository layer
- Tenant provisioning flow (invite link → account creation → first login)
- Tenant-scoped admin user management

### Goal 5: Pricing & Billing Infrastructure

**Tasks:**
- Stripe integration for subscription management
- Usage-based metering (job executions per month) for transparency
- Free trial tier: 1 process, 50 job executions, 3 users, 30 days

---

## 4. Potential Customer Segments & Named Examples

### Segment 1: Precision Machining Shops (CNC)

**Why they need it:** High-mix, low-volume shops produce unique or small-batch parts with tight tolerances. They have ISO 9001 or IATF 16949 requirements but cannot afford enterprise MES. Paper travelers and spreadsheets cause first-article failures, rework loops, and audit findings.

**Key pain points:** Traceability of serialized parts, dimensional inspection records, operator sign-off, PFMEA for new part onboarding.

**Example customers:**
- Custom precision machining shops serving aerospace/defense primes (Boeing, Lockheed suppliers)
- Medical device component manufacturers (Class II device machined parts)
- Automotive Tier 2/Tier 3 suppliers making brackets, housings, fastener assemblies

**Approximate market:** 30,000+ job shops in the US alone; majority under 50 employees with no MES

---

### Segment 2: Electronics Assembly (PCBA/Box Build)

**Why they need it:** PCB assembly operations have complex, multi-step processes (stencil print → pick and place → reflow → AOI → hand solder → ICT → functional test). Each step has critical parameters (paste volume, reflow profile, temperature) and binary pass/fail gates. Traceability to lot numbers, component reels, and board serial numbers is required by customers.

**Key pain points:** Parameter capture per board, lot traceability for component COCs, first-article documentation, IPC-A-610 inspection records.

**Example customers:**
- Contract electronics manufacturers (CEMs) serving IoT, medical, industrial
- In-house PCBA operations at hardware startups scaling from prototype to production
- Defense electronics assemblers (IPC-A-610 Class 3 requirements)

---

### Segment 3: Medical Device Manufacturers (FDA-regulated)

**Why they need it:** 21 CFR Part 820 (QSR) and ISO 13485 mandate documented procedures, device history records (DHRs), and demonstrable process control. A DHR must be producible on demand for any serialized unit. Most Class II device manufacturers outside top-tier companies maintain DHRs manually.

**Key pain points:** DHR generation, lot/serial traceability, deviation documentation, CAPA linkage, audit readiness.

**Example customers:**
- Surgical instrument manufacturers
- Orthopedic implant component suppliers
- Diagnostic device assembly operations
- Single-use device molding and assembly

**Note:** This segment requires 21 CFR Part 11 compliance (electronic signatures, audit trail) — a defined extension of the current signature prompt type and audit trail infrastructure already present in the system.

---

### Segment 4: Aerospace & Defense Manufacturers (AS9100)

**Why they need it:** AS9100 Rev D requires First Article Inspection (FAI) per AS9102, risk management (PFMEA), configuration control, and objective evidence of conformance at every step. Suppliers to Boeing, Lockheed, Northrop, Raytheon face rigorous DCMA/DCAA surveillance.

**Key pain points:** FAI documentation, PFMEA maintenance, non-conformance disposition, SCAR generation, government property tracking.

**Example customers:**
- Structural fabrication shops (sheet metal, composites)
- Avionics assembly and test operations
- MRO (maintenance, repair, overhaul) facilities
- Defense electronics integrators

---

### Segment 5: Food & Beverage Processing

**Why they need it:** FSMA (Food Safety Modernization Act), HACCP plans, and SQF/BRC certification require documented critical control points, temperature and parameter records, batch traceability, and allergen control. Most small-to-mid processors use paper log sheets that are never analyzed.

**Key pain points:** Batch traceability (lot codes, expiry), CCP monitoring records, sanitation step documentation, recall readiness.

**Example customers:**
- Co-manufacturing facilities (contract packagers for branded goods)
- Specialty food producers (sauces, snacks, beverages) scaling past SQF Level 1
- Nutraceutical manufacturers (cGMP compliance requirements)
- Craft breweries and distilleries with state/federal compliance requirements

---

### Segment 6: Pharmaceutical & Biotech Manufacturing

**Why they need it:** FDA 21 CFR Parts 210/211 (cGMP for drugs) require batch records, deviation records, and complete manufacturing traceability. Batch record review is a manual, paper-intensive process at most facilities below top-20 pharma.

**Key pain points:** Electronic batch records (EBR), deviation capture, in-process checks, CoA generation, audit trail.

**Example customers:**
- Contract Development and Manufacturing Organizations (CDMOs) — early-phase clinical manufacturers
- Compounding pharmacies scaling to 503B outsourcing facilities
- Dietary supplement manufacturers (NSF, USP verification programs)
- Cell therapy and biologics manufacturing startups

---

### Segment 7: Metal Fabrication & Welding Shops

**Why they need it:** AWS D1.1, ASME Section IX, and customer weld procedure qualifications require documented Weld Procedure Specifications (WPS), Procedure Qualification Records (PQR), and Welder Performance Qualifications (WPQ). Shops track this on paper and routinely fail ASME surveys.

**Key pain points:** WPS/PQR traceability, welder certification tracking, NDT results per weld joint, material traceability (heat/cert numbers).

**Example customers:**
- Structural steel fabricators (bridge, building, pressure vessel)
- Pipe fabrication shops (power, petrochemical)
- Pressure vessel and heat exchanger manufacturers (ASME "U" stamp)
- Custom weldment shops serving oil & gas

---

### Segment 8: Plastics Injection Molding

**Why they need it:** Automotive and medical customers require PPAP (Production Part Approval Process) submissions including process capability studies, control plans, and measurement system analysis. Most tier-2/3 molders maintain PPAP documents in desktop files, never linked to actual production data.

**Key pain points:** Shot parameter capture (injection pressure, melt temp, cycle time), first-article dimensional data, process capability reporting, PPAP documentation assembly.

**Example customers:**
- Automotive interior and underhood component molders
- Medical-grade resin molders (Class VI biocompatibility required)
- Consumer electronics enclosure molders

---

### Segment 9: Small-Batch & Custom Assembly Operations

**Why they need it:** Makers, contract assemblers, and specialty manufacturers build complex, low-volume products (industrial equipment, robotics, custom electronics) where each unit has a unique configuration. Tracking what went into each unit, who assembled it, and what tests it passed is impossible without a structured system.

**Key pain points:** Serial number tracking, assembly verification (right part, right orientation, right torque), final test results per unit, customer-facing build records.

**Example customers:**
- Industrial automation integrators
- Custom robotics assemblers
- Specialty vehicle upfitters (emergency vehicles, defense vehicles)
- High-value consumer electronics (audio, imaging, scientific instruments)

---

## 5. Go-to-Market Wedge

The strongest initial entry point is **ISO 9001-certified small manufacturers (25–200 employees)** who already have compliance obligations but cannot afford or justify an enterprise MES (Plex, Epicor, SAP ME). This segment:

- Has a defined budget for quality systems (annual audit cost alone justifies the investment)
- Experiences the paper-vs-digital pain acutely
- Is reachable through quality trade associations (ASQ), LinkedIn, and manufacturing conferences (FABTECH, IMTS)
- Has a clear purchase trigger: upcoming surveillance audit, new customer quality requirement, or failed audit finding

A single customer story — "replaced 47 paper travelers and three Excel trackers, passed ISO surveillance with zero major findings" — is a more powerful sales asset than any feature list.
