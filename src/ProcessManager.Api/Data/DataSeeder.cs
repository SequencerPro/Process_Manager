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
            TermProcess = "Process", TermStep = "Operation",
            TermWorkorder = "Work Order"
        };
        var vocabElec = new DomainVocabulary
        {
            Id = Guid.NewGuid(), CreatedAt = Utc(-90), UpdatedAt = Utc(-90),
            Name = "Electronics Production",
            TermKind = "Component Type", TermKindCode = "Part No.",
            TermGrade = "Quality Grade", TermItem = "Board", TermItemId = "Board S/N",
            TermBatch = "Panel", TermBatchId = "Panel ID",
            TermJob = "Production Order", TermWorkflow = "Build Plan",
            TermProcess = "Build Process", TermStep = "Station",
            TermWorkorder = "Production Order"
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
        // ── Step Template Content (using SeederStepBuilder) ──────────────────
        // INSP-01 — Incoming Inspection
        {
            stIncomingInsp.Contents.Add(SeederStepBuilder.Setup(stIncomingInsp, 0,
                "1.  Retrieve the delivery paperwork (packing slip, certificate of conformance) from the goods-received area.\n" +
                "2.  Locate the corresponding purchase order in the system and confirm part numbers and quantities match.\n" +
                "3.  Place the received items in the incoming inspection hold area before beginning physical checks."));
            stIncomingInsp.Contents.Add(SeederStepBuilder.Safety(stIncomingInsp, 20,
                "CAUTION: Do not move items from the inspection hold area to WIP storage until this step is marked Complete. " +
                "Using unverified incoming material is a nonconformance under QMS-012."));
            stIncomingInsp.Contents.Add(SeederStepBuilder.Reference(stIncomingInsp, 30,
                "[Refer to the applicable incoming inspection checklist for this part number. " +
                "Drawing tolerances and acceptance criteria are defined in the part drawing package.]"));

            var dimPrompt = SeederStepBuilder.Numeric(stIncomingInsp, 40,
                "Measured dimension vs. nominal (record deviation in mm)", units: "mm",
                min: -0.5m, max: 0.5m, nominal: 0m, hardLimit: true);
            stIncomingInsp.Contents.Add(dimPrompt);

            stIncomingInsp.Contents.Add(SeederStepBuilder.PassFail(stIncomingInsp, 41,
                "Surface finish acceptable (no scratches, corrosion, or contamination)", hardLimit: false));
            stIncomingInsp.Contents.Add(SeederStepBuilder.PassFail(stIncomingInsp, 42,
                "Certificate of conformance present and matches delivery", hardLimit: true));
            stIncomingInsp.Contents.Add(SeederStepBuilder.Checkbox(stIncomingInsp, 43,
                "Quantity counted and confirmed against purchase order",
                ContentCategory.Inspection));
            stIncomingInsp.Contents.Add(SeederStepBuilder.Note(stIncomingInsp, 80,
                "Record any deviations on the Non-Conformance Log. Items failing hard-limit checks " +
                "must be quarantined and escalated to the Quality Engineer before any disposition decision."));

            stIncomingInsp.RunChartWidgets.Add(SeederStepBuilder.Chart(
                stIncomingInsp, dimPrompt, "Incoming dimension deviation trend"));
        }

        // MACH-01 — CNC Machining
        {
            stCncMach.Contents.Add(SeederStepBuilder.Setup(stCncMach, 0,
                "1.  Retrieve the job traveller and confirm the correct program number is loaded on the CNC controller.\n" +
                "2.  Select and mount tooling per the tool list on the job traveller. Verify tool offsets are set.\n" +
                "3.  Load the workpiece into the fixture; confirm the datum face seats flush against all three locating pins.\n" +
                "4.  Zero the part using the edge-finder or touch probe per the setup sheet.\n" +
                "5.  Run a dry cycle (feedrate override 0%) before the first production cut to verify clearances."));
            stCncMach.Contents.Add(SeederStepBuilder.Safety(stCncMach, 20,
                "Do not reach into the machining envelope while the spindle is energised or the program is active. " +
                "Confirm E-stop is accessible before starting. PPE required: safety glasses, steel-cap footwear. " +
                "Do not leave the machine unattended during the first article cut."));
            stCncMach.Contents.Add(SeederStepBuilder.Reference(stCncMach, 30,
                "[Figure 1 — Fixture loading diagram: datum faces A, B, C and clamping sequence]\n" +
                "[Refer to drawing WDG-100-DIM for dimensional tolerance zones and surface finish callouts]"));

            var depthPrompt = SeederStepBuilder.Numeric(stCncMach, 40,
                "Bored depth (measured with depth micrometer)", units: "mm",
                min: 24.8m, max: 25.2m, nominal: 25.0m, hardLimit: true);
            stCncMach.Contents.Add(depthPrompt);

            var diamPrompt = SeederStepBuilder.Numeric(stCncMach, 41,
                "Outer diameter (measured with digital calliper)", units: "mm",
                min: 49.95m, max: 50.05m, nominal: 50.0m, hardLimit: true);
            stCncMach.Contents.Add(diamPrompt);

            stCncMach.Contents.Add(SeederStepBuilder.PassFail(stCncMach, 42,
                "Surface finish Ra ≤ 1.6 µm — confirm with surface comparator", hardLimit: false));
            stCncMach.Contents.Add(SeederStepBuilder.Checkbox(stCncMach, 43,
                "Burrs removed and chamfers applied per drawing callout",
                ContentCategory.Inspection));
            stCncMach.Contents.Add(SeederStepBuilder.Note(stCncMach, 80,
                "First-article checks (first unit of each job) require a full balloon inspection against the drawing. " +
                "Record the inspection results in the job traveller."));

            stCncMach.RunChartWidgets.Add(SeederStepBuilder.Chart(stCncMach, depthPrompt, "Bored depth trend"));
            stCncMach.RunChartWidgets.Add(SeederStepBuilder.Chart(stCncMach, diamPrompt, "Outer diameter trend"));
        }

        // ASSY-01 — Sub-Assembly
        {
            stSubAssembly.Contents.Add(SeederStepBuilder.Setup(stSubAssembly, 0,
                "1.  Pull all components listed on the BOM from the WIP store and verify quantities and revision levels.\n" +
                "2.  Confirm all required torque tools and fixtures are calibrated and available at the workstation.\n" +
                "3.  Stage the base assembly in the assembly fixture; verify the assembly is secure before beginning."));
            stSubAssembly.Contents.Add(SeederStepBuilder.Safety(stSubAssembly, 20,
                "When applying torque to fasteners, ensure the opposite face of the assembly is supported to prevent loading the PCB or housing. " +
                "Report any cracked housings or stripped threads to the Engineer immediately — do not ship."));
            stSubAssembly.Contents.Add(SeederStepBuilder.Reference(stSubAssembly, 30,
                "[Figure 1 — Exploded assembly diagram: component identification and fastener locations]\n" +
                "[Refer to the assembly BOM for part numbers and revision-controlled quantities]"));

            var torquePrompt = SeederStepBuilder.Numeric(stSubAssembly, 40,
                "Fastener torque (all locations)", units: "Nm",
                min: 2.2m, max: 2.8m, nominal: 2.5m, hardLimit: true);
            stSubAssembly.Contents.Add(torquePrompt);

            stSubAssembly.Contents.Add(SeederStepBuilder.Checkbox(stSubAssembly, 41,
                "All fastener locations populated — none missing or cross-threaded",
                ContentCategory.Inspection));
            stSubAssembly.Contents.Add(SeederStepBuilder.PassFail(stSubAssembly, 42,
                "Connector seating confirmed — all connectors fully latched and retention tabs engaged", hardLimit: false));
            stSubAssembly.Contents.Add(SeederStepBuilder.PassFail(stSubAssembly, 43,
                "Component orientations visually verified against assembly diagram", hardLimit: false));
            stSubAssembly.Contents.Add(SeederStepBuilder.Note(stSubAssembly, 80,
                "Any BOM discrepancy (wrong revision, unexpected substitute) must be raised as a non-conformance " +
                "before assembly proceeds. Do not substitute components without engineer approval."));

            stSubAssembly.RunChartWidgets.Add(SeederStepBuilder.Chart(stSubAssembly, torquePrompt, "Assembly torque trend"));
        }

        // TEST-01 — Functional Test
        {
            stFuncTest.Contents.Add(SeederStepBuilder.Setup(stFuncTest, 0,
                "1.  Connect the assembly to the functional test fixture using the correct cable harness for this product variant.\n" +
                "2.  Confirm the test software version matches the variant code on the job traveller.\n" +
                "3.  Set the power supply to the required test voltage before applying power to the UUT."));
            stFuncTest.Contents.Add(SeederStepBuilder.Safety(stFuncTest, 20,
                "ESD precautions required: wrist strap connected, ESD mat in place, no bare-hand contact with PCB surfaces. " +
                "Do not connect the UUT to the test fixture until the power supply output is verified at 0 V."));
            stFuncTest.Contents.Add(SeederStepBuilder.Reference(stFuncTest, 30,
                "[Refer to the Functional Test Specification document for pass/fail limits for each test point]\n" +
                "[Figure 1 — Test fixture connection diagram and cable harness routing]"));

            var supplyPrompt = SeederStepBuilder.Numeric(stFuncTest, 40,
                "Supply current at nominal voltage (Vcc = 5 V)", units: "mA",
                min: 180m, max: 240m, nominal: 210m, hardLimit: true);
            stFuncTest.Contents.Add(supplyPrompt);

            var outputPrompt = SeederStepBuilder.Numeric(stFuncTest, 41,
                "Output voltage at full load", units: "V",
                min: 4.85m, max: 5.15m, nominal: 5.0m, hardLimit: true);
            stFuncTest.Contents.Add(outputPrompt);

            stFuncTest.Contents.Add(SeederStepBuilder.PassFail(stFuncTest, 42,
                "All self-test LEDs illuminate in correct sequence at power-on", hardLimit: false));
            stFuncTest.Contents.Add(SeederStepBuilder.PassFail(stFuncTest, 43,
                "Communication interface responds to loopback test command", hardLimit: true));
            stFuncTest.Contents.Add(SeederStepBuilder.Choice(stFuncTest, 44,
                "Overall functional test result",
                new[] { "Pass — all limits met", "Fail — one or more limits exceeded", "Inconclusive — test aborted" }));
            stFuncTest.Contents.Add(SeederStepBuilder.Note(stFuncTest, 80,
                "A Fail result on any hard-limit prompt automatically generates a non-conformance record. " +
                "Do not retest without first investigating and documenting the failure mode."));

            stFuncTest.RunChartWidgets.Add(SeederStepBuilder.Chart(stFuncTest, supplyPrompt, "Supply current trend"));
            stFuncTest.RunChartWidgets.Add(SeederStepBuilder.Chart(stFuncTest, outputPrompt, "Output voltage trend"));
        }

        // INSP-02 — Visual Inspection
        {
            stVisualInsp.Contents.Add(SeederStepBuilder.Setup(stVisualInsp, 0,
                "1.  Place the item on the inspection bench under the overhead inspection lamp (minimum 1000 lux).\n" +
                "2.  Rotate the item through 360° to inspect all exterior faces before beginning the formal checklist.\n" +
                "3.  Use a 5× loupe for any area where fine-detail workmanship must be assessed."));
            stVisualInsp.Contents.Add(SeederStepBuilder.Reference(stVisualInsp, 30,
                "[Refer to the visual workmanship standard (IPC-A-610 or company equivalent) for defect classification]\n" +
                "[Figure 1 — Defect reference chart: acceptable vs. rejectable conditions for this product class]"));

            stVisualInsp.Contents.Add(SeederStepBuilder.PassFail(stVisualInsp, 40,
                "No scratches, dents, or gouges exceeding the workmanship standard", hardLimit: false));
            stVisualInsp.Contents.Add(SeederStepBuilder.PassFail(stVisualInsp, 41,
                "Marking and labelling correct, legible, and properly positioned", hardLimit: true));
            stVisualInsp.Contents.Add(SeederStepBuilder.PassFail(stVisualInsp, 42,
                "No contamination (grease, flux residue, metal swarf, moisture)", hardLimit: false));
            stVisualInsp.Contents.Add(SeederStepBuilder.PassFail(stVisualInsp, 43,
                "No visible cracks in housing, moulding, or weld zone", hardLimit: true));
            stVisualInsp.Contents.Add(SeederStepBuilder.Choice(stVisualInsp, 44,
                "Overall cosmetic grade",
                new[] { "Grade A — no visible defects", "Grade B — minor cosmetic only, fully functional", "Reject — functional or safety concern" }));
            stVisualInsp.Contents.Add(SeederStepBuilder.Note(stVisualInsp, 80,
                "Grade B items must be recorded on the traveller and are subject to customer notification rules " +
                "defined in QMS-009. Grade B does not require a non-conformance record unless the customer's " +
                "contract specifies Grade A only."));
        }

        // PACK-01 — Packaging
        {
            stPackaging.Contents.Add(SeederStepBuilder.Setup(stPackaging, 0,
                "1.  Retrieve the correct packaging materials from stores per the packaging specification for this part.\n" +
                "2.  Check that the bag size and anti-static type (if required) match the part number's packaging BOM.\n" +
                "3.  Print the product label and serialised shipping label from the system before beginning."));
            stPackaging.Contents.Add(SeederStepBuilder.Safety(stPackaging, 20,
                "Handle completed assemblies only with clean gloves. Cosmetic defects introduced during packaging " +
                "that were not present at visual inspection are a workmanship nonconformance."));
            stPackaging.Contents.Add(SeederStepBuilder.Reference(stPackaging, 30,
                "[Refer to the packaging specification drawing for this part number: bag type, cushioning type, " +
                "label positions, and carton arrangement]"));

            stPackaging.Contents.Add(SeederStepBuilder.Checkbox(stPackaging, 40,
                "Item cleaned (lint-free cloth) and free of fingerprints before bagging",
                ContentCategory.Inspection));
            stPackaging.Contents.Add(SeederStepBuilder.PassFail(stPackaging, 41,
                "Product label applied: correct part number, revision, serial number, and barcode", hardLimit: true));
            stPackaging.Contents.Add(SeederStepBuilder.PassFail(stPackaging, 42,
                "Bag sealed and desiccant included (if required by spec)", hardLimit: false));
            stPackaging.Contents.Add(SeederStepBuilder.PassFail(stPackaging, 43,
                "Shipping label applied to outer carton: correct address, tracking number, and hazmat markings if applicable",
                hardLimit: true));
            stPackaging.Contents.Add(SeederStepBuilder.Scan(stPackaging, 44,
                "Scan or enter shipping tracking number"));
            stPackaging.Contents.Add(SeederStepBuilder.Note(stPackaging, 80,
                "Do not release cartons to the shipping area until all items in the job have completed this step. " +
                "The job must be marked Complete before the dispatch note is printed."));
        }

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
                EffectiveDate = effectiveDaysAgo.HasValue ? Utc(-effectiveDaysAgo.Value) : null,
                IsSystemContent = true
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
        var stDocSect = db.StepTemplates.FirstOrDefault(t => t.Code == "DOC-SECT-01")
            ?? new StepTemplate
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-400), UpdatedAt = Utc(-400),
                Code = "DOC-SECT-01", Name = "Document Section",
                Description = "A numbered section within a controlled QMS procedure document.",
                Pattern = StepPattern.General, IsActive = true, IsShared = true,
                Status = ProcessStatus.Released, Version = 1,
                IsSystemContent = true
            };
        if (db.Entry(stDocSect).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
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

        // ── QMS-005 — full rich content blocks (ISO 9001:2015 clause 6.1) ────
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms005.CreatedAt, UpdatedAt = qms005.CreatedAt,
                ProcessId = qms005.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for risk and opportunity management per ISO 9001:2015 clause 6.1."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure establishes a systematic and repeatable approach for identifying, assessing, " +
                "and treating risks and opportunities that are relevant to the organisation's Quality Management " +
                "System. Its purpose is to:\n\n" +
                "  a)  Give assurance that the QMS can achieve its intended outcomes\n" +
                "  b)  Enhance desirable effects and prevent or reduce undesired effects\n" +
                "  c)  Achieve improvement in the QMS and in product and service quality\n\n" +
                "Risk-based thinking is fundamental to ISO 9001:2015 and replaces the formal preventive action " +
                "requirement of earlier revisions. Rather than reacting to problems, this procedure drives " +
                "proactive consideration of what could go wrong — and what beneficial outcomes could be " +
                "captured — before they occur."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Scope\n\n" +
                "This procedure applies to all processes, functions, and activities falling within the QMS " +
                "scope defined in QMS-001. It covers:\n\n" +
                "  • Strategic-level risks and opportunities identified through the context analysis (clause 4.1)\n" +
                "  • Process-level risks arising from the QMS process architecture described in QMS-004\n" +
                "  • Operational risks identified by process owners in the course of day-to-day management\n\n" +
                "It does not replace the discipline-specific risk tools used in product design (DFMEA), " +
                "process design (PFMEA/Control Plans), or health and safety management, which are governed " +
                "by their respective procedures. Those findings are, however, fed into the QMS risk register " +
                "when they represent a significant risk to QMS intended outcomes."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Normative reference\n\n" +
                "ISO 9001:2015, clause 6.1 — Actions to address risks and opportunities\n\n" +
                "Clause 6.1 requires that when planning for the QMS the organisation shall consider the issues " +
                "referred to in clause 4.1 and the requirements referred to in clause 4.2 and determine the " +
                "risks and opportunities that need to be addressed to:\n\n" +
                "  a)  Give assurance that the QMS can achieve its intended results\n" +
                "  b)  Enhance desirable effects\n" +
                "  c)  Prevent, or reduce, undesired effects\n" +
                "  d)  Achieve improvement\n\n" +
                "Clause 6.1.2 requires that the organisation shall plan actions to address those risks and " +
                "opportunities; integrate and implement the actions into its QMS processes; and evaluate the " +
                "effectiveness of those actions. Actions shall be proportionate to the potential impact on " +
                "the conformity of products and services."));
            ps1.Contents.Add(Blk(ps1, 3,
                "1.4  Relationship to planning and management review\n\n" +
                "The risk and opportunity register is a live document. It feeds directly into:\n\n" +
                "  • QMS objectives setting (QMS-003) — objectives shall address significant risks\n" +
                "  • Management review inputs (QMS-019) — the register and a summary of treatment effectiveness " +
                "are standing agenda items at every management review\n" +
                "  • Internal audit planning (QMS-018) — high-residual-risk areas receive priority in the audit " +
                "schedule\n" +
                "  • Corrective action (QMS-020) — where risk materialises, a corrective action is raised\n\n" +
                "The interested parties register (QMS-001 annex) identifies the requirements of relevant " +
                "interested parties and is a primary source for opportunity identification."));
            qms005.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms005.CreatedAt, UpdatedAt = qms005.CreatedAt,
                ProcessId = qms005.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability matrix for the risk and opportunity management process."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Top Management\n\n" +
                "  • Approves the risk and opportunity management framework and the scoring criteria documented in this procedure\n" +
                "  • Reviews the risk register and determines whether treatment plans for significant risks are adequate at each management review\n" +
                "  • Allocates resources required for the implementation of risk treatment actions\n" +
                "  • Ensures that quality objectives are aligned with the findings of the risk assessment"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Quality Manager\n\n" +
                "  • Owns and maintains the risk and opportunity register in Process Manager\n" +
                "  • Coordinates the scheduled risk review cycle and ad-hoc assessments triggered by context changes\n" +
                "  • Facilitates risk identification workshops with process owners\n" +
                "  • Reviews treatment action progress and escalates overdue or ineffective actions to top management\n" +
                "  • Presents the register summary at each management review meeting\n" +
                "  • Maintains this procedure and initiates revision when the framework or criteria change"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Process Owners\n\n" +
                "  • Identify risks and opportunities within their own processes and report them to the Quality Manager\n" +
                "  • Own and implement treatment actions assigned to risks in their process area\n" +
                "  • Report treatment progress to the Quality Manager at each scheduled review\n" +
                "  • Notify the Quality Manager promptly when a change within their process area introduces a new risk " +
                "or changes the likelihood or consequence of an existing one\n" +
                "  • Participate in the periodic risk review as required"));
            ps2.Contents.Add(Blk(ps2, 3,
                "All Employees\n\n" +
                "  • Report potential risks, near-misses, and improvement opportunities through the nonconformance " +
                "and improvement reporting channels (QMS-016, QMS-020)\n" +
                "  • Implement risk control measures defined for their work activities\n" +
                "  • Raise concerns directly with their line manager or the Quality Manager if they believe a " +
                "control measure is inadequate or has broken down"));
            qms005.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms005.CreatedAt, UpdatedAt = qms005.CreatedAt,
                ProcessId = qms005.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Step-by-step procedure for identifying, assessing, treating, and monitoring risks and opportunities."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Context review and risk identification\n\n" +
                "At the start of each risk review cycle (minimum annually, or following a significant change " +
                "in organisational context), the Quality Manager reviews the outputs of the context analysis " +
                "(clause 4.1 — internal and external issues) and the interested parties register (clause 4.2 — " +
                "requirements of relevant interested parties). These are the primary inputs to risk identification.\n\n" +
                "For each QMS process the process owner is asked to identify:\n" +
                "  a)  Risks — events or conditions that, if they occurred, would adversely affect the ability " +
                "of the process to deliver its intended output or to conform to requirements\n" +
                "  b)  Opportunities — conditions that, if exploited, would bring about enhancement of quality " +
                "performance, customer satisfaction, or QMS effectiveness\n\n" +
                "Additional sources for identification include: customer complaints and satisfaction data " +
                "(QMS-017), audit findings (QMS-018), corrective action history (QMS-020), PFMEA outputs, " +
                "supplier performance data (QMS-012), and changes in legal or regulatory requirements."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Risk assessment\n\n" +
                "Each identified risk is assessed using a 5×5 likelihood-consequence matrix:\n\n" +
                "  Likelihood score (L):\n" +
                "    1 = Rare          (less than once in 5 years)\n" +
                "    2 = Unlikely      (once every 2–5 years)\n" +
                "    3 = Possible      (once per year)\n" +
                "    4 = Likely        (several times per year)\n" +
                "    5 = Almost certain (monthly or more frequently)\n\n" +
                "  Consequence score (C):\n" +
                "    1 = Negligible    (no customer impact; contained internally)\n" +
                "    2 = Minor         (minor customer inconvenience; no nonconformance)\n" +
                "    3 = Moderate      (customer nonconformance; rework required)\n" +
                "    4 = Significant   (customer complaint, delivery failure, or cost impact > £10 k)\n" +
                "    5 = Severe        (loss of customer, regulatory action, or certification risk)\n\n" +
                "  Risk Priority Number (RPN) = L × C\n\n" +
                "  Risk bands:\n" +
                "    1–4   = Low     — Accept; monitor at annual review\n" +
                "    5–9   = Medium  — Treat; assign owner and action within 90 days\n" +
                "    10–14 = High    — Treat urgently; present to management within 30 days\n" +
                "    15–25 = Critical — Escalate to top management immediately; treatment mandatory before next delivery cycle\n\n" +
                "Opportunity attractiveness is scored on a single combined scale of 1 (minor benefit) " +
                "to 5 (transformational benefit) and is recorded alongside risks in the register."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Determine treatment\n\n" +
                "For each risk, the process owner and Quality Manager agree a treatment option:\n\n" +
                "  Avoid    — Eliminate the risk by changing or discontinuing the activity that gives rise to it\n" +
                "  Reduce   — Take action to lower the likelihood score, the consequence score, or both\n" +
                "  Transfer — Share the risk contractually or through insurance (e.g., supplier quality agreements, " +
                "extended warranty terms)\n" +
                "  Accept   — Acknowledge and monitor without further action, where the cost of treatment " +
                "exceeds the benefit and the RPN is Low\n\n" +
                "For opportunities, treatment options are:\n" +
                "  Exploit  — Take positive action to ensure the opportunity is realised\n" +
                "  Enhance  — Increase the likelihood or magnitude of the beneficial outcome\n" +
                "  Share    — Partner with another party to jointly exploit the opportunity\n" +
                "  Ignore   — Accept that pursuing the opportunity is not feasible at this time\n\n" +
                "Each treatment for a risk rated Medium or above must have:\n" +
                "  • A named action owner\n" +
                "  • A target completion date\n" +
                "  • A description of the action and the expected residual RPN after implementation\n\n" +
                "Treatment actions are tracked in the risk register action column and, where they are " +
                "tied to QMS objectives, in the objectives tracker (QMS-003)."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Implement and verify treatment actions\n\n" +
                "Action owners implement their assigned treatments within the agreed target dates. The " +
                "Quality Manager tracks progress and follows up overdue actions at each monthly quality " +
                "meeting. On completion of a treatment action:\n\n" +
                "  a)  The action owner confirms completion to the Quality Manager\n" +
                "  b)  The Quality Manager re-scores the risk using the same L×C matrix to derive the " +
                "residual RPN and records this in the register\n" +
                "  c)  If the residual RPN remains High or Critical, a further treatment action is required\n" +
                "  d)  If the residual RPN is Low or Medium, the risk is moved to the Monitored status " +
                "and reviewed at the next scheduled review cycle\n\n" +
                "Where a risk treatment action fails to achieve the expected reduction in RPN, a corrective " +
                "action is raised under QMS-020 to investigate and address the root cause of the ineffectiveness."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Step 5 — Monitor and review\n\n" +
                "The risk register is a standing agenda item at every management review (QMS-019). At each " +
                "review the Quality Manager presents:\n\n" +
                "  • New risks added since the last review and their initial RPN\n" +
                "  • Status of all open treatment actions (on track / overdue / complete)\n" +
                "  • Risks where residual RPN has changed since the last review\n" +
                "  • Any risks that materialised during the period and the resulting corrective actions\n" +
                "  • Summary of opportunities pursued and benefits realised\n\n" +
                "In addition to the management review cycle, the register is updated immediately when:\n" +
                "  • A significant change in organisational context, customer base, or regulation is identified\n" +
                "  • A major nonconformance, customer complaint, or audit finding reveals a previously " +
                "unrecognised risk\n" +
                "  • A new process, product, service, or supplier is introduced to the QMS scope\n" +
                "  • A treatment action is closed and a residual RPN is confirmed"));
            qms005.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms005.CreatedAt, UpdatedAt = qms005.CreatedAt,
                ProcessId = qms005.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • Risk and opportunity register — the master register maintained in Process Manager; " +
                "lists all identified risks and opportunities, their current scores, treatment options, " +
                "action owners, target dates, and residual RPNs\n" +
                "  • Risk assessment records — the scoring rationale for each risk (L, C, and RPN) preserved " +
                "as a snapshot at each scheduled review, to provide an auditable history of how the risk profile " +
                "has evolved over time\n" +
                "  • Treatment action records — evidence that assigned treatment actions were implemented " +
                "(e.g., updated work instructions, training records, supplier agreements, process change records)\n" +
                "  • Management review minutes — must include the risk register summary presentation and " +
                "management decisions on treatment; see QMS-019\n" +
                "  • Corrective action records — raised under QMS-020 where a risk materialised or a " +
                "treatment action proved ineffective"));
            ps4.Contents.Add(Blk(ps4, 1,
                "Retention periods\n\n" +
                "The risk register shall be retained for the duration of the organisation's ISO 9001 " +
                "certification plus a minimum of three years. Individual risk assessment snapshots and " +
                "treatment action records shall be retained for a minimum of five years, or longer where " +
                "required by customer contract or regulatory obligation. Records must be available to " +
                "internal and external auditors on request."));
            qms005.ProcessSteps.Add(ps4);
        }

        // ── QMS-006 — full rich content blocks (ISO 9001:2015 clause 7.5) ────
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms006.CreatedAt, UpdatedAt = qms006.CreatedAt,
                ProcessId = qms006.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for control of documented information per ISO 9001:2015 clause 7.5."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure establishes the requirements and controls for all documented information " +
                "that is created, used, and maintained as part of the organisation's Quality Management " +
                "System. Its purpose is to:\n\n" +
                "  a)  Ensure that documented information is available, in the right format and media, " +
                "where and when it is needed\n" +
                "  b)  Protect documented information from loss of confidentiality, improper use, and loss " +
                "of integrity\n" +
                "  c)  Prevent the use of obsolete documents in operational processes\n" +
                "  d)  Provide an auditable record of what information was current at any given point in time\n\n" +
                "ISO 9001:2015 uses the term 'documented information' to encompass what earlier revisions " +
                "called 'documents' (maintained information) and 'records' (retained information). This " +
                "procedure addresses both categories under a unified control framework."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Scope\n\n" +
                "This procedure applies to all documented information falling within the scope of the QMS " +
                "defined in QMS-001, including:\n\n" +
                "  • Documented information required by ISO 9001:2015 (e.g., quality policy, quality " +
                "objectives, scope statement, required procedures, records of conformity)\n" +
                "  • Documented information determined by the organisation to be necessary for the " +
                "effectiveness of the QMS (e.g., work instructions, forms, specifications, drawings)\n" +
                "  • Documented information of external origin that the organisation determines is " +
                "necessary for the planning and operation of the QMS (e.g., customer specifications, " +
                "normative standards, statutory and regulatory documents)\n\n" +
                "It does not govern the management of general business records outside the QMS scope, " +
                "which are managed under the organisation's general records management policy."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Normative reference\n\n" +
                "ISO 9001:2015, clause 7.5 — Documented information\n\n" +
                "Clause 7.5.1 requires the QMS to include documented information required by the standard " +
                "and documented information determined as necessary for the effectiveness of the QMS.\n\n" +
                "Clause 7.5.2 requires that when creating and updating documented information the organisation " +
                "ensures appropriate identification and description, format, and review and approval for " +
                "suitability and adequacy.\n\n" +
                "Clause 7.5.3 requires that documented information required by the QMS and by the standard " +
                "shall be controlled to ensure it is available, suitable for use, protected, distributed, " +
                "stored, preserved, controlled for changes, retained, and disposed of appropriately.\n\n" +
                "Clause 7.5.3 further requires that documented information of external origin determined to " +
                "be necessary for the planning and operation of the QMS shall be identified as appropriate " +
                "and its distribution controlled."));
            ps1.Contents.Add(Blk(ps1, 3,
                "1.4  Document categories and the Document Library\n\n" +
                "All QMS documents are held in the Process Manager Document Library. Documents are categorised " +
                "as follows:\n\n" +
                "  Tier 1 — Quality Manual (QMS-004): the top-level description of the QMS\n" +
                "  Tier 2 — Procedures (QMS-001 to QMS-021): define what is done, by whom, and when\n" +
                "  Tier 3 — Work Instructions (WI-xxx): define in detail how specific tasks are performed\n" +
                "  Tier 4 — Forms, templates, and reference documents: the working tools of the QMS\n" +
                "  External documents: customer specifications, standards, statutory requirements\n\n" +
                "Records (retained documented information) are the completed instances of forms and " +
                "the outputs of QMS processes. They provide evidence of conformity and are subject to " +
                "the retention requirements in this procedure."));
            qms006.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms006.CreatedAt, UpdatedAt = qms006.CreatedAt,
                ProcessId = qms006.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability matrix for control of documented information."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Top Management\n\n" +
                "  • Approves the initial release and major revisions of Tier 1 and Tier 2 QMS documents\n" +
                "  • Ensures adequate resources (people, systems, storage) are provided for document management\n" +
                "  • Reviews and approves the annual document register audit summary presented at management review (QMS-019)"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Quality Manager / Document Controller\n\n" +
                "  • Maintains the master document register and ensures it is up to date at all times\n" +
                "  • Assigns document codes, titles, and revision letters/numbers to all new QMS documents\n" +
                "  • Reviews all draft documents for compliance with the document control requirements of this procedure before submission for approval\n" +
                "  • Approves minor revisions (editorial corrections, reference updates) to Tier 2–4 documents\n" +
                "  • Controls the status of documents (Draft, In Review, Released, Superseded, Obsolete)\n" +
                "  • Ensures obsolete documents are withdrawn from use and clearly identified\n" +
                "  • Manages the external document register and coordinates distribution of controlled external documents\n" +
                "  • Maintains this procedure and initiates revision when the document framework or controls change"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Process Owners and Department Managers\n\n" +
                "  • Author or commission documents within their process area using the approved templates\n" +
                "  • Review documents before submission to the Quality Manager for approval\n" +
                "  • Ensure that their teams use only current Released versions of controlled documents\n" +
                "  • Notify the Quality Manager of any obsolete printed copies or locally-saved electronic copies requiring disposal\n" +
                "  • Identify and submit external documents relevant to their processes to the Document Controller for registration"));
            ps2.Contents.Add(Blk(ps2, 3,
                "IT / Systems Administrator\n\n" +
                "  • Maintains the Process Manager system infrastructure supporting the Document Library\n" +
                "  • Implements and tests backups of the document repository on the schedule agreed with the Quality Manager\n" +
                "  • Controls user access permissions in the Document Library in accordance with the access control matrix\n" +
                "  • Notifies the Quality Manager of any system outages or data integrity issues affecting the Document Library"));
            ps2.Contents.Add(Blk(ps2, 4,
                "All Employees\n\n" +
                "  • Use only Released versions of QMS documents obtained from the Process Manager Document Library\n" +
                "  • Do not retain personal copies of controlled documents except where explicitly authorised (e.g., hard-copy controlled distribution)\n" +
                "  • Report to their line manager any discrepancy between a document version in use and the version in the Document Library"));
            qms006.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms006.CreatedAt, UpdatedAt = qms006.CreatedAt,
                ProcessId = qms006.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Step-by-step procedure for creating, approving, distributing, revising, and disposing of QMS documents."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Create a new document\n\n" +
                "  1.1  The process owner or Quality Manager identifies the need for a new QMS document " +
                "(e.g., new process, new regulatory requirement, audit finding).\n\n" +
                "  1.2  The Quality Manager assigns a unique document code using the approved coding convention:\n" +
                "       QMS-xxx for QMS procedures; WI-xxx for work instructions; FRM-xxx for forms; " +
                "EXT-xxx for external reference documents.\n\n" +
                "  1.3  The author creates the document using the current approved template for the relevant tier. " +
                "All templates are held in the Document Library under the 'Templates' category.\n\n" +
                "  1.4  The document header must include: document code, title, revision status/letter, " +
                "effective date, author name, approver name, and page numbering.\n\n" +
                "  1.5  The document is saved in the Document Library at Draft status. Draft documents are " +
                "visible to reviewers but are not available for operational use."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Review and approval\n\n" +
                "  2.1  The author submits the Draft document for review via the Document Library approval " +
                "workflow. The workflow automatically notifies the designated reviewer(s).\n\n" +
                "  2.2  Reviewer(s) assess the document for technical accuracy, completeness, and compliance " +
                "with the document control requirements of this procedure. Reviewers record their comments " +
                "within the workflow; the author resolves all comments before proceeding.\n\n" +
                "  2.3  The document is submitted to the approver (Quality Manager for Tier 2; top management " +
                "for Tier 1; process owner or Quality Manager for Tiers 3–4).\n\n" +
                "  2.4  On approval, the Document Library automatically sets the status to Released, records " +
                "the effective date, and increments or assigns the revision identifier. The previous Released " +
                "version is automatically set to Superseded.\n\n" +
                "  2.5  If the document is rejected during review or approval, the workflow returns it to " +
                "Draft status with the reviewer's or approver's comments. The author addresses the comments " +
                "and resubmits."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Distribution and access control\n\n" +
                "  3.1  On release, the Document Library notifies all users whose roles include the affected " +
                "document's process area. Notification is by system alert and, for critical procedure changes, " +
                "additionally by email from the Quality Manager.\n\n" +
                "  3.2  Where hard-copy controlled distribution is required (e.g., for use in production areas " +
                "without reliable network access), the Quality Manager prints and stamps controlled copies " +
                "'CONTROLLED COPY — [date]', records the distribution in the hard-copy distribution log, " +
                "and obtains sign-off from the recipient.\n\n" +
                "  3.3  Uncontrolled printed copies must be marked 'UNCONTROLLED COPY — VERIFY AGAINST " +
                "DOCUMENT LIBRARY BEFORE USE'. The use of uncontrolled copies is discouraged and is " +
                "permitted only with explicit authorisation from the Quality Manager.\n\n" +
                "  3.4  External documents (customer specifications, normative standards) are registered in " +
                "the external document register. The Quality Manager assigns a responsible owner for each " +
                "external document, who monitors for revisions and notifies the Quality Manager of changes " +
                "that affect the QMS."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Revise an existing document\n\n" +
                "  4.1  Any employee may request a document revision by raising a revision request through " +
                "the Document Library, describing the proposed change and reason.\n\n" +
                "  4.2  The Quality Manager assesses the request and, if approved, creates a new Draft " +
                "revision of the document. The version history section records the change description and " +
                "the names of the author and approver.\n\n" +
                "  4.3  The revision follows the same review and approval steps as a new document (Step 2).\n\n" +
                "  4.4  Minor revisions (e.g., correction of a typographical error, update of a reference " +
                "code) may be approved by the Quality Manager alone. Major revisions (substantive changes " +
                "to process, responsibility, or requirement) require the same approver as the original.\n\n" +
                "  4.5  On release of the revised document the previous revision is automatically Superseded. " +
                "Hard-copy controlled distribution holders are notified and must return the superseded copy " +
                "to the Quality Manager for secure disposal."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Step 5 — Obsolescence and disposal\n\n" +
                "  5.1  A document is made Obsolete when the process it governs is discontinued, the " +
                "document is merged into another, or it is no longer required by the QMS or the standard.\n\n" +
                "  5.2  The Quality Manager sets the document status to Obsolete in the Document Library. " +
                "Obsolete documents are retained in the library as read-only records for the defined " +
                "retention period but are clearly flagged and are not searchable in operational views.\n\n" +
                "  5.3  All hard-copy controlled distributions of the obsolete document must be recalled " +
                "and the copies shredded or otherwise securely destroyed. The disposal is recorded in the " +
                "hard-copy distribution log.\n\n" +
                "  5.4  At the end of the retention period, the Quality Manager arranges permanent deletion " +
                "or secure archiving of the document, records the disposal action, and updates the document " +
                "register accordingly."));
            qms006.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms006.CreatedAt, UpdatedAt = qms006.CreatedAt,
                ProcessId = qms006.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • Document register — the master list of all QMS documents including code, title, " +
                "revision status, effective date, process owner, approver, and retention period; " +
                "maintained in the Process Manager Document Library\n" +
                "  • Approval and review records — captured within the Document Library workflow; " +
                "records the reviewer, approver, date, and any comments or rejections for each document revision\n" +
                "  • Version history — all superseded and obsolete revisions retained in read-only form " +
                "in the Document Library for the duration of the document retention period\n" +
                "  • Hard-copy distribution log — records each controlled hard copy issued, recipient, " +
                "date of issue, and date of recall or disposal\n" +
                "  • External document register — list of all external documents in use within the QMS, " +
                "responsible owner, and date of last currency check\n" +
                "  • Access control matrix — defines which roles have create, edit, approve, and read-only " +
                "access to each document category in the Document Library\n" +
                "  • Disposal records — evidence of secure disposal of hard copies and permanently deleted " +
                "electronic records at end of retention period"));
            ps4.Contents.Add(Blk(ps4, 1,
                "Retention periods\n\n" +
                "  • Tier 1 and Tier 2 QMS documents — all revisions retained for the lifetime of the " +
                "organisation's ISO 9001 certification plus a minimum of five years after the document " +
                "is made Obsolete\n" +
                "  • Tier 3 work instructions — current revision plus the two immediately preceding " +
                "revisions retained; older revisions may be deleted after Quality Manager approval\n" +
                "  • Tier 4 forms and templates — current version retained; superseded versions retained " +
                "for one year after supersession unless a completed record exists, in which case the form " +
                "version is retained alongside the record\n" +
                "  • External documents — retained for as long as they are referenced in any current QMS " +
                "document, plus two years\n" +
                "  • Approval and review workflow records — retained for the same period as the document " +
                "to which they relate\n\n" +
                "Retention periods may be extended by customer contract or regulatory requirement. Where " +
                "a conflict exists, the longer retention period takes precedence."));
            qms006.ProcessSteps.Add(ps4);
        }

        // ── QMS-007 — full rich content blocks (ISO 9001:2015 clauses 7.2 & 7.3) ─
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms007.CreatedAt, UpdatedAt = qms007.CreatedAt,
                ProcessId = qms007.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for competence, training and awareness per ISO 9001:2015 clauses 7.2 and 7.3."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure defines how the organisation:\n\n" +
                "  a)  Determines the competence required for persons performing work under its control that " +
                "affects the performance and effectiveness of the QMS\n" +
                "  b)  Ensures those persons are competent on the basis of appropriate education, training, " +
                "or experience\n" +
                "  c)  Where applicable, takes action to acquire the necessary competence and evaluates the " +
                "effectiveness of those actions\n" +
                "  d)  Ensures that persons performing work under the organisation's control are aware of the " +
                "quality policy, relevant quality objectives, their contribution to the effectiveness of the " +
                "QMS, and the implications of not conforming to QMS requirements\n\n" +
                "Competent people are the foundation of a functioning QMS. This procedure replaces the " +
                "reactive 'send someone on a course' approach with a systematic, role-based competency " +
                "framework that drives targeted development and provides auditable evidence of capability."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Scope\n\n" +
                "This procedure applies to all persons performing work under the organisation's control " +
                "that affects QMS performance, including:\n\n" +
                "  • Permanent employees in all functions relevant to the QMS scope\n" +
                "  • Fixed-term and temporary employees performing work that affects product or service conformity\n" +
                "  • Contractors and agency workers performing tasks governed by a QMS procedure or work instruction\n" +
                "  • Suppliers performing outsourced processes on the organisation's behalf, where competence " +
                "requirements are placed on them by contract\n\n" +
                "It does not govern the general HR development activities outside the QMS scope, " +
                "though the training records system is shared."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Normative reference\n\n" +
                "ISO 9001:2015, clause 7.2 — Competence\n\n" +
                "Requires the organisation to determine the necessary competence of persons doing work " +
                "under its control that affects quality performance; ensure those persons are competent; " +
                "take actions to acquire competence where needed; evaluate the effectiveness of those " +
                "actions; and retain appropriate documented information as evidence.\n\n" +
                "ISO 9001:2015, clause 7.3 — Awareness\n\n" +
                "Requires that all persons doing work under the organisation's control are aware of: " +
                "the quality policy; relevant quality objectives; their contribution to the effectiveness " +
                "of the QMS, including the benefits of improved quality performance; and the implications " +
                "of not conforming to QMS requirements."));
            ps1.Contents.Add(Blk(ps1, 3,
                "1.4  Relationship to other procedures\n\n" +
                "  • QMS-002 (Quality Policy) — the current signed quality policy forms the basis of the " +
                "awareness briefings required by clause 7.3\n" +
                "  • QMS-003 (Quality Objectives) — relevant objectives are communicated to all employees " +
                "as part of the annual awareness programme\n" +
                "  • QMS-005 (Risk Management) — competence gaps identified in the TNA that represent a " +
                "significant risk to QMS outcomes are entered into the risk register\n" +
                "  • QMS-018 (Internal Audit) — audit findings relating to competence or awareness trigger " +
                "a training needs review for the affected area\n" +
                "  • QMS-020 (Corrective Action) — where a nonconformance root cause is attributed to a " +
                "competence or awareness failure, retraining is a standard corrective action type"));
            qms007.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms007.CreatedAt, UpdatedAt = qms007.CreatedAt,
                ProcessId = qms007.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability matrix for competence determination, training delivery, and awareness."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Top Management\n\n" +
                "  • Approves the competency framework and ensures resources are allocated for training delivery\n" +
                "  • Reviews the training needs analysis summary and training plan at management review (QMS-019)\n" +
                "  • Ensures the quality policy and relevant objectives are communicated to all persons under " +
                "the organisation's control\n" +
                "  • Sets expectations for a culture of continuous learning and quality awareness"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Human Resources Manager\n\n" +
                "  • Maintains the HR system and training records for all employees\n" +
                "  • Coordinates the annual training needs analysis process with line managers\n" +
                "  • Sources, books, and administers training courses and external qualifications\n" +
                "  • Issues training completion certificates and updates individual training records\n" +
                "  • Reports training plan completion status to the Quality Manager quarterly\n" +
                "  • Ensures new employees receive induction training before undertaking work that affects quality"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Quality Manager\n\n" +
                "  • Owns and maintains the competency framework (QMS-critical roles and required competencies)\n" +
                "  • Reviews training effectiveness evidence and confirms whether competence has been achieved\n" +
                "  • Designs and delivers quality-specific awareness sessions (policy, objectives, QMS changes)\n" +
                "  • Escalates training plans that address significant quality risks to management\n" +
                "  • Maintains this procedure and initiates revision when the framework changes"));
            ps2.Contents.Add(Blk(ps2, 3,
                "Line Managers / Process Owners\n\n" +
                "  • Identify the competence requirements for each role within their team using the competency framework\n" +
                "  • Conduct individual competence assessments against those requirements\n" +
                "  • Nominate team members for training and confirm completion and effectiveness\n" +
                "  • Ensure that personnel do not undertake quality-critical tasks without verified competence\n" +
                "  • Notify HR and the Quality Manager when role requirements change or new tasks require new competencies"));
            ps2.Contents.Add(Blk(ps2, 4,
                "Individual Employees\n\n" +
                "  • Complete assigned training within the timescales set in the training plan\n" +
                "  • Notify their line manager if they do not feel competent to perform an assigned task\n" +
                "  • Participate in competence assessments and awareness surveys honestly\n" +
                "  • Apply the skills and knowledge acquired through training in their day-to-day work"));
            qms007.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms007.CreatedAt, UpdatedAt = qms007.CreatedAt,
                ProcessId = qms007.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Steps for determining competence requirements, identifying gaps, delivering training, evaluating effectiveness, and maintaining awareness."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Define and maintain the competency framework\n\n" +
                "The Quality Manager, working with HR and process owners, maintains a competency framework " +
                "that specifies, for each QMS-critical role:\n\n" +
                "  a)  The education, qualifications, or professional registrations required (e.g., degree, " +
                "trade apprenticeship, CIPS, IOSH)\n" +
                "  b)  The technical skills and knowledge required (e.g., ability to read engineering drawings, " +
                "operate CNC equipment, conduct first article inspection)\n" +
                "  c)  The quality-specific competencies required (e.g., completion of QMS induction, " +
                "ISO 9001 awareness, internal auditor training)\n" +
                "  d)  Any regulatory or statutory competencies (e.g., fork-lift licence, COSHH awareness, " +
                "electrical competency certificates)\n\n" +
                "The framework is reviewed annually and updated whenever a role changes materially, a new " +
                "process is introduced, or a regulatory requirement is revised."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Identify competence gaps (Training Needs Analysis)\n\n" +
                "Annually (and on the appointment of a new person to a role), the line manager conducts a " +
                "Training Needs Analysis (TNA) for each person in their team:\n\n" +
                "  2.1  Compare the individual's current verified competencies (from their training record) " +
                "against the requirements of their role in the competency framework.\n\n" +
                "  2.2  Identify gaps — competencies required by the role that the individual has not yet " +
                "demonstrated.\n\n" +
                "  2.3  Classify each gap by priority:\n" +
                "       High   — required to perform current role safely or to conformance; must be closed " +
                "within 30 days\n" +
                "       Medium — required within the current role but risk is being managed (e.g., supervised " +
                "working); close within 90 days\n" +
                "       Low    — development need for career progression or contingency; include in annual plan\n\n" +
                "  2.4  Submit TNA outputs to HR and the Quality Manager for consolidation into the " +
                "organisation-wide training plan."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Plan and deliver training\n\n" +
                "HR prepares an annual training plan consolidating all identified gaps and categorising " +
                "the appropriate training method for each:\n\n" +
                "  On-the-job training (OJT) — for practical skills; delivered by a designated competent " +
                "trainer/buddy; recorded on an OJT sign-off sheet\n" +
                "  Internal classroom or e-learning — for quality policy, objectives, product knowledge, " +
                "process awareness; delivered by Quality Manager or HR\n" +
                "  External course or qualification — for specialist technical, safety, or professional " +
                "competencies; sourced from approved training providers\n" +
                "  Mentoring or coaching — for leadership, management, or complex technical development\n\n" +
                "Before a person undertakes a quality-critical task without supervision, the line manager " +
                "must confirm in the training system that the relevant competency has been verified. " +
                "Personnel working under supervised conditions must have a designated supervisor who is " +
                "personally verified as competent."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Evaluate training effectiveness\n\n" +
                "Training is not complete until its effectiveness has been evaluated. The method of " +
                "evaluation is proportionate to the significance of the competency:\n\n" +
                "  Test or assessment — for regulatory, safety-critical, or quality-critical competencies; " +
                "a pass mark of 80% or higher is required\n" +
                "  Observation — line manager or Quality Manager observes the individual performing the " +
                "task to the required standard; outcome recorded on the OJT sign-off sheet\n" +
                "  Supervisor confirmation — for lower-risk competencies; the individual's supervisor " +
                "confirms in writing that the training has been applied effectively\n" +
                "  Post-training review — line manager reviews whether quality metrics for the area have " +
                "improved following training, at 90 days after delivery\n\n" +
                "Where training is evaluated as ineffective, an alternative development approach is " +
                "agreed between the line manager, HR, and the Quality Manager. Persistent effectiveness " +
                "failures are escalated to top management."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Step 5 — Maintain awareness of quality commitments\n\n" +
                "Awareness is maintained through a structured programme, not a one-off induction event:\n\n" +
                "  Induction — all new employees and contractors complete a QMS Induction within their " +
                "first five working days. The induction covers: the quality policy and its meaning for " +
                "the individual's role; the current quality objectives; the consequences of nonconformance " +
                "for customers and the organisation; how to raise a nonconformance or improvement suggestion; " +
                "who the Quality Manager is and how to contact them.\n\n" +
                "  Annual re-briefing — the Quality Manager conducts a brief (15–30 minutes) annual " +
                "quality awareness session for all staff, covering any changes to the quality policy " +
                "or objectives, QMS performance highlights, any significant audit findings, and the " +
                "improvement plan for the coming year.\n\n" +
                "  Point-of-change communication — whenever a procedure, work instruction, or quality " +
                "objective changes materially, the Quality Manager issues a briefing note and, where " +
                "necessary, arranges targeted awareness training for affected staff.\n\n" +
                "  Visual management — quality policy posters, current quality objectives displays, " +
                "and QMS performance boards are maintained in prominent locations in all operational areas."));
            qms007.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms007.CreatedAt, UpdatedAt = qms007.CreatedAt,
                ProcessId = qms007.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • Competency framework — the role-by-role matrix of required education, skills, " +
                "qualifications, and quality competencies; maintained by the Quality Manager in the " +
                "Document Library\n" +
                "  • Training needs analysis records — annual TNA outputs for each individual; retained " +
                "in the HR training system\n" +
                "  • Training plan — the organisation-wide annual training schedule; approved by top " +
                "management and included in management review inputs\n" +
                "  • Individual training records — for each person: courses attended, dates, delivery " +
                "method, trainer or provider, outcome/pass mark, and effectiveness evaluation result; " +
                "maintained in the HR training system\n" +
                "  • OJT sign-off sheets — for on-the-job skills verification; signed by trainer and " +
                "trainee; retained as hard copy or scanned to the training record\n" +
                "  • External qualifications and certificates — copies of certificates for externally " +
                "accredited qualifications retained in the HR training system\n" +
                "  • Induction completion records — confirmation that each new employee has completed " +
                "the QMS induction; signed or acknowledged within the system\n" +
                "  • Awareness session attendance records — sign-in sheets or electronic acknowledgements " +
                "for annual briefings and point-of-change communications"));
            ps4.Contents.Add(Blk(ps4, 1,
                "Retention periods\n\n" +
                "  • Individual training records — retained for the duration of the individual's employment " +
                "plus five years after leaving, or as required by regulatory or customer obligation if longer\n" +
                "  • OJT sign-off sheets and certificates — same period as the individual training record\n" +
                "  • Training needs analysis records — retained for three years\n" +
                "  • Competency framework — all revisions retained for the lifetime of the ISO 9001 " +
                "certification plus three years\n" +
                "  • Awareness session records — retained for three years\n\n" +
                "Records must be available to internal and external auditors on request. Where a " +
                "regulatory requirement specifies a longer retention period, that period takes precedence."));
            qms007.ProcessSteps.Add(ps4);
        }

        // ── QMS-008 — full rich content blocks (ISO 9001:2015 clause 7.1.5) ────
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms008.CreatedAt, UpdatedAt = qms008.CreatedAt,
                ProcessId = qms008.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for control of monitoring and measuring resources per ISO 9001:2015 clause 7.1.5."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure establishes the requirements for identifying, calibrating, maintaining, " +
                "and controlling all monitoring and measuring resources used to provide evidence of " +
                "conformity of products and services to requirements. Its purpose is to:\n\n" +
                "  a)  Ensure that measurement results are valid and traceable to national or international " +
                "measurement standards\n" +
                "  b)  Protect measurement equipment from damage, deterioration, and unauthorised adjustment " +
                "that could invalidate calibration status\n" +
                "  c)  Provide assurance that measurements used to make conformity decisions are fit for purpose\n" +
                "  d)  Define the response when equipment is found to be out of tolerance, so that the " +
                "impact on previously measured product can be assessed and appropriate action taken\n\n" +
                "Measurement integrity is directly connected to product conformity; an uncalibrated or " +
                "out-of-tolerance instrument can cause defective product to be accepted and conforming " +
                "product to be rejected. This procedure ensures neither occurs."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Scope\n\n" +
                "This procedure applies to all monitoring and measuring equipment and devices used to:\n\n" +
                "  • Verify conformity of incoming materials, in-process product, and finished goods to " +
                "dimensional, physical, chemical, or functional requirements\n" +
                "  • Perform process monitoring that provides input to product acceptance decisions\n" +
                "  • Conduct environmental measurements required by the QMS or by customer specification " +
                "(e.g., temperature and humidity monitoring in controlled storage or cleanroom areas)\n\n" +
                "Equipment used exclusively for indicative, non-conformance-decision purposes (e.g., a " +
                "workshop clock, a general-purpose thermometer used only for process guidance) is excluded " +
                "from mandatory calibration but must be identified and clearly labelled as 'NOT FOR " +
                "ACCEPTANCE USE'. Any equipment whose scope of use changes to include conformity decisions " +
                "must be immediately added to the calibration register."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Normative reference\n\n" +
                "ISO 9001:2015, clause 7.1.5 — Monitoring and measuring resources\n\n" +
                "Clause 7.1.5.1 (General) requires the organisation to determine and provide the resources " +
                "needed to ensure valid and reliable results when monitoring or measuring is used to verify " +
                "conformity of products and services.\n\n" +
                "Clause 7.1.5.2 (Measurement traceability) requires that measuring equipment shall be:\n" +
                "  a)  Calibrated or verified, or both, at specified intervals or prior to use, against " +
                "measurement standards traceable to international or national measurement standards\n" +
                "  b)  Identified in order to determine their status\n" +
                "  c)  Safeguarded from adjustments, damage, or deterioration that would invalidate the " +
                "calibration status and subsequent measurement results\n\n" +
                "Where no international or national standard exists, the basis for calibration or " +
                "verification shall be retained as documented information."));
            ps1.Contents.Add(Blk(ps1, 3,
                "1.4  Relationship to other procedures\n\n" +
                "  • QMS-005 (Risk Management) — out-of-tolerance trends or systemic calibration failures " +
                "are raised as QMS risks\n" +
                "  • QMS-013 (Production Planning & Control) — in-process inspection requirements define " +
                "which instruments are needed for each production stage\n" +
                "  • QMS-014 (Inspection & Testing) — final inspection and test activities rely on " +
                "equipment controlled by this procedure\n" +
                "  • QMS-016 (Nonconformance Control) — product nonconformances discovered during recall " +
                "and re-evaluation following an out-of-tolerance finding are processed under QMS-016\n" +
                "  • QMS-020 (Corrective Action) — root cause analysis for repeated out-of-tolerance events " +
                "or calibration system failures is managed under QMS-020"));
            qms008.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms008.CreatedAt, UpdatedAt = qms008.CreatedAt,
                ProcessId = qms008.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability matrix for monitoring and measuring resource control."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Quality Manager\n\n" +
                "  • Owns the calibration register and ensures it is complete, current, and accessible\n" +
                "  • Approves or revises calibration intervals based on equipment history and risk\n" +
                "  • Approves the use of external calibration laboratories and maintains the approved " +
                "supplier list for calibration services\n" +
                "  • Authorises the release of equipment following a successful post-calibration check\n" +
                "  • Leads or delegates out-of-tolerance investigations and determines the scope of recall " +
                "and re-evaluation\n" +
                "  • Maintains this procedure and initiates revision when the framework or controls change"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Metrology Coordinator\n\n" +
                "  • Maintains the physical calibration register and schedules all due calibrations\n" +
                "  • Arranges calibration with approved internal or external providers within the required recall period\n" +
                "  • Applies calibration status labels to all equipment on receipt after calibration\n" +
                "  • Removes equipment from service when calibration is overdue and quarantines it pending action\n" +
                "  • Receives and files calibration certificates; flags any out-of-tolerance results immediately " +
                "to the Quality Manager\n" +
                "  • Maintains the calibration equipment store and ensures protection during storage and transit"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Production / Inspection Supervisors\n\n" +
                "  • Ensure operators check the calibration status label before using any measuring instrument\n" +
                "  • Report damaged, suspect, or overdue equipment to the Metrology Coordinator immediately\n" +
                "  • Segregate product measured by equipment subsequently found to be out of tolerance " +
                "and await instruction from the Quality Manager before releasing or processing further\n" +
                "  • Do not attempt to adjust, repair, or use equipment with a removed or expired calibration label"));
            ps2.Contents.Add(Blk(ps2, 3,
                "Operators and Inspectors\n\n" +
                "  • Verify that the calibration status label is present and current before each use\n" +
                "  • Handle measuring equipment with care; store in provided cases or rack locations\n" +
                "  • Report any suspected damage, deterioration, or performance anomaly to the supervisor immediately\n" +
                "  • Do not use equipment that displays an expired, missing, or 'OUT OF SERVICE' calibration label"));
            qms008.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms008.CreatedAt, UpdatedAt = qms008.CreatedAt,
                ProcessId = qms008.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Steps for registering, calibrating, labelling, and responding to out-of-tolerance findings for all monitoring and measuring equipment."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Register equipment\n\n" +
                "Every item of monitoring and measuring equipment used for conformity decisions must be " +
                "registered in the calibration register before first use. Registration requires:\n\n" +
                "  a)  Assign a unique equipment ID (format: CAL-YYYY-NNN)\n" +
                "  b)  Record: description, manufacturer, model, serial number, measurement range, " +
                "resolution, and accuracy specification\n" +
                "  c)  Assign the applicable calibration standard and method (internal procedure or " +
                "external laboratory scope of accreditation)\n" +
                "  d)  Set the calibration interval: typically 6 or 12 months; shorter intervals apply " +
                "for equipment used in critical applications or with a history of drift; longer intervals " +
                "may be approved by the Quality Manager where usage history supports it\n" +
                "  e)  Record the location or responsible work area\n\n" +
                "New equipment shall not be released for use on conformity-critical measurements until " +
                "an initial calibration or verification has been performed and the calibration certificate " +
                "received and filed."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Calibrate and verify\n\n" +
                "Calibration is performed by one of the following methods:\n\n" +
                "  Internal calibration — using UKAS-accredited (or equivalent national accreditation body) " +
                "reference standards held by the organisation; the internal calibration procedure defines " +
                "the method, reference standard to be used, acceptance criteria, and correction factors\n\n" +
                "  External calibration laboratory — using a UKAS-accredited (or equivalent) external " +
                "laboratory; the laboratory must be on the approved supplier list (QMS-012); calibration " +
                "certificates must quote the accreditation body, scope, uncertainty of measurement, and " +
                "traceability statement\n\n" +
                "On completion of calibration:\n" +
                "  a)  The calibration result (pass or out-of-tolerance) is recorded in the calibration register\n" +
                "  b)  A calibration certificate is received and filed (internal or external)\n" +
                "  c)  If the instrument passes, a new calibration status label (showing equipment ID, " +
                "calibration date, and next due date) is applied\n" +
                "  d)  If the instrument fails, proceed to Step 4 (out-of-tolerance response)"));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Status labelling and safeguarding\n\n" +
                "Every registered instrument shall carry one of the following status labels at all times:\n\n" +
                "  GREEN label — 'CALIBRATED': equipment is in calibration; label shows equipment ID, " +
                "calibration date, and next due date\n\n" +
                "  YELLOW label — 'LIMITED USE': equipment is calibrated but with restrictions (e.g., " +
                "one axis only, specific range); the restriction is stated on the label and in the register\n\n" +
                "  RED label — 'OUT OF SERVICE': equipment is overdue, damaged, or out-of-tolerance; " +
                "must not be used for any measurement; must be physically quarantined\n\n" +
                "Equipment is protected from damage and unauthorised adjustment by:\n" +
                "  • Storage in designated calibration equipment cases or racks when not in use\n" +
                "  • Sealed adjustment mechanisms (tamper-evident seals) where fitted\n" +
                "  • Restricted access to the calibration store (Metrology Coordinator only)\n" +
                "  • Explicit prohibition on field adjustment without Quality Manager authorisation"));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Out-of-tolerance response\n\n" +
                "When equipment is found to be out of tolerance (at scheduled calibration or at any point " +
                "during use), the following steps are taken immediately:\n\n" +
                "  4.1  The instrument is labelled OUT OF SERVICE and physically removed from the work area " +
                "to the quarantine location.\n\n" +
                "  4.2  The Metrology Coordinator notifies the Quality Manager and the supervisor(s) of all " +
                "areas where the instrument was used since the last known good calibration.\n\n" +
                "  4.3  The Quality Manager determines the scope of re-evaluation: which products were " +
                "measured using the instrument, during which date range, and which characteristics were " +
                "measured. This is assessed against the magnitude and direction of the out-of-tolerance error.\n\n" +
                "  4.4  Affected product is segregated and inspected using a verified in-tolerance instrument. " +
                "Results are recorded and product is dispositioned under QMS-016 (Nonconformance Control) " +
                "where nonconformance is confirmed.\n\n" +
                "  4.5  The out-of-tolerance event and its investigation are recorded in the calibration " +
                "register. If this is a repeat event for the same instrument or a systemic issue across " +
                "multiple instruments, a corrective action is raised under QMS-020.\n\n" +
                "  4.6  The instrument is sent for repair and re-calibration before being returned to service. " +
                "If it cannot be restored to tolerance, it is permanently decommissioned and the register updated."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Step 5 — Recall scheduling and overdue management\n\n" +
                "The Metrology Coordinator reviews the calibration register monthly and generates a recall " +
                "list for instruments due within the next 30 days. For each:\n\n" +
                "  a)  Schedule internal calibration or send to the external laboratory, allowing sufficient " +
                "lead time to return the instrument before the due date\n" +
                "  b)  If the instrument cannot be recalled within its due date (e.g., in continuous use on " +
                "a critical production line), the Quality Manager assesses the risk and either approves a " +
                "short extension (maximum 30 days, documented in the register) or arranges a substitute " +
                "instrument to allow recall\n" +
                "  c)  Instruments that are overdue without an approved extension are automatically placed " +
                "OUT OF SERVICE by the Metrology Coordinator\n\n" +
                "The calibration recall completion rate (% calibrations completed on or before due date) " +
                "is reported at each management review as a QMS performance metric."));
            qms008.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms008.CreatedAt, UpdatedAt = qms008.CreatedAt,
                ProcessId = qms008.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • Calibration register — master list of all registered monitoring and measuring equipment " +
                "with ID, description, calibration interval, last calibration date, next due date, " +
                "calibration result (pass/fail), and current status; maintained in Process Manager\n" +
                "  • Calibration certificates — internal calibration records or external laboratory " +
                "certificates for every calibration event; must include traceability statement, " +
                "uncertainty, and result for each measured parameter\n" +
                "  • Out-of-tolerance investigation records — scope of re-evaluation, affected product " +
                "list, measurement results from re-inspection, and disposition of affected product\n" +
                "  • Recall and extension records — documented approval of any calibration interval " +
                "extension, including the risk assessment and approval by the Quality Manager\n" +
                "  • Decommissioning records — record of instruments permanently removed from service, " +
                "reason, and date; retained to provide a complete equipment history\n" +
                "  • Calibration performance metric — monthly recall completion rate; included in " +
                "management review pack (QMS-019)"));
            ps4.Contents.Add(Blk(ps4, 1,
                "Retention periods\n\n" +
                "  • Calibration certificates — retained for the life of the instrument plus five years " +
                "after decommissioning, or for the period required to cover the product warranty and any " +
                "applicable statutory limitation period, whichever is longer\n" +
                "  • Out-of-tolerance investigation records — retained for ten years, or longer if the " +
                "affected product is subject to a customer or regulatory retention requirement\n" +
                "  • Calibration register history — all revisions retained for the life of the " +
                "ISO 9001 certification plus five years\n\n" +
                "The Quality Manager shall review retention requirements annually against current customer " +
                "contract terms and any changes to applicable statutory limitation periods."));
            qms008.ProcessSteps.Add(ps4);
        }

        // ── QMS-009 — full rich content blocks (ISO 9001:2015 clause 8.2.1) ────
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms009.CreatedAt, UpdatedAt = qms009.CreatedAt,
                ProcessId = qms009.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for customer communication per ISO 9001:2015 clause 8.2.1."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure establishes the channels, responsibilities, and response timescales for " +
                "all communication with customers relating to the organisation's products and services. " +
                "Its purpose is to:\n\n" +
                "  a)  Ensure customers receive accurate and timely information about products, services, " +
                "pricing, lead times, and any changes that affect their orders\n" +
                "  b)  Ensure all customer enquiries, contracts, and orders are handled consistently and " +
                "that commitments are only made after requirements have been reviewed (see QMS-010)\n" +
                "  c)  Provide a structured approach to handling customer feedback and complaints that " +
                "protects the customer relationship and drives internal improvement\n" +
                "  d)  Define the organisation's response to customer requests for contingency information " +
                "and emergency communication needs\n\n" +
                "Effective customer communication is not simply a courtesy — it is a primary input to " +
                "understanding customer requirements, monitoring customer satisfaction, and preventing " +
                "misunderstandings that lead to nonconformances and delivery failures."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Scope\n\n" +
                "This procedure applies to all communication with external customers across the following " +
                "categories defined by ISO 9001:2015 clause 8.2.1:\n\n" +
                "  a)  Providing information relating to products and services\n" +
                "  b)  Handling enquiries, contracts, or orders, including changes\n" +
                "  c)  Obtaining customer feedback relating to products and services, including customer complaints\n" +
                "  d)  Handling or controlling customer property\n" +
                "  e)  Establishing specific requirements for contingency actions, when relevant\n\n" +
                "It applies to communication through all channels: telephone, email, written correspondence, " +
                "customer portals, electronic data interchange (EDI), video conference, and on-site visits. " +
                "Internal communications between departments are out of scope, as is communication with " +
                "suppliers (governed by QMS-012)."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Normative reference\n\n" +
                "ISO 9001:2015, clause 8.2.1 — Customer communication\n\n" +
                "Requires the organisation to implement communication with customers relating to:\n" +
                "  a)  Information relating to products and services\n" +
                "  b)  Handling enquiries, contracts, or orders, including changes\n" +
                "  c)  Obtaining customer feedback relating to products and services, including customer complaints\n" +
                "  d)  Handling or controlling customer property\n" +
                "  e)  Establishing specific requirements for contingency actions, when relevant\n\n" +
                "Customer communication is also an input to clause 9.1.2 (Customer Satisfaction), which " +
                "requires the organisation to monitor customers' perceptions of the degree to which their " +
                "needs and expectations have been fulfilled."));
            ps1.Contents.Add(Blk(ps1, 3,
                "1.4  Communication channels and response-time standards\n\n" +
                "The following channels are authorised for customer communication and their response-time " +
                "standards are:\n\n" +
                "  Telephone — same working day for all inbound calls; voicemails returned within 4 hours\n" +
                "  Email — acknowledged within 4 working hours; substantive response within 1 working day " +
                "for routine matters; within 4 working hours for complaints or urgent delivery issues\n" +
                "  Customer portal / EDI — monitored at least twice daily; order acknowledgements issued " +
                "within 1 working day\n" +
                "  Written correspondence — acknowledged within 2 working days; full response within 5 " +
                "working days unless a longer timescale is agreed and confirmed in writing\n" +
                "  On-site customer visit — confirmed in writing by the Sales or Account Manager; " +
                "pre-visit agenda agreed; post-visit action log issued within 3 working days\n\n" +
                "All customer contacts of any significance (enquiries, orders, complaints, feedback, " +
                "technical queries, delivery issues) are logged in the CRM system on the same day of receipt."));
            qms009.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms009.CreatedAt, UpdatedAt = qms009.CreatedAt,
                ProcessId = qms009.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability matrix for customer communication activities."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Sales Manager / Account Managers\n\n" +
                "  • Primary point of contact for all commercial customer communications\n" +
                "  • Own the CRM record for each customer account; ensure all contacts are logged accurately\n" +
                "  • Handle enquiries, quotations, order placement, and order changes\n" +
                "  • Conduct or coordinate customer satisfaction surveys (QMS-017) and communicate results internally\n" +
                "  • Coordinate on-site customer visits; issue pre-visit agendas and post-visit action logs\n" +
                "  • Escalate customer concerns that may affect delivery, quality, or the commercial relationship " +
                "to the Commercial Director or Managing Director without delay"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Customer Service Team\n\n" +
                "  • Handle day-to-day order management and delivery status queries\n" +
                "  • Receive and log customer complaints; assign a complaint reference number and acknowledge " +
                "receipt to the customer within 4 working hours\n" +
                "  • Track complaint resolution progress and ensure responses are issued within the agreed timescale\n" +
                "  • Escalate complaints classified High or Critical to the Quality Manager and Sales Manager " +
                "within 2 working hours of receipt\n" +
                "  • Manage the handling of customer-owned property and maintain the customer property register"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Technical / Engineering Team\n\n" +
                "  • Respond to customer technical queries relating to product specifications, compatibility, " +
                "materials, and application advice\n" +
                "  • Provide technical inputs to quotations and contract reviews (QMS-010) where requested by Sales\n" +
                "  • Issue technical documentation (drawings, material certificates, test reports) to customers " +
                "in accordance with the agreed delivery terms\n" +
                "  • Support customer audits and source inspections with accurate technical information"));
            ps2.Contents.Add(Blk(ps2, 3,
                "Quality Manager\n\n" +
                "  • Receives and manages all customer 8D, SCAR, or formal corrective action requests\n" +
                "  • Leads or coordinates the response to High and Critical complaints; ensures root cause " +
                "analysis is conducted under QMS-020\n" +
                "  • Communicates quality improvement actions and corrective action closures to the customer\n" +
                "  • Monitors the complaint trend data and presents a summary at management review (QMS-019)\n" +
                "  • Maintains this procedure and the customer complaint classification criteria"));
            ps2.Contents.Add(Blk(ps2, 4,
                "All Customer-Facing Staff\n\n" +
                "  • Communicate professionally and accurately; never make commitments (delivery dates, " +
                "prices, specifications) that have not been confirmed internally\n" +
                "  • Log all significant customer contacts in the CRM on the day of the interaction\n" +
                "  • Never discuss quality failures, nonconformances, or internal investigations with " +
                "customers without first consulting the Quality Manager or Sales Manager\n" +
                "  • Refer media, legal, or regulatory enquiries from customers to senior management immediately"));
            qms009.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms009.CreatedAt, UpdatedAt = qms009.CreatedAt,
                ProcessId = qms009.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Steps for handling enquiries, orders, complaints, feedback, and contingency communication."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Enquiries, quotations, and product information\n\n" +
                "  1.1  All customer enquiries are logged in the CRM the day they are received, with the " +
                "customer name, contact person, date, channel, and a brief description of the enquiry.\n\n" +
                "  1.2  The Account Manager assesses whether the enquiry requires a formal quotation. " +
                "Standard product or service enquiries are responded to directly from the current approved " +
                "price list and lead-time schedule. Non-standard or high-value enquiries are escalated to " +
                "the Commercial Director.\n\n" +
                "  1.3  Before a quotation or commitment to supply is issued, the requirements review " +
                "process defined in QMS-010 must be completed. No commitments are made before this step.\n\n" +
                "  1.4  Product information (datasheets, specifications, drawings, certificates) issued " +
                "to customers must be the current approved version from the Document Library. The " +
                "Account Manager confirms the revision status before issuing."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Order placement and management\n\n" +
                "  2.1  On receipt of a customer purchase order or electronic order, the Customer Service " +
                "team checks the order against the agreed quotation or supply agreement: price, part number, " +
                "revision level, quantity, delivery date, and delivery address.\n\n" +
                "  2.2  Discrepancies between the order and the agreed terms are resolved with the customer " +
                "before acknowledgement. The resolution is documented in the CRM.\n\n" +
                "  2.3  An order acknowledgement is issued to the customer within 1 working day confirming " +
                "the accepted terms.\n\n" +
                "  2.4  Order changes requested by the customer after acknowledgement are re-reviewed under " +
                "QMS-010 before acceptance. The customer is notified in writing of the revised terms and a " +
                "new acknowledgement is issued.\n\n" +
                "  2.5  Delivery status updates are proactively communicated to customers where a confirmed " +
                "delivery date is at risk of being missed. The Account Manager notifies the customer as soon " +
                "as a delay is identified, provides a revised date, and records the communication in the CRM."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Complaint receipt and classification\n\n" +
                "A customer complaint is any expression of dissatisfaction — written or verbal — relating " +
                "to a product, service, delivery, commercial term, or communication. All complaints are:\n\n" +
                "  3.1  Logged in the complaint register with a unique reference number on the same day " +
                "of receipt. Acknowledgement is sent to the customer within 4 working hours.\n\n" +
                "  3.2  Classified by severity:\n" +
                "       Critical — product safety issue, regulatory reportable event, risk of customer line " +
                "stoppage, or threat of certification withdrawal; escalate to Quality Manager and Managing " +
                "Director within 2 hours\n" +
                "       High     — confirmed or suspected delivery of nonconforming product; customer " +
                "measurement rejection; require 8D or formal SCAR response; escalate to Quality Manager " +
                "within 4 working hours\n" +
                "       Medium   — delivery late or incomplete; documentation error; customer inconvenience " +
                "without product nonconformance; respond within 3 working days\n" +
                "       Low      — general dissatisfaction, communication issue, or minor administrative " +
                "error; respond within 5 working days\n\n" +
                "  3.3  A nonconformance is raised under QMS-016 for any complaint involving nonconforming " +
                "product. A corrective action is raised under QMS-020 for any High or Critical complaint " +
                "or where a root cause has been requested by the customer."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Complaint investigation and response\n\n" +
                "  4.1  The Quality Manager leads the investigation for High and Critical complaints; " +
                "the Customer Service Manager leads for Medium and Low complaints with Quality Manager support.\n\n" +
                "  4.2  Where the customer has requested an 8D (Eight Disciplines) or SCAR (Supplier " +
                "Corrective Action Request) response, the Quality Manager prepares this in the format " +
                "specified by the customer and issues it within the timescale stated in the customer's request.\n\n" +
                "  4.3  Where no specific format has been requested:\n" +
                "       Critical/High — written investigation summary including containment action, root " +
                "cause, corrective action, and effectiveness check timeline; issued within 5 working days\n" +
                "       Medium — written response confirming the cause and the corrective or improvement action; " +
                "issued within 5 working days\n" +
                "       Low — written or email response confirming the issue has been noted and any action taken\n\n" +
                "  4.4  The complaint is closed in the complaint register only when the customer has " +
                "confirmed acceptance of the response, or when the agreed response timeline has elapsed " +
                "with no further contact from the customer."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Step 5 — Customer property and contingency communication\n\n" +
                "Customer property (tooling, raw material, customer-supplied components, intellectual " +
                "property, or data) is identified, verified, protected, and safeguarded against loss, " +
                "damage, or misuse. A customer property register is maintained by the relevant department " +
                "manager. Any loss, damage, or unsuitability of customer property is reported to the " +
                "customer immediately and recorded in the CRM and customer property register.\n\n" +
                "Where a customer has specified contingency communication requirements (e.g., notification " +
                "within a defined timescale in the event of a production disruption, natural disaster, or " +
                "key personnel change), those requirements are recorded in the relevant customer account " +
                "record in the CRM and in the supply agreement. The Sales Manager is responsible for " +
                "ensuring contingency notifications are issued in accordance with the agreed terms " +
                "whenever a triggering event occurs."));
            qms009.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms009.CreatedAt, UpdatedAt = qms009.CreatedAt,
                ProcessId = qms009.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • CRM log — record of all significant customer contacts; maintained in the CRM system " +
                "with customer name, date, channel, contact person, summary, and outcome\n" +
                "  • Complaint register — log of all customer complaints with unique reference, date, " +
                "severity classification, description, owner, response date, and closure status; maintained " +
                "in Process Manager\n" +
                "  • Complaint investigation and response records — for High and Critical: 8D, SCAR, or " +
                "written investigation summary; for Medium: written response; for Low: email or note\n" +
                "  • Order acknowledgements and change confirmations — retained as evidence of the agreed " +
                "commercial terms at the time of order\n" +
                "  • Customer property register — list of all customer-owned items in the organisation's " +
                "possession, with condition, location, and any loss or damage reports\n" +
                "  • Customer satisfaction survey results — scored results and trend data; input to the " +
                "management review (QMS-019) and the QMS-017 analysis\n" +
                "  • Contingency notification records — where contingency events have been triggered, " +
                "records of the notification issued and customer acknowledgement"));
            ps4.Contents.Add(Blk(ps4, 1,
                "Retention periods\n\n" +
                "  • CRM log entries — retained for five years from the date of the interaction\n" +
                "  • Complaint records and investigation evidence — retained for ten years, or for the " +
                "period required to cover any statutory limitation period applicable to the product, " +
                "whichever is longer; Critical complaints are retained for the life of the certification\n" +
                "  • Order acknowledgements — retained for five years after the order is closed\n" +
                "  • Customer property register entries — retained for five years after the property " +
                "has been returned to the customer or disposed of\n" +
                "  • Customer satisfaction survey results — retained for five years from the survey date\n\n" +
                "Records must be retrievable by customer name, complaint reference, and date range for " +
                "audit and potential legal purposes."));
            qms009.ProcessSteps.Add(ps4);
        }

        // ── QMS-010 — full rich content blocks (ISO 9001:2015 clauses 8.2.2–8.2.4) ─
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms010.CreatedAt, UpdatedAt = qms010.CreatedAt,
                ProcessId = qms010.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for customer requirements review per ISO 9001:2015 clauses 8.2.2–8.2.4."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure ensures that all customer, statutory, regulatory, and organisation-imposed " +
                "requirements are fully understood and confirmed before the organisation makes any commitment " +
                "to supply a product or service. It prevents the acceptance of orders where requirements " +
                "are ambiguous, unduly risky, or beyond organisational capability, and ensures that any " +
                "subsequent changes to agreed requirements are reviewed and communicated before fulfilment " +
                "proceeds on the changed basis."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Normative reference\n\n" +
                "ISO 9001:2015, clause 8.2.2 — Determining requirements for products and services\n" +
                "Requires the organisation to determine the requirements for products and services to be " +
                "offered to customers, including applicable statutory and regulatory requirements.\n\n" +
                "ISO 9001:2015, clause 8.2.3 — Review of requirements for products and services\n" +
                "Requires that before committing to supply, the organisation reviews requirements specified " +
                "by the customer, requirements not stated but necessary for the intended use, and the " +
                "organisation's own requirements; ensures it can meet the requirements it defines or is " +
                "committed to; retains documented information of the review and of any new requirements.\n\n" +
                "ISO 9001:2015, clause 8.2.4 — Changes to requirements for products and services\n" +
                "Requires that when requirements change the relevant documented information is amended " +
                "and that relevant persons are made aware of the changed requirements."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Scope\n\n" +
                "This procedure applies to all quotations issued and all orders accepted by the organisation, " +
                "including:\n\n" +
                "  • New product and new service orders\n" +
                "  • Repeat orders where the requirements may have changed since the last supply\n" +
                "  • Orders placed by verbal instruction (telephone or on-site), which must be subsequently " +
                "confirmed in writing before production commences\n" +
                "  • Post-acceptance changes requested by the customer or identified internally\n\n" +
                "It does not apply to internal work orders. It is closely linked to QMS-009 (Customer " +
                "Communication) — enquiry handling and complaint management are governed there."));
            qms010.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms010.CreatedAt, UpdatedAt = qms010.CreatedAt,
                ProcessId = qms010.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability for determining, reviewing, and communicating requirements."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Sales / Account Manager\n\n" +
                "  • Captures all customer-stated requirements from the enquiry or purchase order\n" +
                "  • Initiates and coordinates the requirements review process before issuing a quotation\n" +
                "  • Obtains written customer confirmation of agreed requirements before acknowledgement\n" +
                "  • Manages post-acceptance change requests and re-triggers the review process\n" +
                "  • Communicates the agreed and any changed requirements to all internal stakeholders"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Technical / Engineering\n\n" +
                "  • Reviews requirements for technical completeness and compatibility with product/process capability\n" +
                "  • Identifies any statutory, regulatory, or materials requirements applicable to the product\n" +
                "  • Raises technical queries to the customer through the Sales Manager where requirements are unclear\n" +
                "  • Confirms feasibility sign-off on the requirements review record"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Production / Operations\n\n" +
                "  • Confirms capacity and lead-time feasibility against the customer's delivery requirements\n" +
                "  • Identifies any special process, tooling, or resource needs that affect acceptance\n" +
                "  • Confirms feasibility sign-off on the requirements review record"));
            ps2.Contents.Add(Blk(ps2, 3,
                "Quality Manager\n\n" +
                "  • Reviews requirements for quality plan, inspection, and testing implications\n" +
                "  • Confirms whether any customer-specific quality requirements (e.g., PPAP, FAIR, " +
                "special characteristics, customer-mandated suppliers) can be met\n" +
                "  • Escalates requirements that cannot be met to the Sales Manager and Commercial Director " +
                "before the quotation is issued"));
            qms010.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms010.CreatedAt, UpdatedAt = qms010.CreatedAt,
                ProcessId = qms010.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Steps for determining, reviewing, confirming, and managing changes to customer requirements."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Determine requirements\n\n" +
                "On receipt of an enquiry or purchase order the Sales Manager compiles a complete " +
                "requirements list covering:\n\n" +
                "  a)  Customer-stated requirements — part number, revision level, quantity, delivery date, " +
                "price, packaging, labelling, and any customer-specific quality, regulatory, or " +
                "documentation requirements stated in the order or the applicable supply agreement\n" +
                "  b)  Implied requirements — requirements not stated but necessary for the intended or " +
                "specified use (e.g., industry standards, material certifications, standard packaging norms)\n" +
                "  c)  Statutory and regulatory requirements applicable to the product or service in the " +
                "customer's country of use\n" +
                "  d)  Organisation-imposed requirements — internal standards, minimum margin thresholds, " +
                "minimum lead times, and any constraints from the risk register (QMS-005)"));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Review for completeness and feasibility\n\n" +
                "The Sales Manager circulates the requirements list to Technical, Production, and Quality " +
                "for review. Each function signs off on the requirements review record within the agreed " +
                "internal turnaround time (standard: 1 working day for repeat products; 3 working days " +
                "for new products or unusually complex requirements).\n\n" +
                "Reviewers check:\n" +
                "  • That all requirements are clearly defined (no ambiguities or conflicts)\n" +
                "  • That technical, capacity, and quality requirements can be met\n" +
                "  • That all statutory and regulatory requirements have been identified\n\n" +
                "Where a requirement cannot be met or a conflict is identified, the Sales Manager raises " +
                "a query with the customer before proceeding. No quotation or order acknowledgement is " +
                "issued until all queries are resolved and documented."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Issue quotation or acknowledge order\n\n" +
                "Once all reviewers have confirmed feasibility, the Sales Manager issues the quotation " +
                "or order acknowledgement. The acknowledgement explicitly states the agreed requirements — " +
                "part number, revision, quantity, price, delivery terms, and any special conditions — " +
                "so that the customer can confirm their order on an unambiguous basis.\n\n" +
                "Verbal orders received by telephone or during site visits are followed up with a written " +
                "order acknowledgement before production or service provision commences."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Manage changes to requirements\n\n" +
                "Where requirements change after acceptance — whether requested by the customer or " +
                "driven by an internal finding (e.g., an obsolete material, a design change, a " +
                "regulatory update) — the Sales Manager:\n\n" +
                "  a)  Re-initiates the requirements review process for the changed elements\n" +
                "  b)  Updates the requirements review record with the change description, date, and reviewer sign-offs\n" +
                "  c)  Issues a revised order acknowledgement or change confirmation to the customer\n" +
                "  d)  Notifies all internal functions (Technical, Production, Quality, Stores) of the " +
                "changed requirements before any further work is performed\n\n" +
                "Production must not proceed on changed requirements until the revised acknowledgement " +
                "has been issued to the customer and all internal functions have been notified."));
            qms010.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms010.CreatedAt, UpdatedAt = qms010.CreatedAt,
                ProcessId = qms010.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • Requirements review record — completed for every quotation and order; signed off by " +
                "Sales, Technical, Production, and Quality; retained in the order file\n" +
                "  • Order acknowledgements — including the agreed requirements; retained for five years\n" +
                "  • Change review records — updated requirements review record and revised acknowledgement " +
                "for every post-acceptance change; retained in the order file\n" +
                "  • Customer correspondence relating to requirements clarification — emails, meeting notes, " +
                "or query/response chains; retained with the order file\n" +
                "  • Feasibility sign-off — can be integrated into the requirements review record or held " +
                "separately; must be traceable to the specific order\n\n" +
                "Retention: all records associated with an order are retained for five years after the " +
                "last delivery on that order, or longer if required by the customer or applicable regulation."));
            qms010.ProcessSteps.Add(ps4);
        }

        // ── QMS-011 — full rich content blocks (ISO 9001:2015 clause 8.3) ────
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms011.CreatedAt, UpdatedAt = qms011.CreatedAt,
                ProcessId = qms011.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for design and development per ISO 9001:2015 clause 8.3."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure establishes a structured, stage-gated approach to design and development " +
                "(D&D) that ensures:\n\n" +
                "  a)  Design inputs are complete, unambiguous, and traceable to customer and regulatory requirements\n" +
                "  b)  Design outputs can be verified against inputs before release to production\n" +
                "  c)  The design is validated against intended use in representative conditions before first delivery\n" +
                "  d)  Changes to design — at any stage — are reviewed, authorised, and communicated before implementation\n" +
                "  e)  The risk of producing nonconforming product arising from a design deficiency is minimised"));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Normative reference — ISO 9001:2015 clause 8.3\n\n" +
                "8.3.2 Planning: define stages, reviews, responsibilities, resources, and interfaces.\n" +
                "8.3.3 Inputs: determine requirements derived from function, performance, regulatory, " +
                "and prior similar design.\n" +
                "8.3.4 Controls: apply reviews, verification, and validation at appropriate stages; " +
                "resolve problems before proceeding.\n" +
                "8.3.5 Outputs: ensure outputs meet input requirements; include specifications adequate " +
                "for production; reference monitoring and acceptance criteria.\n" +
                "8.3.6 Changes: identify, review, and control changes including evaluation of effect " +
                "on already-delivered product; retain documented information."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Scope\n\n" +
                "This procedure applies to all new product or service design activities and to substantive " +
                "changes to existing designs (i.e., changes that could affect form, fit, function, " +
                "regulatory compliance, or safety). It applies whether design is performed in-house or " +
                "by an external designer under the organisation's direction.\n\n" +
                "It does not apply to minor engineering changes of a documented, predictable nature " +
                "(e.g., toleranced dimension changes within an established process capability) which are " +
                "managed under the engineering change notice (ECN) process referenced in the design change log."));
            qms011.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms011.CreatedAt, UpdatedAt = qms011.CreatedAt,
                ProcessId = qms011.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability for design and development activities."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Design Lead / Chief Engineer\n\n" +
                "  • Owns the D&D plan and controls progression through stage gates\n" +
                "  • Responsible for the completeness and accuracy of design inputs and outputs\n" +
                "  • Chairs or delegates design review meetings\n" +
                "  • Signs off on verification and validation results before transfer to production\n" +
                "  • Authorises engineering change notices for minor changes; escalates substantive " +
                "changes to Quality for full D&D change review"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Quality Manager\n\n" +
                "  • Reviews the D&D plan to ensure review, verification, and validation activities " +
                "are adequate and appropriately staged\n" +
                "  • Participates in design reviews as an independent reviewer\n" +
                "  • Confirms that gate criteria are met before authorising transfer to operations\n" +
                "  • Manages design change review for substantive changes; updates the design change log\n" +
                "  • Ensures that any regulatory or customer approval requirements (e.g., PPAP, FAI) " +
                "are completed before first delivery"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Independent Reviewers\n\n" +
                "  • Provide unbiased technical assessment at each stage gate review\n" +
                "  • Must not review their own design work\n" +
                "  • Record comments and accept-with-actions or reject decisions in the review minutes\n\n" +
                "Production / Manufacturing Engineering\n\n" +
                "  • Review design outputs for manufacturability and process capability\n" +
                "  • Confirm that design outputs include sufficient detail for production to proceed " +
                "without ambiguity\n" +
                "  • Participate in validation activities where process validation is required"));
            qms011.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms011.CreatedAt, UpdatedAt = qms011.CreatedAt,
                ProcessId = qms011.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Stage-gated steps from D&D planning through transfer to production."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Stage 1 — Planning\n\n" +
                "The Design Lead prepares a D&D plan before design work commences, defining:\n" +
                "  • Stages of the D&D activity and the criteria for progression between stages\n" +
                "  • Review, verification, and validation activities at each stage\n" +
                "  • Roles, responsibilities, and authorities\n" +
                "  • Internal and external resources required\n" +
                "  • Customer involvement and approval points\n" +
                "  • Regulatory approvals or certifications required before delivery\n\n" +
                "The plan is reviewed and approved by the Quality Manager before design work starts. " +
                "It is a living document — updated as the design progresses and changes occur."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Stage 2 — Inputs\n\n" +
                "Design inputs are compiled from:\n" +
                "  • Customer requirements from the requirements review (QMS-010)\n" +
                "  • Functional and performance requirements\n" +
                "  • Applicable statutory and regulatory requirements\n" +
                "  • Information derived from previous similar designs (lessons learned, failure history)\n" +
                "  • Any other requirements essential for the specific type of design\n\n" +
                "Inputs must be complete and unambiguous. Conflicting requirements are resolved with the " +
                "customer or the relevant authority before design proceeds. The inputs list is approved " +
                "by the Design Lead and Quality Manager at the Stage 1 gate."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Stage 3 — Design reviews\n\n" +
                "Formal design reviews are conducted at each stage gate defined in the D&D plan. " +
                "Each review:\n" +
                "  • Is attended by representatives of all functions relevant to the stage being reviewed\n" +
                "  • Includes at least one independent reviewer who has not been involved in creating the work being reviewed\n" +
                "  • Is documented in review minutes recording: attendees, items reviewed, findings, decisions (pass/conditional pass/fail), and actions with owners and target dates\n\n" +
                "The Design Lead may not authorise progression through a gate where unresolved action items remain open unless the Quality Manager grants a formal conditional approval with a documented risk assessment."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Stage 4 — Verification and Validation\n\n" +
                "Verification confirms that design outputs meet the design inputs. Methods include " +
                "calculations, drawing checks, prototype testing, and comparison against proven designs.\n\n" +
                "Validation confirms that the resulting product or service can meet the requirements " +
                "for the specified application or intended use. Validation is performed under " +
                "representative conditions (or defined alternatively where this is not possible). " +
                "Where full validation before delivery is not feasible, partial validation and the " +
                "remaining validation plan are approved by the Quality Manager and, where required, the customer.\n\n" +
                "Verification and validation results are retained. Failures at either stage require " +
                "the design to be revised and the affected verification/validation activities to be repeated."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Stage 5 — Transfer and design changes\n\n" +
                "The design is transferred to production only when the Quality Manager has confirmed " +
                "that all gate criteria, verification, and validation activities are complete and that " +
                "any required customer or regulatory approvals have been obtained.\n\n" +
                "For any subsequent design change:\n" +
                "  a)  The change is documented in the design change log with a description, reason, " +
                "and the identity of affected documents and products\n" +
                "  b)  The impact on previously delivered product is assessed; where delivered product " +
                "may be affected, the Sales Manager and Quality Manager determine whether customer " +
                "notification or field action is required\n" +
                "  c)  Substantive changes follow the full D&D review, verification, and validation process\n" +
                "  d)  Minor changes are processed as engineering change notices, reviewed and approved " +
                "by the Design Lead and Quality Manager before release"));
            qms011.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms011.CreatedAt, UpdatedAt = qms011.CreatedAt,
                ProcessId = qms011.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • D&D plan — including stage definitions, review/verification/validation activities, and responsible persons\n" +
                "  • Design inputs record — the approved list of requirements forming the design basis\n" +
                "  • Design output documentation — drawings, specifications, BOM, process descriptions, acceptance criteria\n" +
                "  • Design review minutes — attendees, findings, decisions, and open actions for every formal review\n" +
                "  • Verification records — calculations, test reports, comparison records demonstrating outputs meet inputs\n" +
                "  • Validation records — test data, trial production results, or customer approval documentation\n" +
                "  • Design change log — all changes post-transfer with impact assessment and approval\n" +
                "  • Transfer authorisation — Quality Manager sign-off confirming all gates are closed\n\n" +
                "Retention: D&D records are retained for the life of the product plus ten years, " +
                "or as required by customer contract or regulation if longer."));
            qms011.ProcessSteps.Add(ps4);
        }

        // ── QMS-012 — full rich content blocks (ISO 9001:2015 clause 8.4) ────
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            var ps1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms012.CreatedAt, UpdatedAt = qms012.CreatedAt,
                ProcessId = qms012.Id, StepTemplateId = stDocSect.Id, Sequence = 1,
                NameOverride = "Purpose and Scope",
                DescriptionOverride = "Purpose, scope, and normative context for control of externally provided processes, products and services per ISO 9001:2015 clause 8.4."
            };
            ps1.Contents.Add(Blk(ps1, 0,
                "1.1  Purpose\n\n" +
                "This procedure ensures that externally provided products, services, and processes " +
                "conform to requirements before they are incorporated into the organisation's output " +
                "or delivered directly to customers. It establishes a risk-based framework for " +
                "supplier evaluation, selection, control, and re-evaluation that is proportionate " +
                "to the potential impact on product and service conformity."));
            ps1.Contents.Add(Blk(ps1, 1,
                "1.2  Normative reference — ISO 9001:2015 clause 8.4\n\n" +
                "8.4.1 (General) — Ensure externally provided processes, products and services conform; " +
                "apply controls based on ability to meet requirements and potential impact; maintain " +
                "an approved supplier register.\n\n" +
                "8.4.2 (Type and extent of control) — Ensure externally provided processes remain " +
                "within QMS control; define controls and verification activities; communicate requirements " +
                "to external providers.\n\n" +
                "8.4.3 (Information for external providers) — Communicate requirements for processes, " +
                "products, services, methods, equipment, competence, QMS interactions, and performance " +
                "monitoring before placement of orders."));
            ps1.Contents.Add(Blk(ps1, 2,
                "1.3  Scope and supplier categories\n\n" +
                "This procedure covers all external providers from whom goods or services that affect " +
                "product or service conformity are sourced. Suppliers are classified into three " +
                "categories that determine the level of control applied:\n\n" +
                "  Category A — Critical: suppliers of materials, components, or services that are " +
                "directly incorporated into the product and where a failure would present a safety, " +
                "regulatory, or major quality risk. Require initial on-site audit or PPAP; quarterly " +
                "performance scoring; annual formal re-evaluation.\n\n" +
                "  Category B — Significant: suppliers of standard purchased parts or services with " +
                "a moderate impact on quality. Require initial questionnaire assessment or trial order; " +
                "bi-annual performance scoring; annual review.\n\n" +
                "  Category C — Standard: suppliers of indirect materials, consumables, and services " +
                "with low quality impact. Require registration and periodic review; no formal audit."));
            qms012.ProcessSteps.Add(ps1);

            var ps2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms012.CreatedAt, UpdatedAt = qms012.CreatedAt,
                ProcessId = qms012.Id, StepTemplateId = stDocSect.Id, Sequence = 2,
                NameOverride = "Responsibilities",
                DescriptionOverride = "Accountability for supplier selection, control, and performance monitoring."
            };
            ps2.Contents.Add(Blk(ps2, 0,
                "Procurement Manager\n\n" +
                "  • Owns and maintains the approved supplier register\n" +
                "  • Leads supplier selection and initial assessment activities\n" +
                "  • Ensures purchase orders include all quality, technical, and regulatory requirements\n" +
                "  • Coordinates the quarterly/bi-annual performance scoring process\n" +
                "  • Initiates restriction or removal of under-performing suppliers"));
            ps2.Contents.Add(Blk(ps2, 1,
                "Quality Manager\n\n" +
                "  • Sets the acceptance criteria for each supplier category and approves the assessment methodology\n" +
                "  • Approves new Category A suppliers before first order placement\n" +
                "  • Reviews incoming inspection data and supplier performance scorecards\n" +
                "  • Raises supplier corrective action requests (SCARs) for significant performance failures\n" +
                "  • Approves concessions for nonconforming incoming material (QMS-016)\n" +
                "  • Maintains this procedure"));
            ps2.Contents.Add(Blk(ps2, 2,
                "Technical / Engineering\n\n" +
                "  • Approves suppliers for technically critical or regulated items\n" +
                "  • Defines the technical requirements (specifications, test requirements) to be " +
                "communicated to suppliers via purchase orders\n" +
                "  • Reviews supplier technical documentation (material certs, test reports, COCs)\n\n" +
                "Goods-In / Stores\n\n" +
                "  • Performs incoming inspection or verification in accordance with the incoming " +
                "inspection plan for each supplier category\n" +
                "  • Segregates and quarantines nonconforming deliveries; raises reports for Quality review\n" +
                "  • Confirms that delivery documentation (delivery notes, material certs) matches the purchase order"));
            qms012.ProcessSteps.Add(ps2);

            var ps3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms012.CreatedAt, UpdatedAt = qms012.CreatedAt,
                ProcessId = qms012.Id, StepTemplateId = stDocSect.Id, Sequence = 3,
                NameOverride = "Procedure",
                DescriptionOverride = "Steps for supplier assessment, approval, ordering, incoming inspection, performance monitoring, and re-evaluation."
            };
            ps3.Contents.Add(Blk(ps3, 0,
                "Step 1 — Evaluate and approve new suppliers\n\n" +
                "Before placing a first order with any new supplier, Procurement classifies the supplier " +
                "into a category (A, B, or C) based on the nature and risk of the goods or services " +
                "to be sourced. The initial assessment method is determined by category:\n\n" +
                "  Category A — on-site audit using the organisation's supplier audit checklist; " +
                "or review of an equivalent second-party audit within the last 12 months; " +
                "or review of current third-party certification (ISO 9001, AS9100, IATF 16949, etc.) " +
                "plus a technical qualification (e.g., PPAP, first article approval)\n\n" +
                "  Category B — supplier self-assessment questionnaire; or review of third-party " +
                "certification; or a controlled trial order with enhanced incoming inspection\n\n" +
                "  Category C — registration and confirmation of legal trading status; no formal quality assessment required\n\n" +
                "The Quality Manager approves the assessment outcome for Category A; the Procurement " +
                "Manager approves for Categories B and C. Approved suppliers are added to the " +
                "approved supplier register with their category, scope of approval, and assessment date."));
            ps3.Contents.Add(Blk(ps3, 1,
                "Step 2 — Communicate requirements\n\n" +
                "All purchase orders for Category A and B suppliers must include or reference:\n\n" +
                "  • Full product or service specification (part number, revision level, drawing, standard)\n" +
                "  • Required material certifications, test reports, or certificate of conformance\n" +
                "  • Any applicable statutory or regulatory requirements\n" +
                "  • Inspection and release authority requirements (e.g., customer source inspection rights)\n" +
                "  • Packaging, labelling, and traceability requirements\n" +
                "  • Right of access for the organisation, its customer, and regulatory authorities to " +
                "the supplier's facilities and records\n" +
                "  • Any specific quality management system requirements (e.g., PPAP, IMDS, REACH/RoHS compliance)\n\n" +
                "Purchase orders are reviewed and approved by the Procurement Manager before issue. " +
                "Verbal orders to approved Category B/C suppliers for standard items are permissible " +
                "only where a standing supply agreement already captures the above requirements."));
            ps3.Contents.Add(Blk(ps3, 2,
                "Step 3 — Incoming inspection and verification\n\n" +
                "Incoming goods are inspected or verified by Goods-In in accordance with the incoming " +
                "inspection plan maintained by the Quality Manager. The plan specifies for each " +
                "supplier/product category: the inspection method, sample size, acceptance criteria, " +
                "and required documentation.\n\n" +
                "  Category A — 100% dimensional or functional check on first articles; " +
                "AQL-based sampling for subsequent deliveries\n" +
                "  Category B — document verification (COC, certs) plus visual inspection; " +
                "periodic dimensional sampling\n" +
                "  Category C — document verification and visual check only\n\n" +
                "Where incoming material fails inspection, it is quarantined and a nonconformance " +
                "is raised under QMS-016. The supplier is notified of the rejection and a SCAR or " +
                "return-to-vendor process is initiated by the Quality Manager."));
            ps3.Contents.Add(Blk(ps3, 3,
                "Step 4 — Monitor supplier performance\n\n" +
                "Supplier performance is scored on the following KPIs:\n\n" +
                "  • On-time delivery (OTD) — percentage of line items delivered on the confirmed date\n" +
                "  • Quality acceptance rate (QAR) — percentage of deliveries passing incoming inspection without rejection\n" +
                "  • Documentation compliance — percentage of deliveries accompanied by complete and correct documentation\n" +
                "  • Responsiveness — speed and quality of response to quality queries and SCARs\n\n" +
                "Category A suppliers are scored quarterly; Category B bi-annually. Scores are " +
                "compiled by Procurement and reviewed by the Quality Manager. Suppliers falling below " +
                "the target thresholds (OTD < 95%, QAR < 98%) are placed on a formal improvement " +
                "plan with a 90-day recovery period. Failure to recover triggers restriction " +
                "(new orders suspended) and formal re-evaluation."));
            ps3.Contents.Add(Blk(ps3, 4,
                "Step 5 — Re-evaluate and maintain register\n\n" +
                "A formal re-evaluation is conducted annually for all Category A suppliers and for " +
                "any Category B supplier placed on an improvement plan. Re-evaluation uses the same " +
                "assessment method as the initial approval.\n\n" +
                "Outcomes:\n" +
                "  Continued approval — performance meets targets; no significant findings\n" +
                "  Conditional approval — improvement plan in place; under monitoring\n" +
                "  Restricted — new orders suspended pending corrective action\n" +
                "  Removed — approval withdrawn; supplier removed from register; all open orders reviewed\n\n" +
                "The approved supplier register is updated immediately on any status change. " +
                "Procurement notifies all affected internal functions when a supplier's status changes."));
            qms012.ProcessSteps.Add(ps3);

            var ps4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = qms012.CreatedAt, UpdatedAt = qms012.CreatedAt,
                ProcessId = qms012.Id, StepTemplateId = stDocSect.Id, Sequence = 4,
                NameOverride = "Records and Documented Information",
                DescriptionOverride = "Documented information maintained or retained under this procedure."
            };
            ps4.Contents.Add(Blk(ps4, 0,
                "The following documented information shall be maintained or retained:\n\n" +
                "  • Approved supplier register — current approved list with category, scope, assessment date, status, and any restrictions\n" +
                "  • Supplier assessment records — audit reports, questionnaire responses, trial order results used for initial and re-evaluation approval\n" +
                "  • Purchase orders — including quality clauses and referenced specifications; retained for five years\n" +
                "  • Incoming inspection records — inspection results, acceptance/rejection decisions, and nonconformance references\n" +
                "  • Supplier performance scorecards — quarterly/bi-annual KPI data for each scored supplier\n" +
                "  • SCAR records and responses — corrective action requests issued to suppliers and their responses\n" +
                "  • Improvement plans — documented plans and progress records for suppliers on conditional approval\n\n" +
                "Retention: supplier assessment and performance records are retained for five years " +
                "from the supplier's removal from the register or the end of the last supply relationship. " +
                "Incoming inspection records are retained for the same period as the associated product records."));
            qms012.ProcessSteps.Add(ps4);
        }

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

        // Per-record upsert — only insert docs whose code is not already in the database.
        // This handles partial deletions without hitting the unique-code constraint.
        var existingQmsCodes = db.Processes
            .Where(p => p.Code.StartsWith("QMS-"))
            .Select(p => p.Code)
            .ToHashSet();
        var allQmsDocs = new[]
        {
            qms001, qms002, qms003, qms004, qms005,
            qms006, qms007, qms008, qms009, qms010,
            qms011, qms012, qms013, qms014, qms015,
            qms016, qms017, qms018, qms019, qms020,
            qms021
        };
        db.Processes.AddRange(allQmsDocs.Where(d => !existingQmsCodes.Contains(d.Code)));

        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // System onboarding training documents
    // Each course teaches users a real feature of Process Manager, so browsing
    // the Training Catalogue acts as an interactive orientation guide.
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task SeedTrainingDocumentsAsync(ProcessManagerDbContext db)
    {

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
                EffectiveDate        = Utc(-createdDaysAgo),
                IsSystemContent      = true
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

        // ── Module 9 — Process Authoring Walkthrough ──────────────────────────────
        var trnDsn001 = Course(
            "TRN-DSN-001",
            "How to Create a Process",
            "Process Authoring — Foundation",
            "A practical hands-on walkthrough for any Engineer or Admin who is creating a process " +
            "in Process Manager for the first time. Covers the complete authoring lifecycle: " +
            "creating the process record (code, name, description, role), adding and arranging " +
            "steps from the shared step template library, writing rich instructional content for " +
            "each step in the Slide view, reviewing the finished process in Document view, and " +
            "submitting for approval to publish it. Ends with a self-check. Recommended before " +
            "authoring any live process that will be used in production or compliance activities.",
            expiryDays: 365, "A", 1, 25);

        // ── Shared step template for all training course modules ─────────────
        var stTrnMod = db.StepTemplates.FirstOrDefault(t => t.Code == "TRN-MOD-01")
            ?? new StepTemplate
            {
                Id = Guid.NewGuid(), CreatedAt = Utc(-95), UpdatedAt = Utc(-95),
                Code = "TRN-MOD-01", Name = "Training Module",
                Description = "A learning module or topic within a training course.",
                Pattern = StepPattern.General, IsActive = true, IsShared = true,
                Status = ProcessStatus.Released, Version = 1,
                IsSystemContent = true
            };
        if (db.Entry(stTrnMod).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
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

        // TRN-DSN-001 — How to Create a Process (rich content walkthrough)
        {
            static ProcessStepContent Blk(ProcessStep ps, int order, string body) => new()
            {
                Id = Guid.NewGuid(), CreatedAt = ps.CreatedAt, UpdatedAt = ps.CreatedAt,
                ProcessStepId = ps.Id,
                ContentType = StepContentType.Text,
                ContentCategory = ContentCategory.Reference,
                SortOrder = order, Body = body
            };

            // Step 1 — What is a Process?
            var dsnPs1 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 1,
                NameOverride = "What is a Process?",
                DescriptionOverride = "Understanding the core concept before you start building."
            };
            dsnPs1.Contents.Add(Blk(dsnPs1, 0,
                "In Process Manager, a process is the reusable template that defines a sequence of " +
                "work. Every time that work is actually performed — a production run, a training event, " +
                "a service delivery — the system creates a Job that references the process. The process " +
                "stays unchanged; the job captures what actually happened, who did it, and when.\n\n" +
                "Think of a process as the recipe and the job as the batch record. You write the recipe " +
                "once; you run it as many times as you need, and each run produces its own history."));
            dsnPs1.Contents.Add(Blk(dsnPs1, 1,
                "Every process record has four core header fields:\n\n" +
                "  Code — A short unique identifier for this process (e.g. WDG-MFG-01, QMS-018, TRN-SYS-003). " +
                "Once set, this code should not change — it is used in cross-references, audit trails, and " +
                "document-control numbering.\n\n" +
                "  Name — A clear human-readable title describing what the process covers. " +
                "Aim for 3–7 words that are meaningful to someone who has never seen it before.\n\n" +
                "  Description — One or two sentences explaining the purpose and scope of the process. " +
                "This text appears in search results, the Document Library, and the Training Catalogue, " +
                "so it should help a user decide whether this is the process they are looking for.\n\n" +
                "  Process Role — The classification that controls where the process appears in the " +
                "application (see the next block)."));
            dsnPs1.Contents.Add(Blk(dsnPs1, 2,
                "Process Roles and where they appear:\n\n" +
                "  Manufacturing Process — Shown in the Create Job UI so operators and engineers can " +
                "select it when starting a production run, inspection, or service delivery.\n\n" +
                "  QMS Document — Appears in the Document Library under the QMS Documents filter. " +
                "Subject to the full controlled-document approval workflow (Draft → Pending Approval " +
                "→ Released → Superseded). Used for procedures, policies, and work instructions that " +
                "form part of the ISO 9001 quality system.\n\n" +
                "  Work Instruction — Also in the Document Library. Version-controlled and approval-routed, " +
                "but used for operational instructions rather than formal QMS procedures.\n\n" +
                "  Training — Appears in the Training Catalogue. Completing the training job generates " +
                "a Competency Record for the learner.\n\n" +
                "  Approval Process — Defines an approval routing template used internally by the " +
                "document approval system. Not selectable in the Create Job UI."));
            trnDsn001.ProcessSteps.Add(dsnPs1);

            // Step 2 — Prerequisites
            var dsnPs2 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 2,
                NameOverride = "Before You Start",
                DescriptionOverride = "What you need to have ready before opening the Process Builder."
            };
            dsnPs2.Contents.Add(Blk(dsnPs2, 0,
                "Role requirement\n\n" +
                "Creating and editing processes requires the Engineer or Admin role. Standard users " +
                "can view and execute processes but cannot author them. If you do not see the Design " +
                "section in the navigation sidebar, or cannot see a 'New Process' button on the " +
                "Processes list page, ask your system administrator to check your role assignment."));
            dsnPs2.Contents.Add(Blk(dsnPs2, 1,
                "Identify the steps you need\n\n" +
                "Processes are built from step templates — reusable building blocks that live in the " +
                "shared step library. Before opening the builder, list the steps your process requires " +
                "and check whether suitable templates already exist (Design → Step Templates). " +
                "Using existing shared templates is almost always preferable to creating new ones, " +
                "because content improvements made to a shared template benefit every process that " +
                "uses it.\n\n" +
                "If no suitable template exists, you will need to create one first (see TRN-SYS-004 — " +
                "Managing Step Templates). You cannot add a step to a process without a template."));
            dsnPs2.Contents.Add(Blk(dsnPs2, 2,
                "Draft the process on paper first\n\n" +
                "Before touching the keyboard, write down:\n" +
                "  1.  The process code (check it does not already exist in the system)\n" +
                "  2.  The process name and a one-sentence description\n" +
                "  3.  The ordered list of steps and which template each one uses\n" +
                "  4.  For each step: any content you plan to add (instructions, safety notes, images, prompts)\n\n" +
                "Having this prepared reduces the risk of forgetting a step or entering a description " +
                "that has to be corrected later under formal change control."));
            trnDsn001.ProcessSteps.Add(dsnPs2);

            // Step 3 — Create the Process Record
            var dsnPs3 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 3,
                NameOverride = "Create the Process Record",
                DescriptionOverride = "Opening the Process Builder and filling in the header fields."
            };
            dsnPs3.Contents.Add(Blk(dsnPs3, 0,
                "Navigate to the Process Builder\n\n" +
                "1.  In the left navigation sidebar, expand Design and click Processes.\n" +
                "2.  On the Processes list page, click New Process (top-right of the page).\n" +
                "3.  The New Process form opens. Fill in the fields as described below.\n\n" +
                "If you are creating a QMS Document or Work Instruction you can also reach New Process " +
                "from the Document Library page — the form is identical."));
            dsnPs3.Contents.Add(Blk(dsnPs3, 1,
                "Fill in the header fields\n\n" +
                "Code — Enter the unique code for this process. Follow your organisation's numbering " +
                "convention (e.g. QMS-025 or TRN-DSN-001). The code must not already exist in the " +
                "system — the form will warn you if it does.\n\n" +
                "Name — Enter a clear title. Users search for processes by name, so make it specific " +
                "enough to distinguish it from related processes in the same family.\n\n" +
                "Description — One or two sentences stating what the process covers, who carries it " +
                "out, and any important scope boundary (e.g. 'Covers Widget Mk2 variants only; " +
                "Mk3 and above use WDG-MFG-02').\n\n" +
                "Process Role — Select the appropriate role from the dropdown. This cannot easily be " +
                "changed after the process has been released, so choose carefully."));
            dsnPs3.Contents.Add(Blk(dsnPs3, 2,
                "Save the draft\n\n" +
                "Click Save (or Create). The process is saved in Draft status and you are taken to " +
                "the Process Builder canvas.\n\n" +
                "No one else can see a Draft process in the job creation UI, the Document Library, " +
                "or the Training Catalogue until it is Released — so there is no risk of operators " +
                "encountering an unfinished process."));
            trnDsn001.ProcessSteps.Add(dsnPs3);

            // Step 4 — Add and Arrange Steps
            var dsnPs4 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 4,
                NameOverride = "Add and Arrange Steps",
                DescriptionOverride = "Populating the process with steps from the shared step template library."
            };
            dsnPs4.Contents.Add(Blk(dsnPs4, 0,
                "Adding a step\n\n" +
                "In the Process Builder, click the Add Step button. A panel opens showing the " +
                "step template library — a searchable list of all available templates. " +
                "Filter by name, code, or step pattern. Select the template you want and click Add " +
                "(or double-click the row). The step appears as a node in the Diagram view and " +
                "is added to the end of the process sequence. Repeat for each step.\n\n" +
                "You can add the same template more than once — for example, two separate inspection " +
                "steps at different points in the process."));
            dsnPs4.Contents.Add(Blk(dsnPs4, 1,
                "Reordering steps\n\n" +
                "In the Diagram view, drag and drop step nodes on the canvas to reorder them. " +
                "The sequence numbers update automatically.\n\n" +
                "Alternatively, the Step List view (accessible from the Builder toolbar) lets you " +
                "change sequence numbers by typing or drag rows to reorder. Both views show the " +
                "same data — a change in one immediately appears in the other.\n\n" +
                "To remove a step, select it and click Remove. This removes it from the process " +
                "but does not delete the underlying template from the library."));
            dsnPs4.Contents.Add(Blk(dsnPs4, 2,
                "Name and description overrides\n\n" +
                "Each step can carry a Name Override and a Description Override to personalise a " +
                "shared template for this specific process context. For example, the template might " +
                "be named 'Incoming Inspection' and the override might read " +
                "'Incoming Inspection — Widget Mk2 raw materials only'.\n\n" +
                "Overrides appear instead of the template defaults in the Diagram view, Slide view, " +
                "and in the operator-facing execution wizard. They do not affect the underlying " +
                "template or any other process that references it."));
            trnDsn001.ProcessSteps.Add(dsnPs4);

            // Step 5 — Author Step Content
            var dsnPs5 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 5,
                NameOverride = "Author Step Content",
                DescriptionOverride = "Writing instructions, warnings, and prompts for each step using the Slide view."
            };
            dsnPs5.Contents.Add(Blk(dsnPs5, 0,
                "Two content systems — template-level vs. step-level\n\n" +
                "Process Manager has two places where content can be attached to a step:\n\n" +
                "  Template-level content lives on the step template and is shown every time that " +
                "template is used in any process. This is the right place for universal instructions " +
                "that apply regardless of context — standard safety warnings, measurement methods, " +
                "reference images that never change.\n\n" +
                "  Step-level content lives on the individual process step within this specific " +
                "process only and is shown only when this process is executed. This is the right " +
                "place for context-specific content: which part number to use, process-specific " +
                "acceptance criteria, or supplementary notes that do not apply to every use of the " +
                "template.\n\n" +
                "When building a new process, you author step-level content in the Slide view. " +
                "Template-level content is managed separately under Design → Step Templates."));
            dsnPs5.Contents.Add(Blk(dsnPs5, 1,
                "Using the Slide view\n\n" +
                "Switch to Slide view by clicking the Slide button in the Process Builder toolbar. " +
                "The Slide view shows one step at a time as a full editing panel — like a " +
                "PowerPoint slide for each step.\n\n" +
                "To add a content block to the current step:\n" +
                "  1.  Click Add Block inside the step panel.\n" +
                "  2.  Select the block type: Text (instructions, cautions, notes), " +
                "Image (diagrams and reference photos), or Prompt (operator inputs).\n" +
                "  3.  For Text blocks, type directly into the editor. Use the toolbar for bold, " +
                "italic, and lists.\n" +
                "  4.  For Image blocks, upload the file or choose from the media library.\n" +
                "  5.  For Prompt blocks, select the prompt type (Pass/Fail, Numeric Entry, " +
                "Multiple Choice, Checkbox, Scan, or User Picker) and fill in the label plus " +
                "any validation values.\n" +
                "  6.  Set the Sort Order if block sequence matters (lower numbers appear first).\n" +
                "  7.  Save the block."));
            dsnPs5.Contents.Add(Blk(dsnPs5, 2,
                "Content categories and sort order conventions\n\n" +
                "Each content block can be assigned a category. The category controls the visual " +
                "treatment in the execution wizard so operators can quickly orient themselves:\n\n" +
                "  Setup (0–19) — Preparation: retrieving materials, configuring the workstation, " +
                "loading fixtures.\n\n" +
                "  Safety (20–29) — Hazard warnings. Always enable 'Acknowledgment Required' so " +
                "the operator must explicitly confirm they have read the warning before the wizard " +
                "advances.\n\n" +
                "  Reference (30–39) — Diagrams, drawings, and specification documents. If an " +
                "image is not yet available, use the placeholder format: " +
                "[Figure 1 — description of diagram].\n\n" +
                "  Inspection (40–79) — Active prompts the operator must respond to: numeric " +
                "measurements, pass/fail checks, multiple-choice selections.\n\n" +
                "  Note (80+) — Post-step information, escalation rules, or supplementary " +
                "content that does not block progress."));
            trnDsn001.ProcessSteps.Add(dsnPs5);

            // Step 6 — Review and Submit
            var dsnPs6 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 6,
                NameOverride = "Review and Submit for Approval",
                DescriptionOverride = "Checking the finished process in Document view and publishing through the approval workflow."
            };
            dsnPs6.Contents.Add(Blk(dsnPs6, 0,
                "Self-review in Document view\n\n" +
                "Before submitting for formal approval, switch to Document view (click the Document " +
                "button in the Process Builder toolbar). Document view renders a typeset, read-only " +
                "version of the process — the same layout shown in the Document Library and available " +
                "for printing or PDF export.\n\n" +
                "Use Document view to verify:\n" +
                "  •  All steps appear in the correct sequence\n" +
                "  •  Step names and descriptions are clear and unambiguous\n" +
                "  •  Content blocks appear under the correct steps with no obvious gaps\n" +
                "  •  Safety warnings are present where expected\n" +
                "  •  No obvious spelling or grammar issues\n\n" +
                "You cannot edit in Document view — switch back to Slide or Diagram view to make " +
                "corrections, then return to verify."));
            dsnPs6.Contents.Add(Blk(dsnPs6, 1,
                "Submitting for approval\n\n" +
                "When satisfied, click Submit for Approval (available from the process detail page " +
                "or Process Builder toolbar). A dialogue asks for:\n\n" +
                "  Change Description — Write a specific statement of what has been created or " +
                "changed and why. Good: 'New incoming inspection process for Widget Mk2 raw " +
                "material; no prior process existed.' Poor: 'Updated.'\n\n" +
                "  Approval Process Template — Select the routing template defining who must " +
                "approve this document (e.g. 'Engineering Document — 2 Approvers' or " +
                "'QMS Procedure — QM + Director').\n\n" +
                "  Reviewers — Assign a named reviewer to each approval step.\n\n" +
                "Click Submit. The process status changes to Pending Approval and each reviewer " +
                "receives a notification in their My Work queue."));
            dsnPs6.Contents.Add(Blk(dsnPs6, 2,
                "After approval — what happens\n\n" +
                "When all reviewers have approved, the process automatically transitions to Released:\n\n" +
                "  •  A Manufacturing Process becomes available in the Create Job UI\n" +
                "  •  A QMS Document or Work Instruction appears in the Document Library as " +
                "the current released version\n" +
                "  •  A Training course appears in the Training Catalogue\n\n" +
                "If any reviewer rejects the submission, the process returns to Draft and you receive " +
                "a notification with their comments. Address the feedback and re-submit when ready.\n\n" +
                "You can monitor approval progress at any time from the process detail page, where " +
                "the full approval chain and each reviewer's current status are shown."));
            trnDsn001.ProcessSteps.Add(dsnPs6);

            // Step 7 — Knowledge Check
            var dsnPs7 = new ProcessStep
            {
                Id = Guid.NewGuid(), CreatedAt = trnDsn001.CreatedAt, UpdatedAt = trnDsn001.CreatedAt,
                ProcessId = trnDsn001.Id, StepTemplateId = stTrnMod.Id, Sequence = 7,
                NameOverride = "Knowledge Check",
                DescriptionOverride = "Confirm your understanding before completing this training module."
            };
            dsnPs7.Contents.Add(new ProcessStepContent
            {
                Id = Guid.NewGuid(), CreatedAt = dsnPs7.CreatedAt, UpdatedAt = dsnPs7.CreatedAt,
                ProcessStepId = dsnPs7.Id,
                ContentType = StepContentType.Prompt, PromptType = PromptType.PassFail,
                ContentCategory = ContentCategory.Inspection, SortOrder = 0, IsRequired = true,
                Label = "I can explain the difference between a Process (the reusable template) and a Job (the execution record for a specific run)."
            });
            dsnPs7.Contents.Add(new ProcessStepContent
            {
                Id = Guid.NewGuid(), CreatedAt = dsnPs7.CreatedAt, UpdatedAt = dsnPs7.CreatedAt,
                ProcessStepId = dsnPs7.Id,
                ContentType = StepContentType.Prompt, PromptType = PromptType.PassFail,
                ContentCategory = ContentCategory.Inspection, SortOrder = 1, IsRequired = true,
                Label = "I know which Process Role to choose when creating a manufacturing work instruction versus a training course."
            });
            dsnPs7.Contents.Add(new ProcessStepContent
            {
                Id = Guid.NewGuid(), CreatedAt = dsnPs7.CreatedAt, UpdatedAt = dsnPs7.CreatedAt,
                ProcessStepId = dsnPs7.Id,
                ContentType = StepContentType.Prompt, PromptType = PromptType.PassFail,
                ContentCategory = ContentCategory.Inspection, SortOrder = 2, IsRequired = true,
                Label = "I understand the difference between template-level content (shown every time that template is used) and step-level content (shown only in this specific process)."
            });
            dsnPs7.Contents.Add(new ProcessStepContent
            {
                Id = Guid.NewGuid(), CreatedAt = dsnPs7.CreatedAt, UpdatedAt = dsnPs7.CreatedAt,
                ProcessStepId = dsnPs7.Id,
                ContentType = StepContentType.Prompt, PromptType = PromptType.MultipleChoice,
                ContentCategory = ContentCategory.Inspection, SortOrder = 3, IsRequired = true,
                Label = "Which view in the Process Builder is designed for authoring detailed operator instructions, content blocks, and prompts for each step?",
                Choices = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    "Diagram view — the flowchart canvas",
                    "Slide view — the panel-per-step content editor",
                    "Document view — the typeset read-only output",
                    "List view — the sequence number editor"
                })
            });
            trnDsn001.ProcessSteps.Add(dsnPs7);
        }

        // Per-record upsert — only insert courses whose code is not already in the database.
        var existingTrnCodes = db.Processes
            .Where(p => p.Code.StartsWith("TRN-SYS-") || p.Code.StartsWith("TRN-DSN-"))
            .Select(p => p.Code)
            .ToHashSet();
        var allCourses = new[]
        {
            trn001, trn002, trn003, trn004, trn005, trn006,
            trn007, trn008, trn009, trn010, trn011, trn012,
            trnDsn001
        };
        db.Processes.AddRange(allCourses.Where(c => !existingTrnCodes.Contains(c.Code)));

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

    // =========================================================================
    // M2 — Onboarding sample content
    // =========================================================================

    /// <summary>IDs of the seeded sample artifacts — returned so the caller can store them on onboarding state.</summary>
    public record SampleProcessIds(Guid KindId, Guid StepTemplateId, Guid ProcessId);

    /// <summary>Default domain-vocabulary name for a signup industry choice.</summary>
    public static string ResolveDefaultVocabularyName(OnboardingIndustry industry) => industry switch
    {
        OnboardingIndustry.CNC     => "CNC Machining",
        OnboardingIndustry.PCBA    => "PCB Assembly",
        OnboardingIndustry.Medical => "Medical Device",
        _                          => "General Manufacturing"
    };

    /// <summary>Default "Kind" label for a signup industry choice.</summary>
    public static string ResolveDefaultKindLabel(OnboardingIndustry industry) => industry switch
    {
        OnboardingIndustry.CNC     => "Part",
        OnboardingIndustry.PCBA    => "Board",
        OnboardingIndustry.Medical => "Device",
        _                          => "Part"
    };

    /// <summary>
    /// Seed a tiny, runnable sample: one Kind (with default Grade), one StepTemplate
    /// (input Material port + Inspection prompt + output Material port), one Process
    /// containing that single step, all in Released state. Returns the IDs so the
    /// onboarding wizard can deep-link to them.
    ///
    /// The <paramref name="tenantId"/> is honoured via the tenant context scope the
    /// caller has already opened; we still set it explicitly to be robust against
    /// callers that do not wrap the call in a scope.
    /// </summary>
    public static async Task<SampleProcessIds> SeedSampleProcessAsync(
        ProcessManagerDbContext db,
        OnboardingIndustry industry,
        Guid tenantId)
    {
        // Industry-specific labels. Codes carry the tenant suffix to avoid clashes
        // because Code uniqueness is enforced across rows without tenant awareness.
        var (kindCode, kindName, kindDesc, stepCode, stepName, procCode, procName, inspectLabel, units, min, max, nominal) = industry switch
        {
            OnboardingIndustry.CNC => (
                "SAMPLE-SHAFT", "Sample Shaft", "A demo CNC-turned shaft used by the onboarding wizard.",
                "SAMPLE-TURN", "Turn Outside Diameter",
                "SAMPLE-PROC", "Sample Shaft Turning",
                "Outside diameter (mm)", "mm", 9.95m, 10.05m, 10.00m),
            OnboardingIndustry.PCBA => (
                "SAMPLE-PCB", "Sample PCB", "A demo PCB panel used by the onboarding wizard.",
                "SAMPLE-AOI", "Automated Optical Inspection",
                "SAMPLE-PROC", "Sample PCB Inspection",
                "AOI defect count", "defects", 0m, 0m, 0m),
            OnboardingIndustry.Medical => (
                "SAMPLE-DEV", "Sample Device", "A demo medical device used by the onboarding wizard.",
                "SAMPLE-QC", "Final QC Inspection",
                "SAMPLE-PROC", "Sample Device Final QC",
                "Leak test pressure (kPa)", "kPa", 98.0m, 102.0m, 100.0m),
            _ => (
                "SAMPLE-WIDGET", "Sample Widget", "A demo widget used by the onboarding wizard.",
                "SAMPLE-INSP", "Widget Inspection",
                "SAMPLE-PROC", "Sample Widget Inspection",
                "Widget weight (g)", "g", 49.0m, 51.0m, 50.0m)
        };

        // Short suffix ensures re-runnability (e.g. dogfood testing) — each call
        // creates fresh content if existing sample codes are already in the DB.
        var suffix = tenantId.ToString()[..6].ToUpperInvariant();
        kindCode  = $"{kindCode}-{suffix}";
        stepCode  = $"{stepCode}-{suffix}";
        procCode  = $"{procCode}-{suffix}";

        var now = DateTime.UtcNow;

        // ── Kind + default Grade ─────────────────────────────────────────────
        var kind = new Kind
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            Code = kindCode, Name = kindName, Description = kindDesc,
            IsSerialized = false, IsBatchable = true,
            SourceType = KindSourceType.Make,
            UnitOfMeasure = "Each"
        };
        var gradeNew = new Grade
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            KindId = kind.Id, Code = "NEW", Name = "New", IsDefault = true, SortOrder = 1
        };
        var gradePass = new Grade
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            KindId = kind.Id, Code = "PASS", Name = "Passed", IsDefault = false, SortOrder = 2
        };
        kind.Grades.Add(gradeNew);
        kind.Grades.Add(gradePass);
        db.Kinds.Add(kind);

        // ── StepTemplate with one input port, one output port, one prompt ───
        var step = new StepTemplate
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            Code = stepCode, Name = stepName,
            Description = "Sample step seeded by the onboarding wizard. Safe to edit or delete.",
            Pattern = StepPattern.Transform,
            Version = 1, IsActive = true,
            Status = ProcessStatus.Released,
            IsShared = true,
            ExpectedDurationMinutes = 5
        };
        var inPort = new Port
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            StepTemplateId = step.Id,
            Name = "Incoming", Direction = PortDirection.Input, PortType = PortType.Material,
            KindId = kind.Id, GradeId = gradeNew.Id,
            QtyRuleMode = QuantityRuleMode.Exactly, QtyRuleN = 1,
            SortOrder = 1
        };
        var outPort = new Port
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            StepTemplateId = step.Id,
            Name = "Inspected", Direction = PortDirection.Output, PortType = PortType.Material,
            KindId = kind.Id, GradeId = gradePass.Id,
            QtyRuleMode = QuantityRuleMode.Exactly, QtyRuleN = 1,
            SortOrder = 1
        };
        step.Ports.Add(inPort);
        step.Ports.Add(outPort);

        // One simple numeric prompt — gives the ExecutionWizard something to render.
        var prompt = SeederStepBuilder.Numeric(step, 40, inspectLabel, units: units, min: min, max: max, nominal: nominal);
        prompt.TenantId = tenantId;
        step.Contents.Add(prompt);

        var setup = SeederStepBuilder.Setup(step, 0,
            "This is a sample step seeded automatically by onboarding. Modify or delete it once you've explored the wizard.");
        setup.TenantId = tenantId;
        step.Contents.Add(setup);

        db.StepTemplates.Add(step);

        // ── Process containing the single step ───────────────────────────────
        var process = new Process
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            Code = procCode, Name = procName,
            Description = "Sample process seeded by the onboarding wizard.",
            Version = 1, IsActive = true,
            Status = ProcessStatus.Released,
            RevisionCode = "A", EffectiveDate = now
        };
        var processStep = new ProcessStep
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            CreatedAt = now, UpdatedAt = now,
            ProcessId = process.Id,
            StepTemplateId = step.Id,
            Sequence = 1
        };
        process.ProcessSteps.Add(processStep);
        db.Processes.Add(process);

        await db.SaveChangesAsync();
        return new SampleProcessIds(kind.Id, step.Id, process.Id);
    }
}

