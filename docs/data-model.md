# Process Manager — Data Model (Phases 1–5)

## Version History

| Version | Date       | Notes                          |
|---------|------------|--------------------------------|
| 0.1     | 2026-02-16 | Initial draft — Phases 1–3     |
| 0.2     | 2026-02-16 | Added Phase 5 — Execution      |
| 0.3     | 2026-02-16 | Added Phase 4 — Workflows      |
| 0.4     | 2026-02-22 | Corrections: RoutingType values (Always/GradeBased/Manual, not Sequential); noted JobName/BatchCode denormalized fields on Item/Batch response DTOs for display performance |
| 0.5     | 2026-02-27 | Extended Port model with PortType enum (Material, Parameter, Characteristic, Condition); Kind/Grade/QtyRule fields now Material-only; added DataType/Units/NominalValue/Tolerance fields for Parameter and Characteristic ports |
| 0.6     | 2026-03-08 | Phase 4 extensions for workflow execution: OrgUnit entity, assignee_id on WorkflowProcess, new WorkflowJob entity (with schedule_id), WorkflowSchedule entity; Phase 5 extension: workflow_job_id and workflow_process_id on Job |
| 0.7     | 2026-03-08 | Phase 2 additions: PromptDefinition and PromptOption entities; ExecutionData: prompt_definition_id FK, value widened to text, extended DataType enum (Select, MultiSelect, Barcode, Photo, Signature) |

---

## Overview

This document defines the entities, attributes, relationships, and constraints for the Process Manager system:

- **Phase 1:** Type System (Kind, Grade, Tracking)
- **Phase 2:** Step Design (Step, Port, Quantity Rule)
- **Phase 3:** Process Composition (Process, Process Step, Flow)
- **Phase 4:** Workflow Composition (Workflow, WorkflowProcess, WorkflowLink, WorkflowLinkCondition)
- **Phase 5:** Execution / Runtime (Job, Item, Batch, Step Execution, Port Transaction, Execution Data)

Each entity includes a unique identifier (`id`), audit fields (`created_at`, `updated_at`), and a `version` where applicable.

---

## Entity-Relationship Diagram (Text)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 1: TYPE SYSTEM                                                   │
│                                                                         │
│  ┌──────────┐        ┌──────────┐        ┌──────────────────┐          │
│  │   Kind   │ 1───*  │  Grade   │        │ DomainVocabulary │          │
│  └──────────┘        └──────────┘        └──────────────────┘          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 2: STEP DESIGN                                                   │
│                                                                         │
│  ┌──────────────┐ 1───* ┌──────────┐                                   │
│  │ StepTemplate │───────│   Port   │                                   │
│  └──────────────┘       └──────────┘                                   │
│                              │                                          │
│                              │ references                               │
│                              ▼                                          │
│                     Kind + Grade (Item Type)                            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 3: PROCESS COMPOSITION                                           │
│                                                                         │
│  ┌──────────┐ 1───* ┌─────────────┐                                    │
│  │ Process  │───────│ ProcessStep │──references──▶ StepTemplate        │
│  └──────────┘       └─────────────┘                                    │
│                           │                                             │
│                      1────┼────1                                        │
│                           ▼                                             │
│                      ┌──────────┐                                       │
│                      │   Flow   │ (output port → input port             │
│                      └──────────┘  between adjacent ProcessSteps)       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 4: WORKFLOW COMPOSITION                                          │
│                                                                         │
│  ┌──────────┐ 1───* ┌──────────────────┐ *───1 ┌──────────┐           │
│  │ Workflow │───────│ WorkflowProcess  │───────│ Process  │ (Phase 3) │
│  └──────────┘       └──────────────────┘       └──────────┘           │
│       │                    ▲         ▲                                  │
│       │ 1                  │ source   │ target                          │
│       │                    │          │                                  │
│       └───* ┌──────────────────┐──────┘                                │
│             │  WorkflowLink    │                                       │
│             └──────────────────┘                                       │
│                    │ 1                                                  │
│                    │                                                    │
│                    └───* ┌──────────────────────────┐                  │
│                          │ WorkflowLinkCondition    │                  │
│                          └──────────────────────────┘                  │
│                                  │ *───1 Grade (Phase 1)               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1 Entities

### Kind

Defines what something physically or logically is.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| code           | string(50)  | Unique, Not Null               | Short identifier (e.g., "WDG-100")             |
| name           | string(200) | Not Null                       | Human-readable name (e.g., "Widget")           |
| description    | text        |                                | Detailed description                           |
| is_serialized  | boolean     | Not Null, Default: false       | Whether individual items get unique IDs         |
| is_batchable   | boolean     | Not Null, Default: false       | Whether items can be grouped into batches       |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

### Grade

The condition or qualification an item carries. Defined per Kind.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| kind_id        | UUID        | FK → Kind, Not Null            | The Kind this Grade belongs to                  |
| code           | string(50)  | Not Null                       | Short identifier (e.g., "PASS")                |
| name           | string(200) | Not Null                       | Human-readable name (e.g., "Passed")           |
| description    | text        |                                | Detailed description                           |
| is_default     | boolean     | Not Null, Default: false       | Whether this is the default Grade for the Kind  |
| sort_order     | integer     | Not Null, Default: 0           | Display ordering                               |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- Unique on (kind_id, code)
- Each Kind must have at least one Grade
- At most one Grade per Kind may have is_default = true

### DomainVocabulary

Maps system terms to domain-specific labels.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| name           | string(200) | Unique, Not Null               | Vocabulary name (e.g., "Semiconductor")         |
| term_kind      | string(100) | Not Null                       | Label for Kind (e.g., "Product")               |
| term_kind_code | string(100) | Not Null                       | Label for Kind code (e.g., "Product Code")     |
| term_grade     | string(100) | Not Null                       | Label for Grade (e.g., "Qualification")        |
| term_item      | string(100) | Not Null                       | Label for Item (e.g., "Unit")                  |
| term_item_id   | string(100) | Not Null                       | Label for Item ID (e.g., "Wafer ID")           |
| term_batch     | string(100) | Not Null                       | Label for Batch (e.g., "Lot")                  |
| term_batch_id  | string(100) | Not Null                       | Label for Batch ID (e.g., "Lot Number")        |
| term_job       | string(100) | Not Null                       | Label for Job (e.g., "Work Order")             |
| term_workflow  | string(100) | Not Null                       | Label for Workflow (e.g., "Process Flow")      |
| term_process   | string(100) | Not Null                       | Label for Process (e.g., "Process")            |
| term_step      | string(100) | Not Null                       | Label for Step (e.g., "Operation")             |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

