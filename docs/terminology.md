# Process Manager — Terminology & Definitions

## Version History

| Version | Date       | Notes                          |
|---------|------------|--------------------------------|
| 0.1     | 2026-02-16 | Initial draft                  |

---

## 1. Structural Concepts (Design-Time)

### 1.1 Workflow

A directed graph of **Processes** connected by **Routing Decisions**. A Workflow describes the full end-to-end path that work can take, including branches, loops (via rework), and parallel paths. Workflows are the top-level organizing structure.

### 1.2 Process

A strictly linear sequence of **Steps**. A Process has exactly one entry point (the first Step) and one exit point (the last Step). There is no branching within a Process. Branching occurs at the Workflow level, where completed items leaving one Process are routed into the next.

### 1.3 Step

A discrete unit of work with explicitly defined **Input Ports** and **Output Ports**. A Step is sized for human comprehension: it should be describable with a couple of sentences and pictures. A Step is the smallest unit of work that the system tracks.

### 1.4 Port

A named connection point on a Step through which **Items** flow. Each Port declares:

- **Direction**: Input or Output
- **Item Type** (exactly one Kind + Grade combination)
- **Quantity Rule**: how many items are expected to flow

A Port accepts or produces exactly one **Item Type**. This constraint enables the system to validate that items are flowing to correct destinations.

#### 1.4.1 Input Port

A Port through which items enter a Step.

#### 1.4.2 Output Port

A Port through which items exit a Step.

### 1.5 Quantity Rule

A constraint declared on a Port that specifies how many items are expected to flow. There are four quantity rule modes:

| Mode             | Notation  | Meaning                              | Example Use Case                  |
|------------------|-----------|--------------------------------------|-----------------------------------|
| Exactly N        | `=N`      | Must be exactly N                    | Assembly: exactly 2 blocks in     |
| Zero or N        | `0\|N`    | Conditional — may or may not flow    | Inspection: 0 or 1 through "fail" |
| Range            | `min–max` | Variable within bounds               | Cutting: 8–12 parts out           |
| Unbounded        | `min+`    | At least min, no upper limit         | Shredding: 1+ fragments           |

### 1.6 Flow

A connection that carries items from an Output Port to an Input Port. Flows exist within a Process (step-to-step) and within a Workflow (process-to-process, governed by Routing Decisions).

### 1.7 Routing Decision

A rule or human decision at the Workflow level that determines which Process an item enters after exiting another Process. Routing Decisions are the mechanism by which branching is expressed.

---

## 2. Type System

### 2.1 Kind

What something physically or logically **is**. Examples: Widget, Wafer, Invoice, Steel Sheet. A Kind defines the fundamental identity of an item independent of its condition or qualification.

Each Kind declares:

- A **code** (the short identifier, e.g., "WDG-100")
- A **name** (human-readable, e.g., "Widget")
- A **description**
- A set of valid **Grades**
- **Tracking flags**: Serialized (yes/no) and Batchable (yes/no)

### 2.2 Grade

The condition, qualification, or disposition that an item carries. Grades are defined **per Kind** — each Kind declares its own set of valid Grades.

Examples for Kind "Widget": Raw, Passed, Failed-Dimensional, Failed-Cosmetic, Reworked, Scrapped.

Grades enable the system to structurally prevent invalid flows. For instance, a Rework Step can require Grade=Failed-Dimensional on its Input Port, making it impossible for a Passed part to enter rework.

Users who do not need Grade distinctions define a single default Grade (e.g., "Standard") and never interact with the concept.

### 2.3 Item Type

The effective type of an item, defined as the combination of **Kind + Grade**. For example, "Widget/Passed" and "Widget/Failed-Dimensional" are two distinct Item Types. Ports declare exactly one Item Type.

---

## 3. Runtime Concepts (Execution-Time)

### 3.1 Item

A specific instance (if serialized) or counted quantity (if untracked) of an **Item Type** that flows through Ports, Steps, and Processes.

An Item always belongs to exactly one **Kind** and carries exactly one **Grade** at any point in time. An Item's Grade may change as it flows through Steps (e.g., Raw → Passed after inspection).

### 3.2 Item ID