// =============================================================================
// SeederStepBuilder — typed helpers for authoring StepTemplate content blocks.
//
// USAGE RULES (enforced by convention, not compiler):
//   1. Manufacturing step instructions → StepTemplate.Contents  (StepTemplateContent)
//      QMS / training document steps  → ProcessStep.Contents    (ProcessStepContent)
//      Use the Blk() local helper pattern (already in SeedQmsDocumentsAsync) for
//      the latter; use SeederStepBuilder for the former.
//
//   2. Sort-order conventions:
//        0–19   Setup blocks
//       20–29   Safety blocks  (always set acknowledgmentRequired: true)
//       30–39   Reference blocks
//       40–79   Inspection prompts (NumericEntry, PassFail, MultipleChoice)
//       80+     Note blocks
//
//   3. Any NumericEntry Inspection prompt with min/max values MUST be followed
//      by a RunChartWidget call on the same StepTemplate.
//
//   4. Safety blocks always set acknowledgmentRequired: true — never omit this.
//
//   5. Images are not seeded as real files.  Represent image references as a
//      Reference-category Text block:  "[Figure 1 — Fixture loading diagram]"
// =============================================================================
public static class SeederStepBuilder
{
    // ── Text blocks ──────────────────────────────────────────────────────────

    /// <summary>Setup instruction text block (sort order 0–19).</summary>
    public static StepTemplateContent Setup(StepTemplate st, int order, string body) =>
        TextBlock(st, order, ContentCategory.Setup, body, false);