---

## Phase 2 Entities

### StepTemplate

A reusable definition of a unit of work. "Template" distinguishes the design-time definition from a runtime execution.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| code           | string(50)  | Unique, Not Null               | Short identifier (e.g., "INSP-DIM-01")        |
| name           | string(200) | Not Null                       | Human-readable name (e.g., "Dimensional Inspection") |
| description    | text        |                                | Detailed work instructions / description       |
| pattern        | enum        | Not Null                       | One of: Transform, Assembly, Division, General |
| version        | integer     | Not Null, Default: 1           | Version number for change tracking              |
| is_active      | boolean     | Not Null, Default: true        | Whether this template is available for use      |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

**Notes:**
- The `pattern` field is informational/classificatory. The actual behavior is determined by the port configuration. A StepTemplate marked as "Transform" that has 2 input ports would be caught by validation.
- A `StepTemplate` has two independent extension points: **Ports** (quality-tool connection points for PFMEA, C&E, Control Plan) and **PromptDefinitions** (operator data-collection form fields). Both hang off the same `StepTemplate` but serve different layers and neither requires the other.

### Port

A named connection point on a StepTemplate. Every port has a `port_type` that governs which additional fields are required.

**PortType values:**

| PortType       | Represents                            | Quality tool role                          |
|----------------|---------------------------------------|--------------------------------------------|
| Material       | Physical workpiece or batch           | WIP tracking, PFMEA material causes        |
| Parameter      | A controllable input setting (X)      | C&E X-axis, PFMEA causes, Control Plan     |
| Characteristic | A measurable output/feature (Y)       | C&E Y-axis, PFMEA effects, Control Plan    |
| Condition      | A binary pass/fail prerequisite       | PFMEA error-proofing, Control Plan checks  |

| Attribute          | Type        | Constraints                         | Applies To              | Description                                    |
|--------------------|-------------|-------------------------------------|-------------------------|------------------------------------------------|
| id                 | UUID        | PK                                  | All                     | Unique identifier                              |
| step_template_id   | UUID        | FK → StepTemplate, Not Null         | All                     | The StepTemplate this Port belongs to           |
| name               | string(200) | Not Null                            | All                     | Human-readable name (e.g., "Good Part Out")    |
| direction          | enum        | Not Null                            | All                     | One of: Input, Output                          |
| port_type          | enum        | Not Null                            | All                     | One of: Material, Parameter, Characteristic, Condition |
| kind_id            | UUID        | FK → Kind, nullable                 | Material only           | The Kind of item this port flows                |
| grade_id           | UUID        | FK → Grade, nullable                | Material only           | The Grade of item this port flows               |
| qty_rule_mode      | enum        | nullable                            | Material only           | One of: Exactly, ZeroOrN, Range, Unbounded     |
| qty_rule_n         | integer     | nullable                            | Material only           | The N value (for Exactly and ZeroOrN modes)    |
| qty_rule_min       | integer     | nullable                            | Material only           | Minimum (for Range and Unbounded modes)        |
| qty_rule_max       | integer     | nullable                            | Material only           | Maximum (for Range mode; null for Unbounded)   |
| data_type          | enum        | nullable                            | Parameter, Characteristic | One of: String, Integer, Decimal, Boolean, DateTime |
| units              | string(50)  | nullable                            | Parameter, Characteristic | Unit of measure (e.g., RPM, °C, mm)           |
| nominal_value      | string(200) | nullable                            | Parameter, Characteristic | Target value (stored as string)               |
| lower_tolerance    | string(100) | nullable                            | Parameter, Characteristic | Lower allowable deviation from nominal        |
| upper_tolerance    | string(100) | nullable                            | Parameter, Characteristic | Upper allowable deviation from nominal        |
| sort_order         | integer     | Not Null, Default: 0                | All                     | Display ordering among ports of same direction |
| created_at         | timestamp   | Not Null                            | All                     | Record creation time                           |
| updated_at         | timestamp   | Not Null                            | All                     | Last modification time                         |

**Constraints:**
- When port_type = Material: kind_id, grade_id, and qty_rule_mode are required; grade_id must reference a Grade that belongs to kind_id
- When port_type = Material: qty_rule_n required for Exactly/ZeroOrN; qty_rule_min required for Range/Unbounded; qty_rule_max required for Range; qty_rule_min ≤ qty_rule_max
- When port_type = Parameter or Characteristic: data_type is required
- When port_type = Condition: no additional fields required beyond name and direction
- kind_id, grade_id, and qty_rule_* must be null when port_type ≠ Material
- data_type, units, nominal_value, lower_tolerance, upper_tolerance must be null when port_type = Material or Condition

**Validation rules for pattern consistency** (applies to Material ports only):

| Pattern   | Expected Material Input Ports | Expected Material Output Ports |
|-----------|------------------------------|---------------------------------|
| Transform | Exactly 1                    | Exactly 1                       |
| Assembly  | 2 or more                    | Exactly 1                       |
| Division  | Exactly 1                    | 2 or more                       |
| General   | Any                          | Any                             |

### PromptDefinition

A design-time specification of a single data-collection field presented to the operator when executing a step. `PromptDefinition` definitions are owned by a `StepTemplate` and drive both the UI form (ordering, labels, validation) and completeness gating (a step cannot be completed until all `is_required` prompts have a corresponding `ExecutionData` record).

This is intentionally separate from `Port` (Parameter/Characteristic). A `Port` models process-knowledge relationships for quality tools. A `PromptDefinition` models operator UI and data capture. They coexist on the same `StepTemplate` and may represent the same physical measurement, but they serve different purposes and neither depends on the other.

