# Process Manager — Data Model (Phases 1–5)

## Version History

| Version | Date       | Notes                          |
|---------|------------|--------------------------------|
| 0.1     | 2026-02-16 | Initial draft — Phases 1–3     |
| 0.2     | 2026-02-16 | Added Phase 5 — Execution      |
| 0.3     | 2026-02-16 | Added Phase 4 — Workflows      |
| 0.4     | 2026-02-22 | Corrections: RoutingType values (Always/GradeBased/Manual, not Sequential); noted JobName/BatchCode denormalized fields on Item/Batch response DTOs for display performance |

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

### Port

A named connection point on a StepTemplate.

| Attribute          | Type        | Constraints                    | Description                                    |
|--------------------|-------------|--------------------------------|------------------------------------------------|
| id                 | UUID        | PK                             | Unique identifier                              |
| step_template_id   | UUID        | FK → StepTemplate, Not Null    | The StepTemplate this Port belongs to           |
| name               | string(200) | Not Null                       | Human-readable name (e.g., "Good Part Out")    |
| direction          | enum        | Not Null                       | One of: Input, Output                          |
| kind_id            | UUID        | FK → Kind, Not Null            | The Kind of item this port flows                |
| grade_id           | UUID        | FK → Grade, Not Null           | The Grade of item this port flows               |
| qty_rule_mode      | enum        | Not Null                       | One of: Exactly, ZeroOrN, Range, Unbounded     |
| qty_rule_n         | integer     |                                | The N value (for Exactly and ZeroOrN modes)    |
| qty_rule_min       | integer     |                                | Minimum (for Range and Unbounded modes)        |
| qty_rule_max       | integer     |                                | Maximum (for Range mode; null for Unbounded)   |
| sort_order         | integer     | Not Null, Default: 0           | Display ordering among ports of same direction |
| created_at         | timestamp   | Not Null                       | Record creation time                           |
| updated_at         | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- grade_id must reference a Grade that belongs to the Kind referenced by kind_id
- qty_rule_n is required when qty_rule_mode is Exactly or ZeroOrN
- qty_rule_min is required when qty_rule_mode is Range or Unbounded
- qty_rule_max is required when qty_rule_mode is Range
- qty_rule_min ≤ qty_rule_max (when both present)

**Validation rules for pattern consistency:**

| Pattern   | Expected Input Ports | Expected Output Ports |
|-----------|---------------------|-----------------------|
| Transform | Exactly 1           | Exactly 1             |
| Assembly  | 2 or more           | Exactly 1             |
| Division  | Exactly 1           | 2 or more             |
| General   | Any                 | Any                   |

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

Every Flow must connect ports with identical Item Types (Kind + Grade). A mismatch is a hard error.

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
| is_entry_point | boolean     | Not Null, Default: false       | Is this a starting point in the workflow?      |
| sort_order     | integer     | Not Null, Default: 0           | Display ordering                               |
| created_at     | timestamp   | Not Null                       | Record creation time                           |
| updated_at     | timestamp   | Not Null                       | Last modification time                         |

**Constraints:**
- Unique on (workflow_id, process_id) — a process appears at most once per workflow

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

The overarching work order that drives items through a process. A Job ties together the entire execution lifecycle.

| Attribute      | Type          | Constraints                    | Description                                    |
|----------------|---------------|--------------------------------|------------------------------------------------|
| id             | UUID          | PK                             | Unique identifier                              |
| code           | string(50)    | Unique, Not Null               | Short identifier (e.g., "JOB-2026-001")        |
| name           | string(200)   | Not Null                       | Human-readable name                            |
| description    | text          |                                | Purpose/scope of this job                      |
| process_id     | UUID          | FK → Process, Not Null         | The Process being executed                     |
| status         | enum          | Not Null, Default: Created     | Current lifecycle state                        |
| priority       | integer       | Not Null, Default: 0           | Relative priority (higher = more urgent)       |
| started_at     | timestamp     |                                | When work actually began                       |
| completed_at   | timestamp     |                                | When job finished (completed or cancelled)     |
| created_at     | timestamp     | Not Null                       | Record creation time                           |
| updated_at     | timestamp     | Not Null                       | Last modification time                         |

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

Key-value data captured during execution, associated at one of three levels: Step Execution, Batch, or Item.

| Attribute              | Type          | Constraints                    | Description                                    |
|------------------------|---------------|--------------------------------|------------------------------------------------|
| id                     | UUID          | PK                             | Unique identifier                              |
| key                    | string(200)   | Not Null                       | Data field name (e.g., "Temperature")          |
| value                  | string(1000)  | Not Null                       | Data value stored as string                    |
| data_type              | enum          | Not Null, Default: String      | How to interpret the value                     |
| unit_of_measure        | string(50)    |                                | Unit (e.g., "mm", "°C", "psi")                |
| step_execution_id      | UUID          | FK → StepExecution             | Association level 1: step-wide data            |
| batch_id               | UUID          | FK → Batch                     | Association level 2: batch-level data          |
| item_id                | UUID          | FK → Item                      | Association level 3: item-level data           |
| created_at             | timestamp     | Not Null                       | Record creation time                           |
| updated_at             | timestamp     | Not Null                       | Last modification time                         |

**Constraints:**
- Exactly one of step_execution_id, batch_id, item_id must be non-null
- Multiple data points can exist at the same level (e.g., many measurements per item)

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
