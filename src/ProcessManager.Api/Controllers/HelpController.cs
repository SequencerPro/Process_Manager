using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProcessManager.Api.Controllers;

/// <summary>
/// Provides a public, AI-consumable context document describing the Process Manager
/// domain model, key concepts, and how-to guidance.
///
/// Intended use: paste GET /api/help/context into any AI assistant's system prompt
/// (or have IT pre-load it) so the AI understands your domain before users ask questions.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/help")]
public class HelpController : ControllerBase
{
    [HttpGet("context")]
    [Produces("text/markdown")]
    public IActionResult GetContext()
    {
        return Content(ContextDocumentPublic, "text/markdown; charset=utf-8");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // The context document is written inline so it stays in the binary and
    // is always available, even in Docker deployments where the /docs folder
    // may not be present. Keep it in sync with docs/terminology.md and
    // docs/data-model.md as the system evolves.
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Public so McpController can expose it as a resource.</summary>
    internal static readonly string ContextDocumentPublic = """
        # Process Manager — AI Integration Context

        This document is the authoritative reference for an AI assistant helping users
        work with Process Manager. Read it fully before answering any questions.

        ---

        ## What is Process Manager?

        Process Manager is a manufacturing operations system that treats **process design**
        as the central organising structure of an enterprise. Users define what things are
        (Kinds and Grades), what work looks like (StepTemplates), how work is sequenced
        (Processes), and how work branches (Workflows). At runtime, Jobs flow Items and
        Batches through those designs, and every StepExecution is recorded with data,
        measurements, and observations.

        The core value proposition: a formal, auditable process model that replaces
        tribal knowledge and paper travellers.

        ---

        ## Core Concepts

        ### Kind
        What something physically or logically **is**. Examples: Widget, Wafer, Invoice,
        Steel Sheet. Defined by a code (short ID) and a name.

        Each Kind has two tracking flags:
        - **Serialized** — individual items get unique IDs (serial numbers)
        - **Batchable** — items can be grouped into Batches (lots)

        ### Grade
        The condition, qualification, or disposition an item carries. Grades are defined
        **per Kind**. Examples for Kind "Widget": Raw, Passed, Failed-Dimensional,
        Reworked, Scrapped.

        The **Kind + Grade** combination is called an **Item Type**. Ports declare exactly
        one Item Type, so the system structurally prevents wrong materials from flowing
        to the wrong places.

        ### StepTemplate
        A reusable definition of a unit of work — "the design of a step". StepTemplates
        have named Input and Output Ports. They are designed once and reused across many
        Processes.

        **Step patterns** (classified by Material port count):
        | Pattern   | Material In | Material Out | Example              |
        |-----------|------------|--------------|----------------------|
        | Transform | 1          | 1            | Paint, inspect, test |
        | Assembly  | N          | 1            | Join components      |
        | Division  | 1          | N            | Cut from sheet, sort |
        | General   | N          | M            | Multi-output bath    |

        ### Port
        A named connection point on a StepTemplate. Every Port has a **direction**
        (Input / Output) and a **PortType**:
        - **Material** — physical workpiece or batch that flows. Requires Kind + Grade + Quantity Rule.
        - **Parameter** — a controllable input setting (X-variable in quality terms).
        - **Characteristic** — a measurable output or feature (Y-variable in quality terms).
        - **Condition** — a binary pass/fail prerequisite or error-proofing check.

        ### Quantity Rule (Material ports only)
        How many items are expected at a port:
        | Mode       | Meaning                        |
        |------------|-------------------------------|
        | Exactly N  | Must be exactly N              |
        | Zero or N  | May or may not flow (optional) |
        | Range      | Between min and max            |
        | Unbounded  | At least min, no upper limit   |

        ### Process
        A strictly **linear sequence** of Steps, connected by Flows (port-to-port
        connections between adjacent Steps). There is no branching inside a Process —
        branching happens at the Workflow level.

        A Process must be **validated** before use. Validation checks that every Step's
        output ports are connected to compatible input ports on the next Step.

        ### Flow
        A connection from an Output Port of one ProcessStep to an Input Port of the next
        ProcessStep. Both ports must be of PortType Material and must declare the same
        Item Type (Kind + Grade).

        ### Workflow
        A directed graph of Processes connected by **WorkflowLinks**. Workflows express
        branching: "if a Widget is Passed, send it to Packaging; if it's Failed-Dimensional,
        send it to Rework."

        **WorkflowLink RoutingTypes:**
        - **Always** — item unconditionally follows this link
        - **GradeBased** — item follows this link only when its Grade matches a condition
        - **Manual** — a human operator decides whether to route via this link

        ### Job
        The overarching work order that drives Items through a Workflow. A Job answers
        "what has happened so far?" and "what needs to happen next?".

        **Job lifecycle:** Created → InProgress → OnHold ↔ InProgress → Completed / Cancelled

        ### Item
        A specific instance of an Item Type flowing through the system. If the Kind is
        Serialized, the Item has a unique serial number.

        ### Batch
        A tracked, homogeneous group of Items of one Kind. Carries a Grade. The Batch
        ID is the tracking unit for Batchable Kinds. All Items in the Batch inherit the
        Batch's Grade. Batches are homogeneous — no mixed Kinds.

        ### StepExecution
        A record of a Step being performed at a point in time, within a Job. Captures:
        which Items/Batches flowed through each Port, any data recorded, start/complete
        times, and the operator.

        **StepExecution lifecycle:** Pending → InProgress → Completed / Skipped / Failed

        ### PromptResponse
        A measurement or data capture recorded during a StepExecution. Each response
        belongs to a prompt defined in the StepTemplate's content (e.g., a NumericEntry
        prompt for "Width mm" with min=9.9 / max=10.1). If the value is outside the
        defined bounds, `IsOutOfRange` is set and the alert system flags it.

        ---

        ## How to Build a Process (step-by-step)

        1. **Define Kinds and Grades** (Admin → Kinds)
           - Create a Kind (e.g., "Widget", code "WDG", Serialized=true)
           - Add Grades to it: "Raw" (default), "Passed", "Failed-Dimensional", "Scrapped"

        2. **Design StepTemplates** (Admin → Step Templates)
           - Create a StepTemplate (e.g., "Dimensional Inspection", pattern=Division)
           - Add an Input Port: PortType=Material, Kind=Widget, Grade=Raw, Qty=Exactly 1
           - Add Output Port "Good Part": PortType=Material, Kind=Widget, Grade=Passed, Qty=ZeroOrN 1
           - Add Output Port "Failed Part": PortType=Material, Kind=Widget, Grade=Failed-Dimensional, Qty=ZeroOrN 1
           - Optionally add Prompt content blocks (measurements the operator records)

        3. **Compose the Process** (Admin → Processes)
           - Create a Process (e.g., "Widget Finishing")
           - Add StepTemplates as ProcessSteps in sequence (Step 1: Deburr, Step 2: Inspect)
           - Add Flows connecting each step's output port to the next step's input port
           - Click Validate — the system checks port compatibility

        4. **Optionally build a Workflow** (Admin → Workflows)
           - Only needed if work can branch to different Processes
           - Create a Workflow, add WorkflowProcesses (nodes), add WorkflowLinks (edges)
           - For GradeBased links, add WorkflowLinkConditions (which Grades follow which link)
           - Validate the Workflow

        5. **Create and run a Job** (Jobs)
           - Create a Job referencing the Process (or Workflow)
           - Click Start — StepExecutions are created (one per ProcessStep)
           - Operators navigate to Step Executions, click Work, record data, complete steps
           - Monitor progress on the Job Detail page (Gantt timeline, step status)

        ---

        ## Common Questions

        **Q: What is the difference between a StepTemplate and a ProcessStep?**
        A StepTemplate is the *design* (reusable across many Processes). A ProcessStep is
        an *instance* of a StepTemplate placed at a specific position within a specific
        Process. ProcessSteps can override the template's name and add content specific
        to that use in that Process.

        **Q: Can a Process have steps that run in parallel?**
        No. A Process is strictly linear. For parallel work, design separate Processes
        and connect them via a Workflow.

        **Q: How do I make a step optional?**
        Set the step's Input Port to Quantity Rule = ZeroOrN. At runtime the operator
        can Skip the StepExecution.

        **Q: What if an operator records a measurement outside the expected range?**
        The system sets `IsOutOfRange=true` on the PromptResponse. These appear on the
        Alerts page and the NavMenu shows a count badge. Operators can add an override
        note to accept an out-of-range value.

        **Q: Can I rename "Kind" to "Part" or "Grade" to "Disposition"?**
        Yes. Go to Vocabularies and configure a Domain Vocabulary mapping. The system
        uses generic terms internally but displays your configured labels.

        **Q: How do I track only quantities (not serial numbers)?**
        Set the Kind's Serialized=false. Items become quantity-tracked only. If you also
        set Batchable=true, you can group them into numbered Batches.

        ---

        ## API Quick Reference

        All endpoints are under `/api/`. Authentication uses JWT Bearer tokens.
        Obtain a token via `POST /api/auth/login` with `{ "userName": "...", "password": "..." }`.

        | Resource          | List                          | Get one                    | Create              |
        |-------------------|-------------------------------|----------------------------|---------------------|
        | Kinds             | GET /kinds                    | GET /kinds/{id}            | POST /kinds         |
        | Grades            | GET /kinds/{id}/grades        | —                          | POST /kinds/{id}/grades |
        | Step Templates    | GET /steptemplates            | GET /steptemplates/{id}    | POST /steptemplates |
        | Processes         | GET /processes                | GET /processes/{id}        | POST /processes     |
        | Workflows         | GET /workflows                | GET /workflows/{id}        | POST /workflows     |
        | Jobs              | GET /jobs                     | GET /jobs/{id}             | POST /jobs          |
        | Step Executions   | GET /step-executions          | GET /step-executions/{id}  | (auto-created on job start) |
        | Alerts            | GET /alerts/out-of-range      | —                          | —                   |

        **Common job transitions:**
        - `POST /api/jobs/{id}/start` — Created → InProgress
        - `POST /api/jobs/{id}/complete` — InProgress → Completed
        - `POST /api/jobs/{id}/hold` — InProgress → OnHold
        - `POST /api/jobs/{id}/cancel` — any → Cancelled

        **Common step execution transitions:**
        - `POST /api/step-executions/{id}/start`
        - `POST /api/step-executions/{id}/complete`
        - `POST /api/step-executions/{id}/skip`
        - `POST /api/step-executions/{id}/fail`

        **Data capture during a step:**
        - `POST /api/step-executions/{id}/prompt-responses` with `{ "responses": [{ "stepTemplateContentId": "...", "responseValue": "10.03" }] }`

        **MCP server** (for AI assistants): `POST /mcp`
        Supports `initialize`, `tools/list`, `tools/call`, `resources/list`, `resources/read`.
        Data tools require Bearer token. See GET /mcp for server info.

        ---

        ## Terminology Quick Reference

        | System Term    | Also known as (examples)                        |
        |----------------|-------------------------------------------------|
        | Kind           | Part, Product, Material, Document, Component    |
        | Grade          | Disposition, Qualification, Status, Condition   |
        | Item           | Unit, Piece, Serial Number                      |
        | Batch          | Lot, Bundle, Group                              |
        | Job            | Work Order, Work Request, Case                  |
        | StepTemplate   | Operation definition, Task template             |
        | Process        | Routing, Operation sequence, Process plan       |
        | Workflow       | Value stream, Process flow, Procedure           |
        | StepExecution  | Operation record, Work record                   |
        | PromptResponse | Measurement, Observation, Data capture          |
        """;
}