| Attribute           | Type        | Constraints                         | Description                                                                 |
|---------------------|-------------|-------------------------------------|-----------------------------------------------------------------------------|
| id                  | UUID        | PK                                  | Unique identifier                                                           |
| step_template_id    | UUID        | FK → StepTemplate, Not Null         | The StepTemplate this prompt belongs to                                     |
| key                 | string(100) | Unique per step_template, Not Null  | Machine-readable field name (e.g., `solder_temp`, `operator_id`)           |
| label               | string(200) | Not Null                            | Question or field label shown to the operator                               |
| description         | text        |                                     | Additional guidance / help text                                             |
| data_type           | enum        | Not Null                            | See DataType enum below                                                     |
| collection_scope    | enum        | Not Null, Default: PerStep          | One of: PerStep, PerItem, PerBatch                                          |
| is_required         | boolean     | Not Null, Default: true             | Whether an answer must exist before the step can be completed               |
| units               | string(50)  |                                     | Unit of measure for numeric prompts (e.g., `°C`, `mm`, `kPa`)              |
| nominal_value       | string(200) |                                     | Expected/target value (stored as string; interpretation depends on data_type) |
| lower_limit         | string(200) |                                     | Minimum acceptable value (numeric prompts)                                  |
| upper_limit         | string(200) |                                     | Maximum acceptable value (numeric prompts)                                  |
| validation_pattern  | string(500) |                                     | Regex applied to String and Barcode answers to validate format              |
| sort_order          | integer     | Not Null, Default: 0                | Display order within the step's form                                        |
| created_at          | timestamp   | Not Null                            | Record creation time                                                        |
| updated_at          | timestamp   | Not Null                            | Last modification time                                                      |

**DataType enum values** (shared with `ExecutionData.data_type`):

| Value       | Stored as          | Use cases                                                          |
|-------------|--------------------|--------------------------------------------------------------------||
| String      | text               | Free-text notes, names, addresses, survey answers                  |
| Integer     | text (parsed)      | Counts, cycles, quantities                                         |
| Decimal     | text (parsed)      | Temperatures, pressures, dimensions                                |
| Boolean     | "true"/"false"     | Pass/Fail flags, yes/no questions                                  |
| DateTime    | ISO-8601 string    | Timestamps, dates, scheduled times                                 |
| Select      | option value       | Single-choice from a predefined list (e.g., failure reason)        |
| MultiSelect | comma-delimited    | Multiple choices from a list (e.g., checklist items completed)     |
| Barcode     | scanned string     | Part serial number, workpiece ID, asset tag (with optional regex validation) |
| Photo       | file reference     | Defect photo, completed assembly evidence, condition documentation |
| Signature   | encoded string     | Operator sign-off, approval capture                                |

**CollectionScope enum values:**

| Value    | Description                                                                           |
|----------|---------------------------------------------------------------------------------------|
| PerStep  | One answer per step execution — entered once regardless of how many items are in scope |
| PerItem  | One answer per item flowing through the step — form repeats for each serialized item   |
| PerBatch | One answer per batch — entered once per batch passing through the step                 |

**Constraints:**
- `key` must be unique within a `step_template_id`
- `lower_limit` and `upper_limit` only apply when `data_type` is Integer or Decimal
- `validation_pattern` only applies when `data_type` is String or Barcode
- `units` and `nominal_value` only apply when `data_type` is Integer or Decimal
- Prompts with `data_type` = Select or MultiSelect must have at least one associated `PromptOption`

### PromptOption

A single choice in the option list for a `Select` or `MultiSelect` prompt. The operator sees `label`; the stored value in `ExecutionData` is `value`.

| Attribute              | Type        | Constraints                         | Description                                         |
|------------------------|-------------|-------------------------------------|-----------------------------------------------------|
| id                     | UUID        | PK                                  | Unique identifier                                   |
| prompt_definition_id   | UUID        | FK → PromptDefinition, Not Null     | The prompt this option belongs to                   |
| label                  | string(200) | Not Null                            | Human-readable option text shown in the UI          |
| value                  | string(200) | Not Null                            | Stored value in ExecutionData (may differ from label) |
| sort_order             | integer     | Not Null, Default: 0                | Display order in the option list                    |
| is_active              | boolean     | Not Null, Default: true             | Whether this option is currently offered            |
| created_at             | timestamp   | Not Null                            | Record creation time                                |
| updated_at             | timestamp   | Not Null                            | Last modification time                              |

**Constraints:**
- `value` must be unique within a `prompt_definition_id`
- Only valid when the parent `PromptDefinition.data_type` is Select or MultiSelect

---

## Phase 3 Entities

### Process

A linear sequence of steps.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| code           | string(50)  | Unique, Not Null               | Short identifier (e.g., "WDG-MACH-01")        |
| name           | string(200) | Not Null                       | Human-readable name (e.g., "Widget Machining") |
| description    | text        |                                | Purpose and scope of this process              |
| version        | integer     | Not Null, Default: 1           | Version number for change tracking              |
| is_active      | boolean     | Not Null, Default: true        | Whether this process is available for use       |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

### ProcessStep

An instance of a StepTemplate placed at a specific position within a Process.

| Attribute          | Type        | Constraints                    | Description                                    |
|--------------------|-------------|--------------------------------|------------------------------------------------|
| id                 | UUID        | PK                             | Unique identifier                              |
| process_id         | UUID        | FK → Process, Not Null         | The Process this step belongs to                |
| step_template_id   | UUID        | FK → StepTemplate, Not Null    | The StepTemplate being used                    |
| sequence           | integer     | Not Null                       | Position in the process (1-based)              |
| name_override      | string(200) |                                | Optional override of the StepTemplate name     |
| description_override | text      |                                | Optional override of the StepTemplate description |
| created_at         | timestamp   | Not Null                       | Record creation time                           |
| updated_at         | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- Unique on (process_id, sequence) — no two steps share the same position
- sequence values must be contiguous starting from 1

### Flow

A connection from an Output Port of one ProcessStep to an Input Port of the next ProcessStep within a Process.

