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