    /// <summary>
    /// Safety instruction text block (sort order 20–29).
    /// Always sets AcknowledgmentRequired = true.
    /// </summary>
    public static StepTemplateContent Safety(StepTemplate st, int order, string body) =>
        TextBlock(st, order, ContentCategory.Safety, body, acknowledgmentRequired: true);

    /// <summary>Reference/diagram placeholder text block (sort order 30–39).</summary>
    public static StepTemplateContent Reference(StepTemplate st, int order, string body) =>
        TextBlock(st, order, ContentCategory.Reference, body, false);

    /// <summary>Note text block (sort order 80+).</summary>
    public static StepTemplateContent Note(StepTemplate st, int order, string body) =>
        TextBlock(st, order, ContentCategory.Note, body, false);

    private static StepTemplateContent TextBlock(
        StepTemplate st, int order, ContentCategory category, string body, bool acknowledgmentRequired) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Text,
        SortOrder = order,
        ContentCategory = category,
        Body = body,
        AcknowledgmentRequired = acknowledgmentRequired,
        IntroducedInVersion = st.Version
    };

    // ── Prompt blocks ────────────────────────────────────────────────────────

    /// <summary>
    /// Numeric measurement prompt (sort order 40–79).
    /// Pass hardLimit: true for characteristics that must trigger NC disposition on breach.
    /// A RunChart MUST be added for any numeric prompt that has min/max values.
    /// </summary>
    public static StepTemplateContent Numeric(
        StepTemplate st, int order, string label,
        string? units = null,
        decimal? min = null, decimal? max = null, decimal? nominal = null,
        bool required = true, bool hardLimit = false) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Prompt,
        PromptType = PromptType.NumericEntry,
        SortOrder = order,
        ContentCategory = ContentCategory.Inspection,
        Label = label,
        Units = units,
        MinValue = min, MaxValue = max, NominalValue = nominal,
        IsRequired = required,
        IsHardLimit = hardLimit,
        IntroducedInVersion = st.Version
    };

    /// <summary>
    /// Pass / Fail prompt (sort order 40–79).
    /// Pass hardLimit: true for characteristics where Fail must trigger NC disposition.
    /// </summary>
    public static StepTemplateContent PassFail(
        StepTemplate st, int order, string label,
        bool required = true, bool hardLimit = false) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Prompt,
        PromptType = PromptType.PassFail,
        SortOrder = order,
        ContentCategory = ContentCategory.Inspection,
        Label = label,
        IsRequired = required,
        IsHardLimit = hardLimit,
        IntroducedInVersion = st.Version
    };

    /// <summary>Multiple-choice prompt (sort order 40–79).</summary>
    public static StepTemplateContent Choice(
        StepTemplate st, int order, string label,
        string[] options, bool required = true) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Prompt,
        PromptType = PromptType.MultipleChoice,
        SortOrder = order,
        ContentCategory = ContentCategory.Inspection,
        Label = label,
        Choices = System.Text.Json.JsonSerializer.Serialize(options),
        IsRequired = required,
        IntroducedInVersion = st.Version
    };

    /// <summary>Single-line text / scan entry prompt.</summary>
    public static StepTemplateContent Scan(
        StepTemplate st, int order, string label, bool required = true) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Prompt,
        PromptType = PromptType.Scan,
        SortOrder = order,
        ContentCategory = ContentCategory.Setup,
        Label = label,
        IsRequired = required,
        IntroducedInVersion = st.Version
    };

    /// <summary>Checkbox acknowledgment prompt.</summary>
    public static StepTemplateContent Checkbox(
        StepTemplate st, int order, string label,
        ContentCategory category = ContentCategory.Setup, bool required = true) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Prompt,
        PromptType = PromptType.Checkbox,
        SortOrder = order,
        ContentCategory = category,
        Label = label,
        IsRequired = required,
        IntroducedInVersion = st.Version
    };

    /// <summary>UserPicker prompt — captures a named user (instructor, witness, signatory).</summary>
    public static StepTemplateContent UserPicker(
        StepTemplate st, int order, string label,
        ContentCategory category = ContentCategory.Inspection, bool required = true) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        ContentType = StepContentType.Prompt,
        PromptType = PromptType.UserPicker,
        SortOrder = order,
        ContentCategory = category,
        Label = label,
        IsRequired = required,
        IntroducedInVersion = st.Version
    };

    // ── Run chart ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Attach a run chart to a StepTemplate, sourcing data from the given Numeric prompt block.
    /// Call this AFTER adding the source prompt to st.Contents.
    /// SpecMin/SpecMax default to the prompt's MinValue/MaxValue when null.
    /// </summary>
    public static RunChartWidget Chart(
        StepTemplate st, StepTemplateContent sourcePrompt, string label,
        int window = 30, decimal? specMin = null, decimal? specMax = null) => new()
    {
        Id = Guid.NewGuid(), CreatedAt = st.CreatedAt, UpdatedAt = st.CreatedAt,
        StepTemplateId = st.Id,
        SourceContentId = sourcePrompt.Id,
        Label = label,
        ChartWindowSize = window,
        SpecMin = specMin ?? sourcePrompt.MinValue,
        SpecMax = specMax ?? sourcePrompt.MaxValue,
        DisplayOrder = st.RunChartWidgets.Count
    };
}