| Attribute              | Type        | Constraints                    | Description                                    |
|------------------------|-------------|--------------------------------|------------------------------------------------|
| id                     | UUID        | PK                             | Unique identifier                              |
| process_id             | UUID        | FK → Process, Not Null         | The Process this flow belongs to                |
| source_process_step_id | UUID        | FK → ProcessStep, Not Null     | The upstream ProcessStep                       |
| source_port_id         | UUID        | FK → Port, Not Null            | The output port on the source step             |
| target_process_step_id | UUID        | FK → ProcessStep, Not Null     | The downstream ProcessStep                    |
| target_port_id         | UUID        | FK → Port, Not Null            | The input port on the target step              |
| created_at             | timestamp   | Not Null                       | Record creation time                           |
| updated_at             | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- source_port_id must reference a Port with direction = Output that belongs to the StepTemplate of source_process_step_id
- target_port_id must reference a Port with direction = Input that belongs to the StepTemplate of target_process_step_id
- source_process_step_id and target_process_step_id must belong to the same Process
- source_process_step_id.sequence + 1 = target_process_step_id.sequence (adjacent steps only)
- **Type compatibility:** The source port's Kind + Grade must match the target port's Kind + Grade exactly
- **Quantity compatibility:** The source port's quantity rule must be compatible with the target port's quantity rule (e.g., a source producing "0 or 1" feeding a target expecting "exactly 1" is a warning)
- An output port may connect to at most one flow (an item leaves by one path)
- An input port may connect to at most one flow (an item arrives from one source)

---

## Cross-Phase Validation Rules

These rules span multiple entities and ensure the model is internally consistent.

### Rule 1: Grade belongs to Kind

Wherever a Kind + Grade pair is referenced (e.g., on a Port), the Grade must be a member of that Kind's grade set.

### Rule 2: Port configuration matches Step pattern

The number of input and output ports on a StepTemplate should be consistent with its declared pattern. This is a warning, not a hard error (the pattern field is informational).

### Rule 3: Flow type compatibility

Flows connect **Material ports only**. A Flow may not be created between Parameter, Characteristic, or Condition ports — those relationships are expressed in the PFMEA and C&E Matrix modules, not as runtime flows.

Every Flow connecting two Material ports must reference ports with identical Item Types (Kind + Grade). A type mismatch is a hard error.

### Rule 4: Complete flow coverage within a Process

