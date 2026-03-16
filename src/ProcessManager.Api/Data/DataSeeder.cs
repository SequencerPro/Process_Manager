using ProcessManager.Domain.Entities;
using ProcessManager.Domain.Enums;

namespace ProcessManager.Api.Data;

/// <summary>
/// Populates the database with realistic example data on first run.
/// Skips entirely if Kinds already exist, so repeated deployments are safe.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(ProcessManagerDbContext db)
    {
        // Idempotent — skip if the seeded processes already exist.
        if (db.Processes.Any(p => p.Code == "WDG-MFG-01" || p.Code == "PCB-ASSY-01")) return;

        // ── Vocabularies ─────────────────────────────────────────────────────
        var vocabGeneral = new DomainVocabulary
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-90), UpdatedAt = Utc(-90),
            Name = "General Manufacturing",
            TermKind = "Material Kind", TermKindCode = "Material Code",
            TermGrade = "Grade", TermItem = "Unit", TermItemId = "Serial No.",
            TermBatch = "Lot", TermBatchId = "Lot No.",
            TermJob = "Work Order", TermWorkflow = "Workflow",
            TermProcess = "Process", TermStep = "Operation"
        };
        var vocabElec = new DomainVocabulary
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-90), UpdatedAt = Utc(-90),
            Name = "Electronics Production",
            TermKind = "Component Type", TermKindCode = "Part No.",
            TermGrade = "Quality Grade", TermItem = "Board", TermItemId = "Board S/N",
            TermBatch = "Panel", TermBatchId = "Panel ID",
            TermJob = "Production Order", TermWorkflow = "Build Plan",
            TermProcess = "Build Process", TermStep = "Station"
        };
        db.DomainVocabularies.AddRange(vocabGeneral, vocabElec);

        // ── Kinds & Grades ───────────────────────────────────────────────────
        var kindWidget = new Kind
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-80), UpdatedAt = Utc(-80),
            Code = "WDG-100", Name = "Widget",
            Description = "Standard machined widget component.",
            IsSerialized = true, IsBatchable = true
        };
        kindWidget.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-80), UpdatedAt = Utc(-80), KindId = kindWidget.Id, Code = "NEW",  Name = "New",     IsDefault = true,  SortOrder = 1 });
        kindWidget.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-80), UpdatedAt = Utc(-80), KindId = kindWidget.Id, Code = "A",    Name = "Grade A",  IsDefault = false, SortOrder = 2, Description = "Meets all dimensional tolerances." });
        kindWidget.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-80), UpdatedAt = Utc(-80), KindId = kindWidget.Id, Code = "B",    Name = "Grade B",  IsDefault = false, SortOrder = 3, Description = "Minor cosmetic deviation, fully functional." });
        kindWidget.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-80), UpdatedAt = Utc(-80), KindId = kindWidget.Id, Code = "SCRAP",Name = "Scrap",    IsDefault = false, SortOrder = 4, Description = "Failed inspection — do not ship." });

        var kindPcb = new Kind
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-75), UpdatedAt = Utc(-75),
            Code = "PCB-200", Name = "PCB Assembly",
            Description = "Populated printed circuit board assembly.",
            IsSerialized = true, IsBatchable = false
        };
        kindPcb.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-75), UpdatedAt = Utc(-75), KindId = kindPcb.Id, Code = "UNGRD", Name = "Ungraded", IsDefault = true,  SortOrder = 1 });
        kindPcb.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-75), UpdatedAt = Utc(-75), KindId = kindPcb.Id, Code = "PASS",  Name = "Pass",     IsDefault = false, SortOrder = 2 });
        kindPcb.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-75), UpdatedAt = Utc(-75), KindId = kindPcb.Id, Code = "FAIL",  Name = "Fail",     IsDefault = false, SortOrder = 3 });
        kindPcb.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-75), UpdatedAt = Utc(-75), KindId = kindPcb.Id, Code = "RWRK",  Name = "Rework",   IsDefault = false, SortOrder = 4 });

        var kindBulk = new Kind
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-70), UpdatedAt = Utc(-70),
            Code = "CMP-300", Name = "Compound",
            Description = "Bulk chemical compound used in coating operations.",
            IsSerialized = false, IsBatchable = true
        };
        kindBulk.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-70), UpdatedAt = Utc(-70), KindId = kindBulk.Id, Code = "STD",  Name = "Standard", IsDefault = true,  SortOrder = 1 });
        kindBulk.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-70), UpdatedAt = Utc(-70), KindId = kindBulk.Id, Code = "PREM", Name = "Premium",  IsDefault = false, SortOrder = 2 });
        kindBulk.Grades.Add(new Grade { Id = Guid.NewGuid(), CreatedAt = Utc(-70), UpdatedAt = Utc(-70), KindId = kindBulk.Id, Code = "REJ",  Name = "Reject",   IsDefault = false, SortOrder = 3 });

        db.Kinds.AddRange(kindWidget, kindPcb, kindBulk);

        // Helper: look up a grade by code
        Grade WdgGrade(string code) => kindWidget.Grades.First(g => g.Code == code);
        Grade PcbGrade(string code) => kindPcb.Grades.First(g => g.Code == code);

        // ── Step Templates ────────────────────────────────────────────────────
        var stIncomingInsp = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-60), UpdatedAt = Utc(-60),
            Code = "INSP-01", Name = "Incoming Inspection",
            Description = "Verify item dimensions, surface finish, and documentation on receipt.",
            Pattern = StepPattern.Transform, IsActive = true, Version = 1
        };
        var stCncMach = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-60), UpdatedAt = Utc(-60),
            Code = "MACH-01", Name = "CNC Machining",
            Description = "Machine component to final dimensional tolerances on CNC mill.",
            Pattern = StepPattern.Transform, IsActive = true, Version = 2
        };
        var stSubAssembly = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-55), UpdatedAt = Utc(-55),
            Code = "ASSY-01", Name = "Sub-Assembly",
            Description = "Combine sub-components into final assembly per BOM.",
            Pattern = StepPattern.Assembly, IsActive = true, Version = 1
        };
        var stFuncTest = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-55), UpdatedAt = Utc(-55),
            Code = "TEST-01", Name = "Functional Test",
            Description = "Execute automated functional test suite and record pass/fail.",
            Pattern = StepPattern.Transform, IsActive = true, Version = 3
        };
        var stVisualInsp = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-50), UpdatedAt = Utc(-50),
            Code = "INSP-02", Name = "Visual Inspection",
            Description = "Operator visual check for cosmetic defects and workmanship.",
            Pattern = StepPattern.Transform, IsActive = true, Version = 1
        };
        var stPackaging = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-50), UpdatedAt = Utc(-50),
            Code = "PACK-01", Name = "Packaging",
            Description = "Clean, bag, label, and pack units for shipment.",
            Pattern = StepPattern.General, IsActive = true, Version = 1
        };
        db.StepTemplates.AddRange(stIncomingInsp, stCncMach, stSubAssembly, stFuncTest, stVisualInsp, stPackaging);

        // ── Processes ─────────────────────────────────────────────────────────
        // Process 1: Widget Manufacturing
        var procWidget = new Process
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-45), UpdatedAt = Utc(-45),
            Code = "WDG-MFG-01", Name = "Widget Manufacturing",
            Description = "Full manufacturing flow for standard widgets from incoming goods to packaged output.",
            Version = 1, IsActive = true
        };
        var psW1 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-45), UpdatedAt = Utc(-45), ProcessId = procWidget.Id, StepTemplateId = stIncomingInsp.Id, Sequence = 1 };
        var psW2 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-45), UpdatedAt = Utc(-45), ProcessId = procWidget.Id, StepTemplateId = stCncMach.Id,      Sequence = 2 };
        var psW3 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-45), UpdatedAt = Utc(-45), ProcessId = procWidget.Id, StepTemplateId = stVisualInsp.Id,   Sequence = 3 };
        var psW4 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-45), UpdatedAt = Utc(-45), ProcessId = procWidget.Id, StepTemplateId = stPackaging.Id,    Sequence = 4 };
        procWidget.ProcessSteps.Add(psW1);
        procWidget.ProcessSteps.Add(psW2);
        procWidget.ProcessSteps.Add(psW3);
        procWidget.ProcessSteps.Add(psW4);

        // Process 2: PCB Assembly
        var procPcb = new Process
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-40), UpdatedAt = Utc(-40),
            Code = "PCB-ASSY-01", Name = "PCB Assembly & Test",
            Description = "SMT population, sub-assembly, and functional test for PCB assemblies.",
            Version = 1, IsActive = true
        };
        var psP1 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-40), UpdatedAt = Utc(-40), ProcessId = procPcb.Id, StepTemplateId = stIncomingInsp.Id, Sequence = 1 };
        var psP2 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-40), UpdatedAt = Utc(-40), ProcessId = procPcb.Id, StepTemplateId = stSubAssembly.Id,  Sequence = 2 };
        var psP3 = new ProcessStep { Id = Guid.NewGuid(), CreatedAt = Utc(-40), UpdatedAt = Utc(-40), ProcessId = procPcb.Id, StepTemplateId = stFuncTest.Id,      Sequence = 3 };
        procPcb.ProcessSteps.Add(psP1);
        procPcb.ProcessSteps.Add(psP2);
        procPcb.ProcessSteps.Add(psP3);

        db.Processes.AddRange(procWidget, procPcb);

        // ── Jobs, Items & Step Executions ────────────────────────────────────

        // ── Job 1: Completed widget run (20 days ago) ─────────────────────────
        var job1 = new Job
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-25), UpdatedAt = Utc(-20),
            Code = "WO-2026-001", Name = "Widget Batch Run — Sprint 1",
            Description = "First production run of WDG-100 widgets for customer order CO-4421.",
            ProcessId = procWidget.Id, Status = JobStatus.Completed,
            Priority = 3, StartedAt = Utc(-24), CompletedAt = Utc(-20)
        };
        AddCompletedStepExecutions(job1, new[] { psW1, psW2, psW3, psW4 }, Utc(-24), Utc(-20));
        for (int i = 1; i <= 5; i++)
        {
            var grade = i <= 4 ? WdgGrade("A") : WdgGrade("B");
            job1.Items.Add(new Item
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-25), UpdatedAt = Utc(-20),
                SerialNumber = $"WDG-{i:D4}", KindId = kindWidget.Id, GradeId = grade.Id,
                JobId = job1.Id, Status = ItemStatus.Completed
            });
        }

        // ── Job 2: In-progress widget run (started 5 days ago) ───────────────
        var job2 = new Job
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-7), UpdatedAt = Utc(-1),
            Code = "WO-2026-002", Name = "Widget Batch Run — Sprint 2",
            Description = "Second production run. Targeting 8 units.",
            ProcessId = procWidget.Id, Status = JobStatus.InProgress,
            Priority = 5, StartedAt = Utc(-5)
        };
        // Step 1 complete, step 2 in-progress, steps 3-4 pending
        job2.StepExecutions.Add(MakeStepExecution(job2.Id, psW1, 1, StepExecutionStatus.Completed,  Utc(-5), Utc(-4)));
        job2.StepExecutions.Add(MakeStepExecution(job2.Id, psW2, 2, StepExecutionStatus.InProgress, Utc(-3), null));
        job2.StepExecutions.Add(MakeStepExecution(job2.Id, psW3, 3, StepExecutionStatus.Pending,    null,    null));
        job2.StepExecutions.Add(MakeStepExecution(job2.Id, psW4, 4, StepExecutionStatus.Pending,    null,    null));
        for (int i = 6; i <= 10; i++)
        {
            var status = i <= 8 ? ItemStatus.InProcess : ItemStatus.Available;
            job2.Items.Add(new Item
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-7), UpdatedAt = Utc(-2),
                SerialNumber = $"WDG-{i:D4}", KindId = kindWidget.Id, GradeId = WdgGrade("NEW").Id,
                JobId = job2.Id, Status = status
            });
        }

        // ── Job 3: Created PCB order (not yet started) ───────────────────────
        var job3 = new Job
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-2), UpdatedAt = Utc(-2),
            Code = "PO-2026-003", Name = "PCB Assembly — Rev B Boards",
            Description = "Build 4 units of PCB-200 Rev B for prototype validation.",
            ProcessId = procPcb.Id, Status = JobStatus.Created,
            Priority = 2
        };
        job3.StepExecutions.Add(MakeStepExecution(job3.Id, psP1, 1, StepExecutionStatus.Pending, null, null));
        job3.StepExecutions.Add(MakeStepExecution(job3.Id, psP2, 2, StepExecutionStatus.Pending, null, null));
        job3.StepExecutions.Add(MakeStepExecution(job3.Id, psP3, 3, StepExecutionStatus.Pending, null, null));
        for (int i = 1; i <= 4; i++)
        {
            job3.Items.Add(new Item
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-2), UpdatedAt = Utc(-2),
                SerialNumber = $"PCB-{i:D4}", KindId = kindPcb.Id, GradeId = PcbGrade("UNGRD").Id,
                JobId = job3.Id, Status = ItemStatus.Available
            });
        }

        // ── Job 4: On-hold widget order ───────────────────────────────────────
        var job4 = new Job
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-15), UpdatedAt = Utc(-10),
            Code = "WO-2026-004", Name = "Widget Run — Customer Hold",
            Description = "Order placed on hold pending customer specification change.",
            ProcessId = procWidget.Id, Status = JobStatus.OnHold,
            Priority = 1, StartedAt = Utc(-14)
        };
        job4.StepExecutions.Add(MakeStepExecution(job4.Id, psW1, 1, StepExecutionStatus.Completed,  Utc(-14), Utc(-13)));
        job4.StepExecutions.Add(MakeStepExecution(job4.Id, psW2, 2, StepExecutionStatus.Pending,    null,     null));
        job4.StepExecutions.Add(MakeStepExecution(job4.Id, psW3, 3, StepExecutionStatus.Pending,    null,     null));
        job4.StepExecutions.Add(MakeStepExecution(job4.Id, psW4, 4, StepExecutionStatus.Pending,    null,     null));
        for (int i = 11; i <= 13; i++)
        {
            job4.Items.Add(new Item
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-15), UpdatedAt = Utc(-10),
                SerialNumber = $"WDG-{i:D4}", KindId = kindWidget.Id, GradeId = WdgGrade("NEW").Id,
                JobId = job4.Id, Status = ItemStatus.Available
            });
        }

        // ── Job 5: Completed PCB run (older) ─────────────────────────────────
        var job5 = new Job
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-60), UpdatedAt = Utc(-50),
            Code = "PO-2026-005", Name = "PCB Assembly — Rev A Boards",
            Description = "Initial Rev A boards for engineering validation.",
            ProcessId = procPcb.Id, Status = JobStatus.Completed,
            Priority = 3, StartedAt = Utc(-58), CompletedAt = Utc(-50)
        };
        AddCompletedStepExecutions(job5, new[] { psP1, psP2, psP3 }, Utc(-58), Utc(-50));
        for (int i = 5; i <= 8; i++)
        {
            var grade = i == 7 ? PcbGrade("RWRK") : PcbGrade("PASS");
            job5.Items.Add(new Item
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-60), UpdatedAt = Utc(-50),
                SerialNumber = $"PCB-REV-A-{i:D3}", KindId = kindPcb.Id, GradeId = grade.Id,
                JobId = job5.Id, Status = ItemStatus.Completed
            });
        }

        // ── Job 6: Cancelled order ────────────────────────────────────────────
        var job6 = new Job
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-30), UpdatedAt = Utc(-28),
            Code = "WO-2026-006", Name = "Widget Run — Cancelled",
            Description = "Order cancelled by customer before production began.",
            ProcessId = procWidget.Id, Status = JobStatus.Cancelled,
            Priority = 2
        };
        job6.StepExecutions.Add(MakeStepExecution(job6.Id, psW1, 1, StepExecutionStatus.Pending, null, null));
        job6.StepExecutions.Add(MakeStepExecution(job6.Id, psW2, 2, StepExecutionStatus.Pending, null, null));
        job6.StepExecutions.Add(MakeStepExecution(job6.Id, psW3, 3, StepExecutionStatus.Pending, null, null));
        job6.StepExecutions.Add(MakeStepExecution(job6.Id, psW4, 4, StepExecutionStatus.Pending, null, null));

        db.Jobs.AddRange(job1, job2, job3, job4, job5, job6);

        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ISO 9001:2015 QMS Document seed
    // Own idempotency guard → safe to run against databases that already have
    // the manufacturing demo data.
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task SeedQmsDocumentsAsync(ProcessManagerDbContext db)
    {
        if (db.Processes.Any(p => p.Code == "QMS-001")) return;

        // Helper — DRY up Process construction
        static Process Doc(string code, string name, string description,
                           ProcessStatus status, string revision, int version,
                           int createdDaysAgo, int? effectiveDaysAgo = null) =>
            new()
            {
                Id          = Guid.NewGuid(),
                CreatedAt   = Utc(-createdDaysAgo),
                UpdatedAt   = Utc(-createdDaysAgo),
                Code        = code,
                Name        = name,
                Description = description,
                ProcessRole = ProcessRole.QmsDocument,
                Status      = status,
                IsActive    = true,
                Version     = version,
                RevisionCode = revision,
                EffectiveDate = effectiveDaysAgo.HasValue ? Utc(-effectiveDaysAgo.Value) : null
            };

        // ── 1. QMS Framework ─────────────────────────────────────────────────
        var qms001 = Doc("QMS-001", "Quality Management System Scope",
            "Defines the boundaries and applicability of the QMS including the products and services " +
            "covered, relevant interested parties, and any exclusions with justification per clause 4.3.",
            ProcessStatus.Released, "B", 2, 365, 300);

        var qms002 = Doc("QMS-002", "Quality Policy",
            "Top-management statement of the organisation's commitment to quality, the framework for " +
            "setting quality objectives, and its intent to satisfy applicable requirements and continually " +
            "improve the QMS per clause 5.2.",
            ProcessStatus.Released, "B", 2, 365, 300);

        var qms003 = Doc("QMS-003", "Quality Objectives and Planning",
            "Register of measurable quality objectives aligned to the quality policy, together with " +
            "plans specifying who is responsible, what resources are needed, when results will be " +
            "reviewed, and how achievement will be evaluated per clause 6.2.",
            ProcessStatus.Released, "A", 1, 300, 280);

        var qms004 = Doc("QMS-004", "Quality Manual",
            "High-level overview of the QMS structure, the interaction between its processes, and " +
            "references to applicable procedures. Provides context on the organisation per clause 4.",
            ProcessStatus.Released, "C", 3, 400, 330);

        var qms005 = Doc("QMS-005", "Risk and Opportunity Management Procedure",
            "Describes how risks and opportunities are identified, assessed, and addressed to give " +
            "assurance that the QMS can achieve its intended outcomes and prevent unwanted effects " +
            "per clause 6.1.",
            ProcessStatus.Released, "A", 1, 280, 260);

        // ── 2. Support Procedures (Clause 7) ─────────────────────────────────
        var qms006 = Doc("QMS-006", "Control of Documented Information Procedure",
            "Governs the creation, updating, distribution, access, retrieval, storage, preservation, " +
            "version control, and disposition of all documented information required by the QMS " +
            "per clause 7.5.",
            ProcessStatus.Released, "B", 2, 360, 340);

        var qms007 = Doc("QMS-007", "Competence, Training and Awareness Procedure",
            "Defines how competence requirements are determined, how training needs are identified and " +
            "fulfilled, how effectiveness is evaluated, and how awareness of quality policy and relevant " +
            "objectives is maintained per clause 7.2 and 7.3.",
            ProcessStatus.Released, "A", 1, 250, 230);

        var qms008 = Doc("QMS-008", "Calibration and Monitoring Equipment Control",
            "Covers the identification, calibration, verification, and handling of all monitoring and " +
            "measuring resources; maintains a calibration register and defines out-of-tolerance response " +
            "actions per clause 7.1.5.",
            ProcessStatus.Released, "A", 1, 240, 220);

        var qms009 = Doc("QMS-009", "Customer Communication Procedure",
            "Describes channels and responsibilities for communicating with customers regarding product " +
            "and service information, enquiries, contracts, orders, feedback, complaints, and contingency " +
            "requirements per clause 8.2.1.",
            ProcessStatus.Released, "A", 1, 200, 185);

        // ── 3. Operations Procedures (Clause 8) ──────────────────────────────
        var qms010 = Doc("QMS-010", "Customer Requirements Review Procedure",
            "Defines the process for determining, reviewing, and confirming customer and statutory " +
            "requirements before commitment to supply, and managing changes to requirements after " +
            "acceptance per clause 8.2.2–8.2.4.",
            ProcessStatus.Released, "A", 1, 220, 200);

        var qms011 = Doc("QMS-011", "Design and Development Procedure",
            "Establishes planning, input, output, review, verification, validation, and transfer " +
            "controls for design and development activities, including management of changes to " +
            "design outputs per clause 8.3.",
            ProcessStatus.Released, "A", 1, 210, 195);

        var qms012 = Doc("QMS-012", "Supplier and External Provider Control Procedure",
            "Sets criteria for evaluating, selecting, monitoring, and re-evaluating external providers; " +
            "defines how the type and extent of control is determined based on risk and impact on " +
            "conforming outputs per clause 8.4.",
            ProcessStatus.Released, "B", 2, 310, 290);

        var qms013 = Doc("QMS-013", "Identification and Traceability Procedure",
            "Defines how products and services are identified throughout production and service " +
            "provision, and how traceability is maintained and recorded where it is a requirement " +
            "per clause 8.5.2.",
            ProcessStatus.Released, "A", 1, 190, 175);

        var qms014 = Doc("QMS-014", "Customer and External Provider Property Procedure",
            "Describes how property belonging to customers or external providers (including intellectual " +
            "property and personal data) is identified, protected, safeguarded, and reported upon " +
            "if lost, damaged, or found unsuitable per clause 8.5.3.",
            ProcessStatus.Released, "A", 1, 180, 165);

        var qms015 = Doc("QMS-015", "Preservation and Handling Procedure",
            "Specifies requirements for handling, packaging, storage, protection, and delivery of " +
            "products and service outputs to prevent damage and deterioration during internal " +
            "processing and final delivery per clause 8.5.4.",
            ProcessStatus.Released, "A", 1, 170, 155);

        var qms016 = Doc("QMS-016", "Control of Nonconforming Outputs Procedure",
            "Defines how products and services that do not conform to requirements are identified, " +
            "segregated, evaluated, and dispositioned (rework, concession, scrap, containment), " +
            "and how documented information is retained per clause 8.7.",
            ProcessStatus.Released, "B", 2, 320, 300);

        // ── 4. Performance Evaluation and Improvement (Clauses 9–10) ─────────
        var qms017 = Doc("QMS-017", "Customer Satisfaction Monitoring Procedure",
            "Describes methods for monitoring and measuring the degree to which customer needs and " +
            "expectations have been fulfilled, including surveys, complaint analysis, and warranty " +
            "returns per clause 9.1.2.",
            ProcessStatus.Released, "A", 1, 160, 145);

        var qms018 = Doc("QMS-018", "Internal Audit Procedure",
            "Establishes the internal audit programme, audit criteria, scope, frequency, methods, " +
            "auditor competence and objectivity requirements, and responsibilities for reporting " +
            "results and taking corrective action per clause 9.2.",
            ProcessStatus.Released, "B", 2, 350, 330);

        var qms019 = Doc("QMS-019", "Management Review Procedure",
            "Defines the inputs, outputs, frequency, and responsibilities for management review of " +
            "the QMS to ensure its continuing suitability, adequacy, effectiveness, and alignment " +
            "with strategic direction per clause 9.3.",
            ProcessStatus.Released, "A", 1, 340, 320);

        var qms020 = Doc("QMS-020", "Corrective Action and Continual Improvement Procedure",
            "Describes how nonconformities are reacted to, root causes identified, corrective actions " +
            "implemented and verified for effectiveness, and how the QMS is continually improved " +
            "per clause 10.2 and 10.3.",
            ProcessStatus.Released, "B", 2, 355, 335);

        // QMS-021 intentionally in Draft — new document under review
        var qms021 = Doc("QMS-021", "Knowledge Management Procedure",
            "Addresses how the organisation determines, maintains, and makes available the knowledge " +
            "necessary for operation and for achieving conformity of products and services, and how " +
            "it acquires additional knowledge per clause 7.1.6.",
            ProcessStatus.Draft, "A", 1, 30);

        // ── Shared step template for all QMS document sections ───────────────
        var stDocSect = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-400), UpdatedAt = Utc(-400),
            Code = "DOC-SECT-01", Name = "Document Section",
            Description = "A numbered section within a controlled QMS procedure document.",
            Pattern = StepPattern.General, IsActive = true, IsShared = true,
            Status = ProcessStatus.Released, Version = 1
        };
        db.StepTemplates.Add(stDocSect);

        static void AddQmsSteps(Process doc, StepTemplate tmpl,
            string purpose, string responsibilities, string procedure, string records)
        {
            doc.ProcessSteps.Add(new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = doc.CreatedAt, UpdatedAt = doc.CreatedAt,
                ProcessId = doc.Id, StepTemplateId = tmpl.Id, Sequence = 1,
                NameOverride = "Purpose and Scope", DescriptionOverride = purpose
            });
            doc.ProcessSteps.Add(new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = doc.CreatedAt, UpdatedAt = doc.CreatedAt,
                ProcessId = doc.Id, StepTemplateId = tmpl.Id, Sequence = 2,
                NameOverride = "Responsibilities", DescriptionOverride = responsibilities
            });
            doc.ProcessSteps.Add(new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = doc.CreatedAt, UpdatedAt = doc.CreatedAt,
                ProcessId = doc.Id, StepTemplateId = tmpl.Id, Sequence = 3,
                NameOverride = "Procedure", DescriptionOverride = procedure
            });
            doc.ProcessSteps.Add(new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = doc.CreatedAt, UpdatedAt = doc.CreatedAt,
                ProcessId = doc.Id, StepTemplateId = tmpl.Id, Sequence = 4,
                NameOverride = "Records and Documented Information", DescriptionOverride = records
            });
        }

        AddQmsSteps(qms001, stDocSect,
            "Defines the boundaries and applicability of the QMS including the products and services covered, relevant interested parties, and any exclusions with justification per clause 4.3.",
            "Top management is accountable for defining the scope; the Quality Manager maintains, communicates, and reviews the scope statement whenever the organisation's context changes.",
            "Review organisational context (clauses 4.1–4.2); determine relevant products, services, and interested parties; identify and justify any exclusions; obtain top-management sign-off; communicate the approved scope to all relevant parties before release.",
            "Signed scope statement, interested-parties register, exclusion justification records, management approval record.");

        AddQmsSteps(qms002, stDocSect,
            "Communicates top management's commitment to quality and provides the framework for setting objectives and satisfying applicable requirements per clause 5.2.",
            "Top management authors and is accountable for the policy; the Quality Manager distributes, maintains awareness records, and ensures the policy is reviewed at each management review.",
            "Draft the policy aligned to organisational context and strategy; verify all clause 5.2 requirements are addressed; obtain top-management signature; communicate to all personnel and post at relevant locations; review annually and update as required.",
            "Signed quality policy, distribution and acknowledgement records, management review minutes reflecting policy review.");

        AddQmsSteps(qms003, stDocSect,
            "Establishes measurable quality objectives aligned to the quality policy and specifies plans including owners, resources, timelines, and evaluation methods per clause 6.2.",
            "The Quality Manager compiles and maintains the objectives register; process owners own individual objectives and report progress; senior management reviews achievement at each management review.",
            "For each objective record the measure, target value, responsible owner, required resources, milestone dates, and review frequency; monitor progress monthly; report deviations with corrective actions; close and archive achieved objectives.",
            "Quality objectives register, monthly progress reports, management review minutes, objective closure records.");

        AddQmsSteps(qms004, stDocSect,
            "Provides a high-level overview of the QMS structure and the interaction between processes, and references all applicable procedures per ISO 9001:2015 clause 4.",
            "The Quality Manager maintains the manual and proposes revisions; top management approves each revision; the manual is available to all staff and relevant external parties on request.",
            "Document organisational context, leadership commitments, planning approach, support processes, operations overview, performance evaluation approach, and improvement mechanism; link to all applicable procedure codes; review at minimum annually.",
            "Approved quality manual with revision history, distribution acknowledgement log, management approval record.");

        AddQmsSteps(qms005, stDocSect,
            "Describes identification, assessment, and treatment of risks and opportunities to protect QMS intended outcomes and prevent undesired effects per clause 6.1.",
            "Process owners identify and own the risks for their processes; the Quality Manager maintains the register and facilitates assessments; management reviews significant risks and the register at each management review.",
            "Identify risks and opportunities for each QMS process; assess likelihood and consequence; determine whether to eliminate, reduce, accept, or exploit; assign owners and implement actions; monitor effectiveness; update the register whenever context changes and at each management review.",
            "Risk and opportunity register, risk assessment records, treatment action tracker, management review minutes.");

        AddQmsSteps(qms006, stDocSect,
            "Governs creation, updating, distribution, access, retrieval, storage, preservation, and disposal of all documented information required or maintained by the QMS per clause 7.5.",
            "The Document Controller or Quality Manager approves all document changes and maintains the master register; department managers control local access; IT maintains the document management system and backup schedule.",
            "Assign a unique code and title using the approved naming convention; author or update using the approved template; submit for review and approval; distribute to all affected functions; protect from inadvertent use of obsolete versions; retain for the defined period; dispose of securely at retention end.",
            "Document register, approval and review records, distribution records, obsolescence and disposal log, access control matrix.");

        AddQmsSteps(qms007, stDocSect,
            "Defines how competence requirements are determined, training needs identified and fulfilled, effectiveness evaluated, and awareness of quality commitments maintained per clauses 7.2 and 7.3.",
            "HR and line managers identify competence requirements and nominate staff for training; the Training Coordinator schedules courses and maintains records; individuals complete assigned training within defined timescales.",
            "Define competence requirements for each role; identify gaps against current staff competence; plan and deliver training through approved methods; evaluate effectiveness by assessment or observation; update competency records; brief staff on quality policy, objectives, and their contribution.",
            "Competence framework, training needs analysis, training records, effectiveness evaluation results, awareness survey or sign-off records.");

        AddQmsSteps(qms008, stDocSect,
            "Ensures all monitoring and measuring resources are identified, calibrated, verified for fitness for purpose, and protected from damage that would invalidate results per clause 7.1.5.",
            "The Metrology Coordinator maintains the calibration register, schedules recall, and acts on out-of-tolerance findings; operators verify equipment status before use and report damage or suspected drift immediately.",
            "Identify all monitoring and measurement equipment; assign a unique ID, calibration standard, and recall interval; apply calibration status label; calibrate against traceable standards; quarantine and investigate any out-of-tolerance equipment; recall and re-evaluate all measurements taken since last valid calibration.",
            "Calibration register, calibration certificates, out-of-tolerance investigation reports, recall and re-evaluation records.");

        AddQmsSteps(qms009, stDocSect,
            "Defines channels, responsibilities, and response timescales for communicating with customers on products and services, orders, feedback, complaints, and contingency arrangements per clause 8.2.1.",
            "Sales manages enquiries and orders; Customer Service handles complaints and satisfaction monitoring; Technical responds to specification and compatibility queries; all contacts are logged in the CRM.",
            "Acknowledge all customer contacts within the agreed timescale; log enquiries, orders, complaints, and feedback in the CRM; classify complaints by severity; escalate critical issues to management; resolve and close with written customer confirmation; conduct satisfaction surveys at defined intervals.",
            "CRM log, complaint register with resolution records, customer satisfaction survey results, escalation and response records.");

        AddQmsSteps(qms010, stDocSect,
            "Ensures customer, statutory, and organisation-imposed requirements are fully determined, reviewed, and confirmed before commitment to supply, and that changes are re-reviewed per clauses 8.2.2–8.2.4.",
            "The Sales or Account Manager performs the initial review and obtains sign-off; Technical and Production confirm feasibility; post-acceptance changes are re-reviewed by the same parties before communicating to the customer.",
            "Capture all requirements from the customer (explicit and implied), applicable regulations, and internal standards; check for completeness and conflicts; resolve any gaps; confirm feasibility with Technical and Production; obtain customer confirmation; document the agreed specification; re-review and communicate all changes.",
            "Requirements review record, order confirmation, customer correspondence, change review records, feasibility sign-off.");

        AddQmsSteps(qms011, stDocSect,
            "Establishes controls for design and development planning, inputs, outputs, reviews, verification, validation, and change management to ensure outputs meet requirements per clause 8.3.",
            "The Design Lead owns the D&D plan and controls gate progression; independent reviewers provide unbiased assessment at each stage; Quality ensures gate criteria are met before authorising transfer to operations.",
            "Plan the D&D activity defining stages, reviews, responsibilities, and resources; capture and validate all inputs; generate outputs meeting input requirements; conduct formal reviews and record actions; verify outputs against inputs; validate the design against intended use in representative conditions; document and review all design changes.",
            "D&D plan, input and output records, review minutes and action log, verification and validation records, design change log, transfer authorisation.");

        AddQmsSteps(qms012, stDocSect,
            "Sets criteria and controls for evaluating, selecting, monitoring, and re-evaluating external providers to ensure externally provided products and services meet requirements per clause 8.4.",
            "Procurement manages the approved-supplier register and performance monitoring programme; Quality sets acceptance criteria and reviews KPI data; Technical approves suppliers for critical or regulated items.",
            "Apply selection criteria against each category of external provision; conduct initial assessment (audit, questionnaire, or trial order); approve or restrict accordingly; include quality requirements on all purchase orders; inspect or verify deliveries; score performance quarterly; re-assess formally and update register annually.",
            "Approved supplier register, assessment records, purchase orders with quality clauses, incoming inspection records, supplier performance scorecards, re-evaluation records.");

        AddQmsSteps(qms013, stDocSect,
            "Defines how products and services are uniquely identified throughout processing and how traceability is maintained and recorded where it is a requirement per clause 8.5.2.",
            "Production Supervisor ensures identification is applied correctly; operators record identifiers at each step; QA audits traceability records and investigates any break in chain.",
            "Apply unique identifier (serial number, lot number, or batch label) at receipt or initial production stage; maintain through all processing steps without interruption; record the identifier against job, step execution, and item records; retain complete traceability records for the product lifetime or mandatory retention period; provide traceability reports on request.",
            "Traveller, job card, or batch record; serialisation log; item history report; traceability audit findings.");

        AddQmsSteps(qms014, stDocSect,
            "Describes identification, protection, safeguarding, and reporting obligations for property belonging to customers or external providers including intellectual property and personal data per clause 8.5.3.",
            "Receiving inspects and records customer property on arrival; the designated custodian maintains safe storage; Quality must be immediately notified of any loss, damage, or unsuitability finding.",
            "Identify and tag all customer-owned items or data on receipt; record in the custody register with condition notes; store in a designated secure area; inspect condition at defined intervals; report immediately to the customer and internally if any item is lost, damaged, or found unsuitable; obtain disposal or return instructions in writing.",
            "Customer property register, custody receipts, incoming inspection records, loss or damage reports, correspondence on disposition.");

        AddQmsSteps(qms015, stDocSect,
            "Specifies requirements for handling, packaging, storage, protection, and delivery of products and service outputs to prevent damage or deterioration during internal processing and final delivery per clause 8.5.4.",
            "Stores personnel apply handling instructions and maintain storage conditions; Production applies preservation measures at each processing stage; Despatch verifies packaging and condition before shipment.",
            "Define handling and packaging requirements per product type and risk (ESD, contamination, impact, temperature, humidity, orientation); apply preservation at each stage; store in designated areas with monitored conditions; stage for despatch using approved packaging; confirm product condition and documentation completeness before release for delivery.",
            "Preservation requirements register, stores condition monitoring records, despatch inspection records, delivery documentation.");

        AddQmsSteps(qms016, stDocSect,
            "Defines how products and services that fail to conform to requirements are identified, segregated, evaluated, and dispositioned to prevent unintended use or delivery per clause 8.7.",
            "First-line operators or inspectors raise nonconformances and apply hold tagging; QA reviews and authorises disposition; Engineering approves concessions; Management reviews trends for systemic issues.",
            "Identify and physically tag the nonconforming output immediately; move to the quarantine hold area; evaluate against original requirements and acceptance criteria; select disposition (rework, concession, scrap, or return to supplier); implement and verify the disposition; re-inspect after rework; update records and analyse trend data.",
            "Nonconformance reports, hold tags, disposition records, concession approvals, rework re-inspection records, NC trend analysis.");

        AddQmsSteps(qms017, stDocSect,
            "Describes methods for monitoring and measuring customer perception to determine whether customer needs have been met and to feed results into continual improvement per clause 9.1.2.",
            "Customer Service administers surveys and logs spontaneous feedback; the Quality Manager analyses aggregated results and feeds them into the management review; Account Managers follow up on low satisfaction scores.",
            "Issue structured satisfaction surveys at defined intervals (at minimum annually); record all unsolicited feedback, complaints, and compliments; analyse complaint trends, warranty return rates, and on-time delivery performance; compile a satisfaction index; report findings to management review; define improvement actions for any metric below target.",
            "Customer satisfaction surveys, feedback log, complaint and NC trend reports, satisfaction index, management review input.");

        AddQmsSteps(qms018, stDocSect,
            "Establishes the internal audit programme to verify QMS conformance, effective implementation, and alignment with planned arrangements per clause 9.2.",
            "The Quality Manager plans and manages the audit programme; certified internal auditors conduct audits independently of their own work; auditees are responsible for completing agreed corrective actions on time.",
            "Draft the annual audit schedule covering all QMS processes and clauses; assign competent and independent auditors; issue advance notification to auditees; conduct audits using checklists and interviews; record findings and observations; issue the audit report; raise nonconformances for all findings; verify corrective action closure within agreed timescales.",
            "Audit schedule, audit plans and checklists, audit reports, nonconformance records, corrective action tracker, closure verification records.");

        AddQmsSteps(qms019, stDocSect,
            "Defines the inputs, outputs, frequency, and responsibilities for management review of the QMS to ensure continuing suitability, adequacy, effectiveness, and strategic alignment per clause 9.3.",
            "Top management chairs the review and owns resulting decisions; the Quality Manager prepares the input pack and records minutes; process owners present performance data for their areas.",
            "Schedule formal reviews at minimum twice per year; compile all required inputs (customer satisfaction, quality objectives performance, process metrics, audit results, NC trends, supplier performance, risks, resource needs, continual improvement opportunities); conduct the meeting; record decisions and assign action owners with completion dates; circulate minutes; track action completion.",
            "Management review agenda, pre-meeting input pack, meeting minutes, action tracker, previous review follow-up records.");

        AddQmsSteps(qms020, stDocSect,
            "Describes how nonconformities are reacted to, root causes identified, corrective actions implemented and verified for effectiveness, and the QMS continually improved per clauses 10.2 and 10.3.",
            "The problem owner drives root-cause investigation and corrective action implementation; the Quality Manager verifies effectiveness and maintains the corrective action register; Management reviews significant systemic issues.",
            "React to the nonconformity and contain its immediate effect; investigate root cause using a structured technique (5-Why, Fishbone, Fault Tree); implement the corrective action; verify effectiveness by monitoring for recurrence over an appropriate period; identify opportunities for broader QMS improvement; update the risk register if relevant.",
            "Corrective action register, root-cause analysis records, action implementation evidence, effectiveness review outcome, improvement log.");

        AddQmsSteps(qms021, stDocSect,
            "Addresses how the organisation determines, maintains, and makes available the knowledge necessary for operations and for achieving conformity of products and services per clause 7.1.6.",
            "Knowledge owners document and maintain their domain knowledge; the QMS Manager maintains the knowledge asset register; HR ensures planned knowledge transfer during role transitions and succession events.",
            "Identify the knowledge needed for each key process and role; document and store in approved repositories (procedures, work instructions, training materials, databases); review currency annually; plan and execute knowledge transfer when roles change; identify gaps arising from new technology, regulatory changes, or market evolution; acquire additional knowledge through training, recruitment, or partnership.",
            "Knowledge asset register, knowledge capture and transfer records, gap analysis, review and update logs.");

        db.Processes.AddRange(
            qms001, qms002, qms003, qms004, qms005,
            qms006, qms007, qms008, qms009, qms010,
            qms011, qms012, qms013, qms014, qms015,
            qms016, qms017, qms018, qms019, qms020,
            qms021);

        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // System onboarding training documents
    // Each course teaches users a real feature of Process Manager, so browsing
    // the Training Catalogue acts as an interactive orientation guide.
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task SeedTrainingDocumentsAsync(ProcessManagerDbContext db)
    {
        if (db.Processes.Any(p => p.Code == "TRN-SYS-001")) return;

        static Process Course(
            string code, string name, string competencyTitle, string description,
            int? expiryDays, string revision, int version, int createdDaysAgo) =>
            new()
            {
                Id                   = Guid.NewGuid(),
                CreatedAt            = Utc(-createdDaysAgo),
                UpdatedAt            = Utc(-createdDaysAgo),
                Code                 = code,
                Name                 = name,
                CompetencyTitle      = competencyTitle,
                Description          = description,
                CompetencyExpiryDays = expiryDays,
                ProcessRole          = ProcessRole.Training,
                Status               = ProcessStatus.Released,
                IsActive             = true,
                Version              = version,
                RevisionCode         = revision,
                EffectiveDate        = Utc(-createdDaysAgo)
            };

        // ── Module 1 — Orientation ────────────────────────────────────────────
        var trn001 = Course(
            "TRN-SYS-001",
            "Introduction to Process Manager",
            "Process Manager Fundamentals",
            "An overview of the Process Manager platform and how its modules fit together. " +
            "Covers the core concepts of Kinds, Processes, Jobs, and Items; explains the difference " +
            "between the Design (processes & documents), Execution (jobs & workflows), Quality " +
            "(non-conformances & approvals), Training, and Accountability modules; and introduces " +
            "the vocabulary system that lets each organisation rename terms to match their own " +
            "industry language (e.g. 'Work Order' vs 'Production Order', 'Board' vs 'Unit'). " +
            "Completion of this course is the recommended first step for all new users.",
            expiryDays: null, "A", 1, 90);

        var trn002 = Course(
            "TRN-SYS-002",
            "Navigating the Interface",
            "UI Navigation",
            "A practical walkthrough of the Process Manager user interface. Covers the collapsible " +
            "navigation sidebar and its sections (Design, Execution, Config, Quality, Document Library, " +
            "Accountability, Training, Admin, Reports); explains the breadcrumb trail, page-level " +
            "action buttons, search boxes, and pagination controls; demonstrates switching between " +
            "list and detail views; and explains how the active/inactive toggle and status badges " +
            "appear throughout the application. No prior system knowledge required.",
            expiryDays: null, "A", 1, 90);

        // ── Module 2 — Design ─────────────────────────────────────────────────
        var trn003 = Course(
            "TRN-SYS-003",
            "Building and Managing Processes",
            "Process Builder Proficiency",
            "Covers the full authoring lifecycle for a manufacturing or service process. Explains " +
            "how to create a new process (code, name, description, and process role); add steps " +
            "from the step template library; reorder and remove steps; and save the process. " +
            "Introduces the three Process Builder views — Diagram (flowchart canvas with drag-and-drop " +
            "nodes), Slide (PowerPoint-style panel-per-step editor for detailed content blocks), and " +
            "Document (read-only typeset view for review and printing). Explains how to add rich " +
            "content to steps (instructions, cautions, images, tables, and reference links) and how " +
            "version control and the approval workflow interact with the builder.",
            expiryDays: 365, "A", 1, 85);

        var trn004 = Course(
            "TRN-SYS-004",
            "Managing Step Templates",
            "Step Template Administration",
            "Step templates are the reusable building blocks that engineers assemble into processes. " +
            "This course explains the difference between step templates and process steps; covers how " +
            "to create a new template (code, name, description, pattern, and port configuration); " +
            "demonstrates editing and versioning an existing template; and explains how changes to a " +
            "template propagate (or do not propagate) to processes that already reference it. " +
            "Also covers the five step patterns — Transform, Assembly, Disassembly, Inspection, and " +
            "General — and when to use each one.",
            expiryDays: 365, "A", 1, 85);

        // ── Module 3 — Document Library ───────────────────────────────────────
        var trn005 = Course(
            "TRN-SYS-005",
            "Using the Document Library",
            "Document Library User",
            "The Document Library surfaces all processes whose role is QMS Document, Work Instruction, " +
            "or Manufacturing Process — giving them a document-centric view with code, title, revision, " +
            "status, and effective date. This course explains how to browse, filter (by type, status, " +
            "and search text), and view a document's full detail page; how to navigate between " +
            "the Document view and the Process Builder for the same record; and how to understand " +
            "the lifecycle states (Draft → Pending Approval → Released → Superseded). Covers the " +
            "difference between a QMS Document, a Work Instruction, and a Manufacturing Process.",
            expiryDays: null, "A", 1, 80);

        var trn006 = Course(
            "TRN-SYS-006",
            "Document Approval Workflow",
            "Document Approver",
            "Explains the controlled-document approval cycle built into Process Manager. Covers how " +
            "an engineer submits a Draft document for approval (selecting the appropriate approval " +
            "process template and writing a change description); how approvers receive and act on " +
            "approval requests (approve, reject, or request changes); how an approved document " +
            "transitions to Released with an effective date; and how to create a new revision of a " +
            "Released document, automatically superseding the previous version. Explains audit-trail " +
            "records and who can perform each action based on user role (Admin vs Engineer vs standard user).",
            expiryDays: 730, "A", 1, 80);

        // ── Module 4 — Execution ──────────────────────────────────────────────
        var trn007 = Course(
            "TRN-SYS-007",
            "Creating and Managing Jobs",
            "Job Execution — Operator",
            "Jobs are the primary execution record in Process Manager — they tie a process to a " +
            "specific production run, training event, or service delivery. This course covers how to " +
            "create a new job (selecting the process, entering the job code and name, setting priority); " +
            "how to understand the job status lifecycle (Created → In Progress → Completed / On Hold / " +
            "Cancelled); how step executions are created automatically from process steps; how to start, " +
            "complete, and annotate individual step executions; and how to view the full job timeline " +
            "and step-by-step history. Also explains how Items (serialised units, batches, or bulk stock) " +
            "are attached to a job and how their grades are recorded.",
            expiryDays: 365, "A", 1, 75);

        var trn008 = Course(
            "TRN-SYS-008",
            "Working with Items, Batches and Grades",
            "Item Management",
            "Explains the Item model — the individual unit of work that flows through a process. " +
            "Covers serialised items (trackable by serial number, e.g. a PCB board), batch items " +
            "(grouped by lot number, e.g. a chemical batch), and grade assignment at each processing " +
            "stage. Demonstrates creating items within a job, updating their status and grade as they " +
            "move through steps, and running batch operations. Explains the relationship between the " +
            "Kind (product type), the Grade options defined on that Kind, and the Item record. " +
            "Covers the scrap / rework / pass disposition workflow.",
            expiryDays: 365, "A", 1, 75);

        // ── Module 5 — Quality ────────────────────────────────────────────────
        var trn009 = Course(
            "TRN-SYS-009",
            "Raising and Managing Non-Conformances",
            "Non-Conformance Reporter",
            "Non-conformances (NCs) capture deviations from requirements and drive corrective action. " +
            "This course explains when and how to raise an NC (selecting severity, category, and the " +
            "related job or item); how to describe the detected problem and the immediate containment " +
            "action; how NCs transition through their status states (Open → Under Review → Closed / " +
            "Voided); how to attach root-cause analysis and corrective action notes; and how to view " +
            "NC history for a specific item or process. Explains the difference between an NC and " +
            "a PFMEA risk entry.",
            expiryDays: 365, "A", 1, 70);

        // ── Module 6 — Analytics and Reports ─────────────────────────────────
        var trn010 = Course(
            "TRN-SYS-010",
            "Analytics, Reports and Dashboards",
            "Analytics User",
            "Process Manager captures operational data that can be surfaced through its built-in " +
            "analytics module and Power BI dashboard integration. This course explains how to use the " +
            "Analytics page to view job throughput, step cycle-time trends, and NC rates; how to run " +
            "and export the standard Reports (job summary, process performance, competency status); " +
            "and how to connect a Power BI workspace to the Process Manager API for custom reporting. " +
            "Covers the run-chart view for monitoring process metrics over time and how to interpret " +
            "common quality indicators.",
            expiryDays: null, "A", 1, 65);

        // ── Module 7 — Training and Competency ───────────────────────────────
        var trn011 = Course(
            "TRN-SYS-011",
            "Training Catalogue and Competency Records",
            "Training System User",
            "This course explains the Training module you are using right now. Training processes " +
            "(like this one) appear in the Training Catalogue alongside the learner's own competency " +
            "history for each course. Launching a training course creates a Job of type Training, and " +
            "completing that job generates a Competency Record. Competency records have a status " +
            "(Current, Expiring Soon, Expired) based on the CompetencyExpiryDays setting on the " +
            "course. The Competency Matrix (Admin/Engineer view) shows every user's competency status " +
            "across all active training courses as a colour-coded grid. This course also explains how " +
            "Admins and Engineers create new training courses in the Process Builder and publish them " +
            "by setting status to Released.",
            expiryDays: null, "B", 2, 60);

        // ── Module 8 — Administration ─────────────────────────────────────────
        var trn012 = Course(
            "TRN-SYS-012",
            "User Administration and Role Management",
            "User Administrator",
            "Covers the Admin section of Process Manager, accessible only to users with the Admin " +
            "role. Explains the two built-in roles — Admin (full access including user management, " +
            "admin-release of documents, and all configuration) and Engineer (design and authoring " +
            "access: can create and edit processes, documents, step templates, and training courses, " +
            "but cannot manage users or perform admin-release). Demonstrates how to create a new " +
            "user account, assign or remove roles, reset passwords, and deactivate accounts. " +
            "Covers the Domain Vocabulary configuration page where terminology can be customised " +
            "per-process to match the organisation's own language.",
            expiryDays: 730, "A", 1, 55);

        // ── Shared step template for all training course modules ─────────────
        var stTrnMod = new StepTemplate
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-95), UpdatedAt = Utc(-95),
            Code = "TRN-MOD-01", Name = "Training Module",
            Description = "A learning module or topic within a training course.",
            Pattern = StepPattern.General, IsActive = true, IsShared = true,
            Status = ProcessStatus.Released, Version = 1
        };
        db.StepTemplates.Add(stTrnMod);

        static void AddTrnStep(Process course, StepTemplate tmpl, int seq, string name, string description)
        {
            course.ProcessSteps.Add(new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = course.CreatedAt, UpdatedAt = course.CreatedAt,
                ProcessId = course.Id, StepTemplateId = tmpl.Id, Sequence = seq,
                NameOverride = name, DescriptionOverride = description
            });
        }

        // TRN-SYS-001 ─ Introduction to Process Manager
        AddTrnStep(trn001, stTrnMod, 1, "What is Process Manager?",
            "Overview of the platform, its purpose, and the business problems it solves for manufacturing, quality, and training teams.");
        AddTrnStep(trn001, stTrnMod, 2, "Core Concepts: Kinds, Grades, and Items",
            "The material model explained: Kinds (product types), Grades (quality levels), and Items (individually tracked serialised units, batches, or bulk stock).");
        AddTrnStep(trn001, stTrnMod, 3, "Processes and Jobs",
            "The relationship between a Process (the reusable step template) and a Job (the execution record for a specific production run, training event, or service delivery), including the step execution lifecycle.");
        AddTrnStep(trn001, stTrnMod, 4, "Platform Modules and Navigation",
            "Guided tour of each UI section: Design, Execution, Quality, Config, Document Library, Accountability, Training, Admin, and Reports — and how they relate to each other.");
        AddTrnStep(trn001, stTrnMod, 5, "Knowledge Check",
            "Verify understanding of the platform's core concepts through a series of review questions covering Kinds, Processes, Jobs, and the document lifecycle.");

        // TRN-SYS-002 ─ Navigating the Interface
        AddTrnStep(trn002, stTrnMod, 1, "The Navigation Sidebar",
            "Using the collapsible sidebar sections (Design, Execution, Config, Quality, Document Library, Accountability, Training, Admin, Reports), understanding the active-item highlight, and expanding or collapsing section groups.");
        AddTrnStep(trn002, stTrnMod, 2, "List Pages, Search, and Filters",
            "Using search boxes, dropdown type filters, status badge filters, and pagination controls to locate items quickly in any list view.");
        AddTrnStep(trn002, stTrnMod, 3, "Detail Pages and Action Buttons",
            "Understanding the breadcrumb trail, interpreting page-level action buttons (Edit, Builder, Submit for Approval, Archive, Delete), and using inline confirmation modals safely.");
        AddTrnStep(trn002, stTrnMod, 4, "Knowledge Check",
            "Demonstrate the ability to navigate to any major section and correctly interpret page elements, status badges, and action buttons presented on screen.");

        // TRN-SYS-003 ─ Building and Managing Processes
        AddTrnStep(trn003, stTrnMod, 1, "Creating a Process",
            "How to open the Process Builder and create a new process by entering the code, name, description, and process role (Manufacturing Process, QMS Document, Work Instruction, Training, or Approval Process).");
        AddTrnStep(trn003, stTrnMod, 2, "Adding and Arranging Steps",
            "Browsing the step template library, adding steps to the process, and reordering or removing them to match the intended workflow sequence.");
        AddTrnStep(trn003, stTrnMod, 3, "Builder Views: Diagram, Slide, and Document",
            "When to use the Diagram view (flowchart canvas with drag-and-drop nodes), the Slide view (panel-per-step content editor), and the Document view (typeset read-only output for review and printing).");
        AddTrnStep(trn003, stTrnMod, 4, "Adding Rich Content to Steps",
            "Writing instructions and cautions; inserting images; adding reference links; and using the text editor tools in the Slide view to produce operator-ready work instructions.");
        AddTrnStep(trn003, stTrnMod, 5, "Version Control and the Approval Workflow",
            "How to save a draft, submit for approval with a change description, track review progress, act on reviewer feedback, and publish a new released revision of the process.");

        // TRN-SYS-004 ─ Managing Step Templates
        AddTrnStep(trn004, stTrnMod, 1, "Step Templates vs Process Steps",
            "Understanding the distinction: a Step Template is a reusable definition in the shared library; a Process Step is an instance of that template placed at a specific position in one process with optional name and description overrides.");
        AddTrnStep(trn004, stTrnMod, 2, "Creating a New Step Template",
            "Setting code, name, description, step pattern (Transform, Assembly, Division, General), and port configuration for a new reusable step template.");
        AddTrnStep(trn004, stTrnMod, 3, "Editing and Versioning Templates",
            "How to update a template's instructions, description, or ports; how version numbering works; and the impact on processes that already reference the template.");
        AddTrnStep(trn004, stTrnMod, 4, "Step Patterns Explained",
            "When to use Transform (1 input, 1 output for single-unit processing), Assembly (multiple inputs, 1 output for combining parts), Division (1 input, multiple outputs for splitting), and General (any configuration for document-type steps).");

        // TRN-SYS-005 ─ Using the Document Library
        AddTrnStep(trn005, stTrnMod, 1, "Document Library Overview",
            "How the Document Library differs from the Processes design list: code, revision label, effective date, and document role are surfaced to support a document-centric view for QMS, instructions, and training.");
        AddTrnStep(trn005, stTrnMod, 2, "Browsing and Filtering Documents",
            "Using the type filter (QMS Document, Work Instruction, Manufacturing Process, Training), the status filter, and the search box to locate a specific document quickly.");
        AddTrnStep(trn005, stTrnMod, 3, "Viewing a Document's Detail Page",
            "Reading the description, stepping through the document sections (steps), reviewing the revision history, and using the Builder and Edit buttons to switch into the authoring view.");
        AddTrnStep(trn005, stTrnMod, 4, "Document Lifecycle States",
            "Understanding Draft, Pending Approval, Released, Superseded, and Retired states — what each means for editing, usage in jobs, and discovery in the library.");

        // TRN-SYS-006 ─ Document Approval Workflow
        AddTrnStep(trn006, stTrnMod, 1, "Submitting a Document for Approval",
            "How an Engineer submits a Draft document: writing a meaningful change description, selecting the approval process template, and assigning named reviewers to each approval step.");
        AddTrnStep(trn006, stTrnMod, 2, "Acting as an Approver",
            "Finding your pending approval tasks in My Work or the Approval Queue; reviewing the change description and document content; approving or rejecting with a comment; and withdrawing an approval request if needed.");
        AddTrnStep(trn006, stTrnMod, 3, "Releasing a Document",
            "How an approved document automatically transitions to Released state; the role of the effective date; and when an Admin can use Admin Release to bypass the approval workflow for urgent situations.");
        AddTrnStep(trn006, stTrnMod, 4, "Creating a New Revision",
            "How to branch a Released document into a new Draft revision, preserving the full version history, and how publishing the new revision automatically supersedes the previous one.");
        AddTrnStep(trn006, stTrnMod, 5, "Roles and Permissions",
            "Who can perform each action — submit (Engineer or Admin), approve (any named reviewer), admin-release (Admin only), retire (Admin), and delete (Admin, Draft-only) — based on system role.");

        // TRN-SYS-007 ─ Creating and Managing Jobs
        AddTrnStep(trn007, stTrnMod, 1, "Creating a Job",
            "Selecting the target process (which must be in Released status), entering the job code and name, setting the priority, and understanding how step executions are automatically generated from the process steps.");
        AddTrnStep(trn007, stTrnMod, 2, "Job Status Lifecycle",
            "Understanding the Created → In Progress → Completed / On Hold / Cancelled state machine, and the actions (Start, Put On Hold, Resume, Complete, Cancel) that trigger each transition.");
        AddTrnStep(trn007, stTrnMod, 3, "Working with Step Executions",
            "How to start, complete, and annotate individual step executions; how to record step-level notes and raise alerts; and how the job timeline builds up as steps are completed.");
        AddTrnStep(trn007, stTrnMod, 4, "Items and Job History",
            "Attaching serialised items, batches, or bulk stock to a job; recording and updating grades as items move through steps; and reviewing the full job timeline and step-by-step history after completion.");

        // TRN-SYS-008 ─ Working with Items, Batches and Grades
        AddTrnStep(trn008, stTrnMod, 1, "The Item Model",
            "What an Item represents: a trackable unit of work belonging to a specific Kind, with status and grade tracked at each point of its journey through a job.");
        AddTrnStep(trn008, stTrnMod, 2, "Kinds, Grades, and the Material Model",
            "How Kinds define a product type with its own serialisation and batch rules; how Grades define the quality levels for that Kind; and how Items are assigned a grade when inspected or processed.");
        AddTrnStep(trn008, stTrnMod, 3, "Creating Items Within a Job",
            "How to add serialised items (by serial number), batch items (by lot and quantity), and bulk stock records to an active job, and the difference between each type.");
        AddTrnStep(trn008, stTrnMod, 4, "Updating Status and Grade",
            "Progressing items through Available → In Process → Completed; recording grade changes at inspection steps; and what each item status means for downstream processing.");
        AddTrnStep(trn008, stTrnMod, 5, "Batch Operations and Scrap Workflow",
            "Performing bulk grade updates across multiple items; routing nonconforming items to scrap, rework, or supplier return; and viewing a batch's full processing history.");

        // TRN-SYS-009 ─ Raising and Managing Non-Conformances
        AddTrnStep(trn009, stTrnMod, 1, "What is a Non-Conformance?",
            "The difference between a nonconformance (an actual deviation from requirements) and a PFMEA risk entry (a potential future failure mode); when each is the appropriate record to create.");
        AddTrnStep(trn009, stTrnMod, 2, "Raising an NC",
            "Selecting the severity (Minor, Major, Critical), category, and related job or item; describing the detected problem clearly; and recording the immediate containment action taken.");
        AddTrnStep(trn009, stTrnMod, 3, "NC Status Lifecycle",
            "Moving an NC from Open to Under Review to Closed (fully resolved) or Voided (incorrectly raised); adding root-cause analysis notes and the corrective action taken to formally close the record.");
        AddTrnStep(trn009, stTrnMod, 4, "Reviewing NC History and Trends",
            "Viewing all NCs for a specific item, process, or date range; identifying repeat failure patterns from the list; and feeding trends into the corrective action and QMS management review process.");

        // TRN-SYS-010 ─ Analytics, Reports and Dashboards
        AddTrnStep(trn010, stTrnMod, 1, "Analytics Module Overview",
            "How Process Manager automatically captures operational metrics — job throughput, step cycle-time per process, NC rate — and makes them available through the Analytics module.");
        AddTrnStep(trn010, stTrnMod, 2, "Using the Analytics Page",
            "Reading throughput bar charts and step cycle-time trend lines; applying date range, process, and status filters; and exporting underlying data for further analysis.");
        AddTrnStep(trn010, stTrnMod, 3, "Standard Reports",
            "Running and exporting the built-in reports from the Reports section: job summary, process timing (average duration per step), and competency status across the workforce.");
        AddTrnStep(trn010, stTrnMod, 4, "Power BI Integration",
            "How to register your Power BI workspace in the Admin section, connect to the Process Manager API as a data source, and build custom dashboards using live operational data.");

        // TRN-SYS-011 ─ Training Catalogue and Competency Records
        AddTrnStep(trn011, stTrnMod, 1, "The Training Catalogue",
            "How to browse available training courses, view course descriptions and learning objectives, and understand the competency requirements and expiry policy for each course.");
        AddTrnStep(trn011, stTrnMod, 2, "Launching a Training Course",
            "How starting a training course from the Catalogue creates a Job of type Training; completing each step in that job demonstrates the learning; and completing the job generates a Competency Record for the learner.");
        AddTrnStep(trn011, stTrnMod, 3, "Competency Status",
            "Understanding Current (valid), Expiring Soon (within 30 days of the expiry date), and Expired statuses; how the CompetencyExpiryDays setting on the course drives the expiry date on each competency record.");
        AddTrnStep(trn011, stTrnMod, 4, "The Competency Matrix",
            "How Admins and Engineers use the colour-coded Competency Matrix grid to see every user's status across all active training courses at a glance, and how to use it to plan re-training priorities.");

        // TRN-SYS-012 ─ User Administration and Role Management
        AddTrnStep(trn012, stTrnMod, 1, "The Two Built-In Roles",
            "Admin (full access including user management, admin-release of documents, and all configuration) vs Engineer (design and authoring access: processes, documents, step templates, and training courses, but no user management or admin-release) vs standard user (operational access only).");
        AddTrnStep(trn012, stTrnMod, 2, "Creating and Managing User Accounts",
            "How to create a new user account from the Admin section, assign or change roles, reset a forgotten password, and deactivate an account when an employee leaves.");
        AddTrnStep(trn012, stTrnMod, 3, "Domain Vocabulary Configuration",
            "How to customise terminology per-process from the Config section so that Process Manager uses the language your team already knows — for example 'Work Order' instead of 'Job', or 'Board' instead of 'Item'.");
        AddTrnStep(trn012, stTrnMod, 4, "Best Practices for Role Assignment",
            "The principle of least privilege applied to Process Manager roles; guidance on Engineer vs Admin delegation; and when to use admin-release versus the standard document approval workflow.");

        db.Processes.AddRange(
            trn001, trn002, trn003, trn004, trn005, trn006,
            trn007, trn008, trn009, trn010, trn011, trn012);

        await db.SaveChangesAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DateTime Utc(int daysOffset) =>
        DateTime.UtcNow.AddDays(daysOffset);

    private static StepExecution MakeStepExecution(
        Guid jobId, ProcessStep step, int seq,
        StepExecutionStatus status, DateTime? startedAt, DateTime? completedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = startedAt ?? DateTime.UtcNow,
            UpdatedAt = completedAt ?? startedAt ?? DateTime.UtcNow,
            JobId = jobId,
            ProcessStepId = step.Id,
            Sequence = seq,
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };

    private static void AddCompletedStepExecutions(
        Job job, ProcessStep[] steps, DateTime jobStart, DateTime jobEnd)
    {
        // Spread step start/end evenly across job duration
        var totalMinutes = (jobEnd - jobStart).TotalMinutes;
        var stepDuration = totalMinutes / steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            var stepStart = jobStart.AddMinutes(i * stepDuration);
            var stepEnd   = jobStart.AddMinutes((i + 1) * stepDuration);
            job.StepExecutions.Add(MakeStepExecution(
                job.Id, steps[i], i + 1,
                StepExecutionStatus.Completed, stepStart, stepEnd));
        }
    }
}