A unique identifier for a serialized Item. Only present when the Item's Kind has the Serialized flag set. Item IDs may be system-generated or user-provided.

### 3.3 Batch

A tracked, homogeneous group of Items. A Batch:

- Has its own **Batch ID**
- Contains Items of exactly **one Kind**
- Carries a **Grade** — all Items in the Batch inherit this Grade
- If the Kind is Serialized, the Batch records the individual Item IDs of its members
- If the Kind is not Serialized, the Batch records a **quantity**

Batches are only available for Kinds that have the **Batchable** flag set.

**Batches are homogeneous.** A Batch cannot contain Items of different Kinds. When multiple Kinds must be processed together (e.g., a basket of mixed engine parts in a degreasing bath), this is handled by the Step having multiple Input Ports — one per Kind — and data recorded at the Step Execution level applies to all items across all ports.

#### 3.3.1 Batch Grade Inheritance

The Batch carries the Grade. All Items in the Batch inherit the Batch's Grade. If an individual Item needs a different Grade (e.g., one damaged wafer in a lot), it must be **removed from the Batch**. This removal is itself modeled as a Step (a Division pattern: Batch in → reduced Batch out + rejected Item out).

### 3.4 Job

The overarching work order or request that drives Items through Workflows. A Job is the reason Items come into existence and flow. When Items split, merge, or transform, the Job is the thread that ties the lineage together.

A Job can always answer: "What has happened so far?" and "What needs to happen next?"

### 3.5 Step Execution

A record of a Step being performed at a specific point in time. A Step Execution captures:

- Which Step was performed
- Which Job it was part of
- Which Items/Batches flowed through each Port
- Any data, measurements, or observations recorded

---

## 4. Tracking Levels

Each Kind declares two independent flags that determine how its Items are tracked:

| Serialized? | Batchable? | Example            | Behavior                                             |
|-------------|------------|---------------------|------------------------------------------------------|
| Yes         | Yes        | Semiconductor wafer | Each item has a unique ID; items grouped into lots    |
| Yes         | No         | Jet engine          | Each item has a unique ID; no batch grouping          |
| No          | Yes        | Paint               | Batch has an ID; individual units do not              |
| No          | No         | M6 bolts            | Only quantity is tracked; no individual or group IDs  |

---

## 5. Data Association Levels

Data generated during processing (measurements, observations, parameters) can be associated at three levels:

| Level           | Scope                                         | Example                          |
|-----------------|-----------------------------------------------|----------------------------------|
| Step Execution  | Everything processed in this run of the Step  | Operator name, timestamp         |
| Batch           | All items in the Batch                        | Etch bath temperature, chemistry |
| Item            | One specific serialized Item                  | Individual defect map, thickness |

---

## 6. Step Patterns

Steps are classified by their Port configuration:

| Pattern      | Input Ports | Output Ports | Example                                      |
|--------------|-------------|--------------|-----------------------------------------------|
| Transform    | 1           | 1            | Paint a part, approve a document              |
| Assembly     | N           | 1            | Join two components, add two numbers          |
| Division     | 1           | N            | Cut parts from a sheet, split a batch         |
| General      | N           | M            | Chemical reaction with multiple inputs/outputs|

---

## 7. Domain Vocabulary Mapping

The system uses generic terms internally. These are mapped to domain-specific labels via a configurable vocabulary. Users see familiar terminology; the system stays domain-neutral.

| System Term     | Semiconductor    | General Manufacturing | Finance          |
|-----------------|------------------|-----------------------|------------------|
| Kind            | Product          | Part                  | Document         |
| Kind Code       | Product Code     | Part Number           | Document Code    |
| Grade           | Qualification    | Disposition           | Status           |
| Item            | Unit             | Piece / Unit          | Record           |
| Item ID         | Wafer ID         | Serial Number         | Reference #      |
| Batch           | Lot              | Batch                 | Filing Group     |
| Batch ID        | Lot Number       | Batch Number          | Group Reference  |
| Job             | Work Order       | Work Order            | Case             |
| Workflow        | Process Flow     | Value Stream          | Procedure        |
| Process         | Process          | Process               | Process          |
| Step            | Operation        | Operation             | Task             |