Every Input Port on every ProcessStep (except the first step's input ports) should be the target of a Flow. Unconnected input ports on interior steps are warnings — they represent items that must come from outside the process.

Every Output Port on every ProcessStep (except the last step's output ports) should be the source of a Flow. Unconnected output ports on interior steps are warnings — they represent items that leave the process mid-stream (e.g., waste, rejected parts).

### Rule 5: Process entry and exit ports

The Input Ports of the first ProcessStep define what the Process requires to start. The Output Ports of the last ProcessStep define what the Process produces. These are the Process's external interface and will be used in Phase 4 (Workflow Composition) for routing.

---

## Scenario Walkthrough: Widget Inspection Process

This validates the model against a concrete example.

### Kinds and Grades (Phase 1)

```
Kind: Widget (WDG-100)
  Grades: Raw, Passed, Failed-Dimensional, Failed-Cosmetic, Reworked

Kind: Scrap (SCRAP-001)
  Grades: Standard
```

### Step Templates (Phase 2)

**Step: Dimensional Inspection (INSP-DIM-01)**
- Pattern: Division
- Input Port:  "Part In"     — Kind=Widget, Grade=Raw,                Qty=Exactly 1
- Output Port: "Good Part"   — Kind=Widget, Grade=Passed,             Qty=ZeroOrN(1)
- Output Port: "Failed Part" — Kind=Widget, Grade=Failed-Dimensional, Qty=ZeroOrN(1)

**Step: Deburr (DEBURR-01)**
- Pattern: Transform
- Input Port:  "Part In"  — Kind=Widget, Grade=Raw,  Qty=Exactly 1
- Output Port: "Part Out" — Kind=Widget, Grade=Raw,  Qty=Exactly 1

### Process: Widget Finishing (Phase 3)

```
Process: WDG-FINISH-01 "Widget Finishing"

  ProcessStep 1: DEBURR-01 (sequence=1)
  ProcessStep 2: INSP-DIM-01 (sequence=2)

  Flow: DEBURR-01."Part Out" (Widget/Raw) → INSP-DIM-01."Part In" (Widget/Raw)  ✓ Types match

  Process Entry:  DEBURR-01."Part In"       — requires Widget/Raw
  Process Exit:   INSP-DIM-01."Good Part"   — produces Widget/Passed
                  INSP-DIM-01."Failed Part" — produces Widget/Failed-Dimensional
```

---

## Forward Compatibility Notes

- **Versioning:** StepTemplates and Processes have version fields. Step Executions reference the ProcessStep (which references the StepTemplate) that was active at execution time. Full versioning strategy (immutable versions vs. mutable drafts) is deferred.

---

## Phase 4 Entities

### Enums

#### RoutingType

| Value       | Description                                    |
|-------------|------------------------------------------------|
| Always      | Unconditional — items always follow this link  |
| GradeBased  | Follow when item's grade matches a condition   |
| Manual      | Human operator selects the path                |

### Workflow

A directed graph of Processes connected by routing links. Workflows enable branching, rework loops, and multi-process flows that individual linear Processes cannot express.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| code           | string(50)  | Unique, Not Null               | Short identifier (e.g., "WF-WDG-01")          |
| name           | string(200) | Not Null                       | Human-readable name                            |
| description    | text        |                                | Purpose / scope of this workflow               |
| version        | integer     | Not Null, Default: 1           | Version number for change tracking             |
| is_active      | boolean     | Not Null, Default: true        | Whether this workflow is available for use      |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

### WorkflowProcess

A placement of a Process within a Workflow (a node in the graph).

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| workflow_id    | UUID        | FK → Workflow, Not Null        | The Workflow this placement belongs to         |
| process_id     | UUID        | FK → Process, Not Null         | The Process being placed                       |
| assignee_id    | UUID        | FK → OrgUnit, nullable         | Department/role responsible for executing this node |
| is_entry_point | boolean     | Not Null, Default: false       | Is this a starting point in the workflow?      |
| sort_order     | integer     | Not Null, Default: 0           | Display ordering                               |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- Unique on (workflow_id, process_id) — a process appears at most once per workflow
- assignee_id, when set, must reference an OrgUnit with is_active = true

### WorkflowLink

A directed edge from one WorkflowProcess to another, with routing rules that determine when items follow this path.

| Attribute                    | Type        | Constraints                    | Description                                    |
|------------------------------|-------------|--------------------------------|------------------------------------------------|
| id                           | UUID        | PK                             | Unique identifier                              |
| workflow_id                  | UUID        | FK → Workflow, Not Null        | The Workflow this link belongs to              |
| source_workflow_process_id   | UUID        | FK → WorkflowProcess, Not Null | Source node (items leave from)                 |
| target_workflow_process_id   | UUID        | FK → WorkflowProcess, Not Null | Target node (items arrive at)                  |
| routing_type                 | enum        | Not Null, Default: Always      | How routing is determined                      |
| name                         | string(200) |                                | Optional label for the link                    |
| sort_order                   | integer     | Not Null, Default: 0           | Display ordering among links from same source  |
| created_at                   | timestamp   | Not Null                       | Record creation time                           |
| updated_at                   | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- Source and target must belong to the same Workflow
- Source and target must be different WorkflowProcesses (no self-loops)
- Unique on (source_workflow_process_id, target_workflow_process_id) — at most one link between any directed pair

### WorkflowLinkCondition

A grade-based routing condition on a WorkflowLink. When a link has routing_type = GradeBased, items follow it only when their current grade matches one of the link's conditions.

| Attribute          | Type        | Constraints                    | Description                                    |
|--------------------|-------------|--------------------------------|------------------------------------------------|
| id                 | UUID        | PK                             | Unique identifier                              |
| workflow_link_id   | UUID        | FK → WorkflowLink, Not Null   | The link this condition belongs to             |
| grade_id           | UUID        | FK → Grade, Not Null           | Route when item has this grade                 |
| created_at         | timestamp   | Not Null                       | Record creation time                           |
| updated_at         | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- Only valid when the link's routing_type = GradeBased
- Unique on (workflow_link_id, grade_id) — no duplicate conditions per link

### OrgUnit

An assignable entity representing any responsible party: a department, work area, role, or individual. Used to declare responsibility for a `WorkflowProcess` node at design time, and to route notifications when execution advances to that node.

| Attribute      | Type        | Constraints                    | Description                                    |
|----------------|-------------|--------------------------------|------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                              |
| code           | string(50)  | Unique, Not Null               | Short identifier (e.g., "QC", "ASSY", "ENG")  |
| name           | string(200) | Not Null                       | Human-readable name (e.g., "Quality Control")  |
| type           | enum        | Not Null                       | One of: Department, WorkArea, Role, Person     |
| parent_id      | UUID        | FK → OrgUnit, nullable         | Parent in the hierarchy (self-referential)     |
| is_active      | boolean     | Not Null, Default: true        | Whether available for assignment               |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

**OrgUnitType enum values:**

| Value      | Description                                                         |
|------------|---------------------------------------------------------------------|
| Department | A functional department (e.g. Engineering, Quality, Assembly)       |
| WorkArea   | A physical area or cell (e.g. SMT Line, CMM Room, Rework Bench)     |
| Role       | A job function regardless of person (e.g. Process Engineer, Approver) |
| Person     | A specific named individual (least preferred — use Role when possible) |

### WorkflowJob

A top-level execution record for a complete workflow run. Analogous to `Job` for a single process. When a workflow is started, a `WorkflowJob` is created, and `Job` records are created for each active node as the workflow graph is traversed.

| Attribute      | Type        | Constraints                    | Description                                                    |
|----------------|-------------|--------------------------------|----------------------------------------------------------------|
| id             | UUID        | PK                             | Unique identifier                                              |
| workflow_id    | UUID        | FK → Workflow, Not Null        | The Workflow being executed                                     || schedule_id    | UUID          | FK → WorkflowSchedule, nullable | The schedule that created this run; null for ad-hoc runs        || subject        | string(500) |                                | What this run is about (e.g. "Batch #4421", "New hire: J. Smith") |
| status         | enum        | Not Null, Default: Running     | Current lifecycle state                                        |
| started_at     | timestamp   |                                | When the first process job was created                         |
| completed_at   | timestamp   |                                | When the final node completed                                  |
| created_at     | timestamp   | Not Null                       | Record creation time                                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                                         |

**WorkflowJobStatus enum values:**

| Value     | Description                                           |
|-----------|-------------------------------------------------------|
| Running   | One or more process jobs are in progress              |
| Completed | All terminal nodes have reached Completed status      |
| Cancelled | The workflow run was cancelled before completion      |

### WorkflowSchedule

Defines a recurrence rule for automatically creating `WorkflowJob` instances at a fixed cadence. The scheduler background service polls active schedules and fires them when `next_run_at` falls due. Once a `WorkflowJob` is created by the scheduler, it proceeds through the standard sequencing pipeline.

| Attribute           | Type          | Constraints                      | Description                                                                 |
|---------------------|---------------|----------------------------------|-----------------------------------------------------------------------------|
| id                  | UUID          | PK                               | Unique identifier                                                           |
| workflow_id         | UUID          | FK → Workflow, Not Null          | The Workflow to execute on this schedule                                    |
| name                | string(200)   | Not Null                         | Human label (e.g. "Monthly PCB Final Inspection")                           |
| recurrence_type     | enum          | Not Null                         | One of: Daily, Weekly, Monthly, Quarterly, Annually                         |
| recurrence_interval | integer       | Not Null, Default: 1             | Every N recurrence units (e.g. 2 = every 2 weeks)                          |
| day_of_week         | integer       | nullable, 0–6                    | Which weekday to fire; used when recurrence_type = Weekly                  |
| day_of_month        | integer       | nullable, 1–31                   | Which day of month to fire; used for Monthly/Quarterly/Annually             |
| start_date          | date          | Not Null                         | Earliest date this schedule may fire                                        |
| end_date            | date          | nullable                         | Date after which this schedule will not fire; null = no expiry              |
| subject_template    | string(500)   |                                  | Template for WorkflowJob.Subject; supports tokens like `{Month}`, `{Year}` |
| is_active           | boolean       | Not Null, Default: true          | Whether the scheduler should process this record                            |
| next_run_at         | timestamptz   | nullable                         | Computed datetime of the next scheduled fire                                |
| last_run_at         | timestamptz   | nullable                         | When the scheduler last fired this schedule                                 |
| created_at          | timestamp     | Not Null                         | Record creation time                                                        |
| updated_at          | timestamp     | Not Null                         | Last modification time                                                      |

**RecurrenceType enum values:**

| Value     | Description                                                              |
|-----------|--------------------------------------------------------------------------|
| Daily     | Fires every N days                                                       |
| Weekly    | Fires every N weeks on the specified day_of_week                        |
| Monthly   | Fires every N months on the specified day_of_month                      |
| Quarterly | Fires every 3 months on the specified day_of_month                      |
| Annually  | Fires once per year on the specified day_of_month (and month_of_year)   |

---

## Phase 4 Cross-Entity Validation Rules

### Rule 12: Workflow entry point

A Workflow must have at least one WorkflowProcess with is_entry_point = true.

### Rule 13: Workflow link integrity

WorkflowLink source and target must both belong to the same Workflow and must reference different WorkflowProcesses (no self-loops).

### Rule 14: GradeBased links require conditions

A WorkflowLink with routing_type = GradeBased must have at least one WorkflowLinkCondition.

### Rule 15: Process interface compatibility

For a WorkflowLink, the source process's exit ports (last step's output ports) should produce items whose Kind matches the Kind accepted by the target process's entry ports (first step's input ports). A mismatch is a warning, not a hard error (operators may supply items externally).

