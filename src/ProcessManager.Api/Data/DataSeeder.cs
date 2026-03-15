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