---

## Scenario Walkthrough: Widget Manufacturing Workflow (Phase 4)

Building on the Phase 3 Widget Finishing Process:

### Additional Processes (Phase 3)

```
Process: Packaging (PKG-01)
  Step 1: Package (PKG-STEP-01) — Transform
    Input:  "Part In"  — Widget/Passed, Exactly 1
    Output: "Part Out" — Widget/Passed, Exactly 1

Process: Rework (REWORK-01)
  Step 1: Rework (REWORK-STEP-01) — Transform
    Input:  "Part In"  — Widget/Failed-Dimensional, Exactly 1
    Output: "Part Out" — Widget/Raw, Exactly 1
```

### Workflow Definition (Phase 4)

```
Workflow: Widget Manufacturing (WF-WDG-01)

  WorkflowProcess 1: Widget Finishing (WDG-FINISH-01) [entry_point=true]
  WorkflowProcess 2: Packaging (PKG-01)
  WorkflowProcess 3: Rework (REWORK-01)

  Link 1: Widget Finishing → Packaging
    routing_type: GradeBased
    conditions: [Grade=Passed]

  Link 2: Widget Finishing → Rework
    routing_type: GradeBased
    conditions: [Grade=Failed-Dimensional]

  Link 3: Rework → Widget Finishing
    routing_type: Always
    (rework loop — item re-enters the finishing process)
```

### Item Flow Through Workflow

```
1. Widget WDG-001 enters at Widget Finishing (entry point)
2. After Deburr + Inspection, item exits with Grade=Passed
3. Link 1 matches (GradeBased: Passed) → route to Packaging
4. Package process runs → item exits as Widget/Passed

OR:
2. After Inspection, item exits with Grade=Failed-Dimensional
3. Link 2 matches (GradeBased: Failed-Dimensional) → route to Rework
4. Rework process runs → item exits as Widget/Raw
5. Link 3 matches (Always) → route back to Widget Finishing
6. Cycle repeats until item passes inspection
```

### Response DTO Notes

API response DTOs for **Item** and **Batch** include denormalized display fields that are not stored on the entity itself:

| DTO | Denormalized fields | Source |
|---|---|---|
| `ItemResponseDto` | `JobName` (string), `BatchCode` (string?) | Loaded via `.Include(Job)` / `.Include(Batch)` |
| `BatchResponseDto` | `JobName` (string) | Loaded via `.Include(Job)` |

These fields exist to avoid extra round-trips from list views. They are not persisted columns.

---

## Phase 5 Entities

### Enums

#### JobStatus

| Value       | Description                                    |
|-------------|------------------------------------------------|
| Created     | Job has been created but work has not started   |
| InProgress  | Work is actively being performed                |
| Completed   | All steps finished successfully                 |
| Cancelled   | Job was cancelled before completion             |
| OnHold      | Job is temporarily paused                       |

#### ItemStatus

| Value       | Description                                    |
|-------------|------------------------------------------------|
| Available   | Ready to be used in a step                      |
| InProcess   | Currently being worked on in a step             |
| Consumed    | Used up (e.g., assembled into another item)     |
| Completed   | Finished processing, exited the process          |
| Scrapped    | Removed from processing                         |

#### BatchStatus

| Value       | Description                                    |
|-------------|------------------------------------------------|
| Open        | Items can still be added to the batch           |
| Closed      | Sealed — no more items can be added             |
| InProcess   | Currently being worked on in a step             |
| Completed   | Finished processing                             |

#### StepExecutionStatus

| Value       | Description                                    |
|-------------|------------------------------------------------|
| Pending     | Waiting for previous step to complete           |
| InProgress  | Currently being executed                        |
| Completed   | Successfully finished                           |
| Skipped     | Bypassed (e.g., not applicable)                 |
| Failed      | Execution failed                                |

#### DataValueType

| Value       | Description                                    |
|-------------|------------------------------------------------|
| String      | Free-form text                                  |
| Integer     | Whole number                                    |
| Decimal     | Floating-point number                           |
| Boolean     | True/false                                      |
| DateTime    | Date and/or time value                          |

### Job

The overarching work order that drives items through a process. A Job ties together the entire execution lifecycle. When a Job is part of a workflow run, `workflow_job_id` and `workflow_process_id` link it back to its parent `WorkflowJob` and the specific graph node it represents.

| Attribute              | Type          | Constraints                         | Description                                              |
|------------------------|---------------|-------------------------------------|----------------------------------------------------------|
| id                     | UUID          | PK                                  | Unique identifier                                        |
| code                   | string(50)    | Unique, Not Null                    | Short identifier (e.g., "JOB-2026-001")                  |
| name                   | string(200)   | Not Null                            | Human-readable name                                      |
| description            | text          |                                     | Purpose/scope of this job                                |
| process_id             | UUID          | FK → Process, Not Null              | The Process being executed                               |
| workflow_job_id        | UUID          | FK → WorkflowJob, nullable          | Parent workflow run (null for standalone jobs)           |
| workflow_process_id    | UUID          | FK → WorkflowProcess, nullable      | Graph node this job represents in the workflow           |
| status                 | enum          | Not Null, Default: Created          | Current lifecycle state                                  |
| priority               | integer       | Not Null, Default: 0                | Relative priority (higher = more urgent)                 |
| started_at             | timestamp     |                                     | When work actually began                                 |
| completed_at           | timestamp     |                                     | When job finished (completed or cancelled)               |
| created_at             | timestamp     | Not Null                            | Record creation time                                     |
| updated_at             | timestamp     | Not Null                            | Last modification time                                   |

**Behavior:**
- When a Job is created, a StepExecution record is automatically created for each ProcessStep in the Process, all with status = Pending.
- A Job can only be started if status = Created or OnHold.
- A Job can only be completed if all StepExecutions are Completed or Skipped.

### Item

A specific instance of a Kind that flows through the process. Serialized items have unique serial numbers; untracked items are counted by quantity on PortTransactions.

| Attribute      | Type          | Constraints                    | Description                                    |
|----------------|---------------|--------------------------------|------------------------------------------------|
| id             | UUID          | PK                             | Unique identifier                              |
| serial_number  | string(100)   |                                | Unique ID for serialized items                 |
| kind_id        | UUID          | FK → Kind, Not Null            | What this item is                              |
| grade_id       | UUID          | FK → Grade, Not Null           | Current condition/qualification                |
| job_id         | UUID          | FK → Job, Not Null             | The Job this item belongs to                   |
| batch_id       | UUID          | FK → Batch                     | Optional batch membership                      |
| status         | enum          | Not Null, Default: Available   | Current lifecycle state                        |
| created_at     | timestamp     | Not Null                       | Record creation time                           |
| updated_at     | timestamp     | Not Null                       | Last modification time                         |

**Constraints:**
- serial_number is required when the item's Kind has is_serialized = true
- serial_number is unique within a Kind (unique on kind_id + serial_number when not null)
- grade_id must reference a Grade belonging to kind_id
- batch_id can only be set if the Kind has is_batchable = true
- When batch_id is set, the Batch's kind_id must match the Item's kind_id

### Batch

A tracked, homogeneous group of items. The batch carries a Grade that all member items inherit.

| Attribute      | Type          | Constraints                    | Description                                    |
|----------------|---------------|--------------------------------|------------------------------------------------|
| id             | UUID          | PK                             | Unique identifier                              |
| code           | string(50)    | Unique, Not Null               | Batch identifier (e.g., "LOT-2026-042")        |
| kind_id        | UUID          | FK → Kind, Not Null            | All items in this batch are this Kind           |
| grade_id       | UUID          | FK → Grade, Not Null           | Batch-level grade (items inherit)              |
| job_id         | UUID          | FK → Job, Not Null             | The Job this batch belongs to                  |
| quantity       | integer       | Not Null, Default: 0           | Count of items (for non-serialized Kinds)      |
| status         | enum          | Not Null, Default: Open        | Current lifecycle state                        |
| created_at     | timestamp     | Not Null                       | Record creation time                           |
| updated_at     | timestamp     | Not Null                       | Last modification time                         |

**Constraints:**
- grade_id must reference a Grade belonging to kind_id
- kind_id must reference a Kind with is_batchable = true
- When Batch grade changes, all member Items' grades are updated to match

### StepExecution

A record of a Step being performed (or pending) within a Job. Auto-created when the Job is created.

| Attribute          | Type          | Constraints                    | Description                                    |
|--------------------|---------------|--------------------------------|------------------------------------------------|
| id                 | UUID          | PK                             | Unique identifier                              |
| job_id             | UUID          | FK → Job, Not Null             | The Job this execution belongs to              |
| process_step_id    | UUID          | FK → ProcessStep, Not Null     | Which step in the process                      |
| sequence           | integer       | Not Null                       | Mirrors ProcessStep.Sequence for ordering      |
| status             | enum          | Not Null, Default: Pending     | Current execution state                        |
| started_at         | timestamp     |                                | When execution began                           |
| completed_at       | timestamp     |                                | When execution finished                        |
| notes              | text          |                                | Operator notes / observations                  |
| created_at         | timestamp     | Not Null                       | Record creation time                           |
| updated_at         | timestamp     | Not Null                       | Last modification time                         |

**Constraints:**
- Unique on (job_id, process_step_id)
- A StepExecution can only be started if the previous step (by sequence) is Completed or Skipped
- A StepExecution can only be completed if all required port transactions are recorded

### PortTransaction

Records an item or batch flowing through a specific port during a step execution. This is the core traceability record.

| Attribute              | Type          | Constraints                    | Description                                    |
|------------------------|---------------|--------------------------------|------------------------------------------------|
| id                     | UUID          | PK                             | Unique identifier                              |
| step_execution_id      | UUID          | FK → StepExecution, Not Null   | The step execution this occurred during        |
| port_id                | UUID          | FK → Port, Not Null            | Which port the item/batch flowed through       |
| item_id                | UUID          | FK → Item                      | The specific item (for serialized)             |
| batch_id               | UUID          | FK → Batch                     | The batch (for batched items)                  |
| quantity               | integer       | Not Null, Default: 1           | Count (for untracked items or batch quantity)  |
| created_at             | timestamp     | Not Null                       | Record creation time                           |
| updated_at             | timestamp     | Not Null                       | Last modification time                         |

**Constraints:**
- At least one of item_id, batch_id must be set, OR quantity > 0 for untracked items
- The port must belong to the StepTemplate referenced by the StepExecution's ProcessStep
- When recording an output port transaction, the item's grade is updated to match the port's declared grade
- item_id and batch_id are not mutually exclusive — an item may also reference the batch it belongs to

### ExecutionData

A single collected answer captured during execution. Each record corresponds to one prompt answer (or ad-hoc data entry) and is associated at one of three levels: Step Execution, Batch, or Item.

When created by the structured form engine, `prompt_definition_id` is set and `key` is copied from `PromptDefinition.key` at write time (denormalised for queryability). Ad-hoc or legacy records may leave `prompt_definition_id` null and supply `key` directly.

| Attribute              | Type          | Constraints                         | Description                                                          |
|------------------------|---------------|-------------------------------------|----------------------------------------------------------------------|
| id                     | UUID          | PK                                  | Unique identifier                                                    |
| prompt_definition_id   | UUID          | FK → PromptDefinition, nullable     | The prompt this answer responds to; null for ad-hoc data            |
| key                    | string(200)   | Not Null                            | Field name — copied from PromptDefinition.key, or free-text if ad-hoc |
| value                  | text          | Not Null                            | Stored answer; interpretation depends on data_type                  |
| data_type              | enum          | Not Null, Default: String           | See DataType enum on PromptDefinition                               |
| unit_of_measure        | string(50)    |                                     | Unit (e.g., "mm", "°C", "psi")                                       |
| step_execution_id      | UUID          | FK → StepExecution                  | Association level 1: step-wide data                                 |
| batch_id               | UUID          | FK → Batch                          | Association level 2: batch-level data                               |
| item_id                | UUID          | FK → Item                           | Association level 3: item-level data                                |
| created_at             | timestamp     | Not Null                            | Record creation time                                                |
| updated_at             | timestamp     | Not Null                            | Last modification time                                              |

**Constraints:**
- Exactly one of step_execution_id, batch_id, item_id must be non-null
- Multiple data points can exist at the same level (e.g., many measurements per item)
- When `prompt_definition_id` is set, `key` must match `PromptDefinition.key` for that record
- When `prompt_definition_id` is set, `data_type` must match `PromptDefinition.data_type`
- A step execution that has `is_required = true` prompts cannot be marked Completed until an `ExecutionData` record exists for each such prompt at the appropriate `collection_scope` level

---

## Phase 5 Entity-Relationship Diagram (Text)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE 5: EXECUTION / RUNTIME                                          │
│                                                                         │
│  ┌──────────┐ *───1 ┌──────────┐                                       │
│  │   Job    │───────│ Process  │  (from Phase 3)                       │
│  └──────────┘       └──────────┘                                       │
│       │ 1                                                               │
│       │                                                                 │
│       ├───* ┌───────────────┐ *───1 ┌─────────────┐                    │
│       │     │ StepExecution │───────│ ProcessStep │  (from Phase 3)    │
│       │     └───────────────┘       └─────────────┘                    │
│       │           │ 1                                                   │
│       │           │                                                     │
│       │           └───* ┌─────────────────┐                            │
│       │                 │ PortTransaction  │                            │
│       │                 └─────────────────┘                            │
│       │                    │ *         │ *                               │
│       │                    │           │                                 │
│       ├───* ┌──────────┐ ◄┘           └► ┌──────────┐ *───┐           │
│       │     │   Item   │                  │  Batch   │     │           │
│       │     └──────────┘                  └──────────┘     │           │
│       │        │ *───1 Kind (Phase 1)        │ *───1 Kind  │           │
│       │        │ *───1 Grade (Phase 1)       │ *───1 Grade │           │
│       │        │                             │             │           │
│       │        │                             │ 1───* Item  │           │
│       │        │                             │             │           │
│       │        ▼                             ▼             │           │
│       │   ┌───────────────┐           ┌───────────────┐   │           │
│       │   │ ExecutionData │           │ ExecutionData │   │           │
│       │   │ (item-level)  │           │ (batch-level) │   │           │
│       │   └───────────────┘           └───────────────┘   │           │
│       │                                                    │           │
│       └───────────────┐                                    │           │
│                       ▼                                    │           │
│               ┌───────────────┐                            │           │
│               │ ExecutionData │                            │           │
│               │ (step-level)  │◄───────────────────────────┘           │
│               └───────────────┘                                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 5 Cross-Entity Validation Rules

### Rule 6: Item Grade belongs to Item Kind

An Item's grade_id must reference a Grade that belongs to the Item's kind_id.

### Rule 7: Batch Kind must be Batchable

A Batch's kind_id must reference a Kind with is_batchable = true.

### Rule 8: Batch members match Batch Kind

All Items in a Batch must have the same kind_id as the Batch.

### Rule 9: Port Transaction type compatibility

When recording a PortTransaction, the item's Kind must match the Port's declared Kind. For output port transactions, the item's Grade is updated to match the Port's declared Grade.

### Rule 10: Step Execution ordering

A StepExecution can only transition to InProgress if the previous step (by sequence) is Completed or Skipped. The first step (sequence=1) can always be started.

### Rule 11: Job completion requires step completion

A Job can only be marked Completed if all of its StepExecutions are in a terminal state (Completed or Skipped).

---

## Scenario Walkthrough: Widget Finishing Job (Phase 5)

Building on the Phase 3 scenario:

### Setup (Phases 1–3)

```
Kind: Widget (WDG-100), IsSerialized=true, IsBatchable=false
  Grades: Raw, Passed, Failed-Dimensional

Step: Deburr (DEBURR-01) — Transform
  Input:  "Part In"  — Widget/Raw, Exactly 1
  Output: "Part Out" — Widget/Raw, Exactly 1

Step: Inspection (INSP-DIM-01) — Division
  Input:  "Part In"     — Widget/Raw, Exactly 1
  Output: "Good Part"   — Widget/Passed, ZeroOrN 1
  Output: "Failed Part" — Widget/Failed-Dimensional, ZeroOrN 1

Process: Widget Finishing (WDG-FINISH-01)
  Step 1: DEBURR-01
  Step 2: INSP-DIM-01
  Flow: DEBURR-01."Part Out" → INSP-DIM-01."Part In"
```

### Execution (Phase 5)

```
1. Create Job: JOB-2026-001 "Widget Finishing Run #1"
   → Process: WDG-FINISH-01
   → Auto-creates: StepExecution 1 (Pending), StepExecution 2 (Pending)

2. Create Item: SN=WDG-001, Kind=Widget, Grade=Raw, Status=Available

3. Start Job → Status: InProgress

4. Start StepExecution 1 (Deburr) → Status: InProgress
   → Record PortTransaction: WDG-001 through "Part In" (input)
   → Record PortTransaction: WDG-001 through "Part Out" (output)
     → Item grade remains Raw (port declares Widget/Raw)
   → Record ExecutionData: {key: "Operator", value: "J. Smith", type: String}
   → Complete StepExecution 1 → Status: Completed

5. Start StepExecution 2 (Inspection) → Status: InProgress
   → Record PortTransaction: WDG-001 through "Part In" (input)
   → Record PortTransaction: WDG-001 through "Good Part" (output)
     → Item grade changes: Raw → Passed (port declares Widget/Passed)
   → Record ExecutionData: {key: "Length", value: "50.02", type: Decimal, unit: "mm"}  (item-level)
   → Complete StepExecution 2 → Status: Completed

6. Complete Job → Status: Completed
   → Item WDG-001: Kind=Widget, Grade=Passed, Status=Completed
```
