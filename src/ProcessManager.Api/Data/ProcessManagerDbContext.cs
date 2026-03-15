using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Data;

public class ProcessManagerDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ProcessManagerDbContext(
        DbContextOptions<ProcessManagerDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Phase 1: Type System
    public DbSet<Kind> Kinds => Set<Kind>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<DomainVocabulary> DomainVocabularies => Set<DomainVocabulary>();

    // Phase 2: Step Design
    public DbSet<StepTemplate> StepTemplates => Set<StepTemplate>();
    public DbSet<Port> Ports => Set<Port>();
    public DbSet<StepTemplateImage> StepTemplateImages => Set<StepTemplateImage>();
    public DbSet<RunChartWidget> RunChartWidgets => Set<RunChartWidget>();

    // Phase 3: Process Composition
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<ProcessStep> ProcessSteps => Set<ProcessStep>();
    public DbSet<ProcessStepContent> ProcessStepContents => Set<ProcessStepContent>();
    public DbSet<ProcessStepPortOverride> ProcessStepPortOverrides => Set<ProcessStepPortOverride>();
    public DbSet<StepTemplateContent> StepTemplateContents => Set<StepTemplateContent>();
    public DbSet<Flow> Flows => Set<Flow>();

    // Phase 5: Execution / Runtime
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<StepExecution> StepExecutions => Set<StepExecution>();
    public DbSet<PortTransaction> PortTransactions => Set<PortTransaction>();
    public DbSet<ExecutionData> ExecutionData => Set<ExecutionData>();
    public DbSet<PromptResponse> PromptResponses => Set<PromptResponse>();
    public DbSet<NonConformance> NonConformances => Set<NonConformance>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();

    // Phase 4: Workflow Composition
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowProcess> WorkflowProcesses => Set<WorkflowProcess>();
    public DbSet<WorkflowLink> WorkflowLinks => Set<WorkflowLink>();
    public DbSet<WorkflowLinkCondition> WorkflowLinkConditions => Set<WorkflowLinkCondition>();

    // Power BI Dashboards
    public DbSet<PowerBiDashboard> PowerBiDashboards => Set<PowerBiDashboard>();

    // Phase 7: Quality Engineering Tools
    public DbSet<Pfmea> Pfmeas => Set<Pfmea>();
    public DbSet<PfmeaFailureMode> PfmeaFailureModes => Set<PfmeaFailureMode>();
    public DbSet<PfmeaAction> PfmeaActions => Set<PfmeaAction>();
    public DbSet<CeMatrix> CeMatrices => Set<CeMatrix>();
    public DbSet<CeInput> CeInputs => Set<CeInput>();
    public DbSet<CeOutput> CeOutputs => Set<CeOutput>();
    public DbSet<CeCorrelation> CeCorrelations => Set<CeCorrelation>();
    public DbSet<ControlPlan> ControlPlans => Set<ControlPlan>();
    public DbSet<ControlPlanEntry> ControlPlanEntries => Set<ControlPlanEntry>();

    // Phase 14: Document Control & QMS
    public DbSet<DocumentApprovalRequest> DocumentApprovalRequests => Set<DocumentApprovalRequest>();

    // Phase 10a: Root Cause Library
    public DbSet<RootCauseEntry> RootCauseEntries => Set<RootCauseEntry>();

    // Phase 10b: Ishikawa Diagrams
    public DbSet<IshikawaDiagram> IshikawaDiagrams => Set<IshikawaDiagram>();
    public DbSet<IshikawaCause> IshikawaCauses => Set<IshikawaCause>();

    // Phase 10c: Branching 5 Whys
    public DbSet<FiveWhysAnalysis> FiveWhysAnalyses => Set<FiveWhysAnalysis>();
    public DbSet<FiveWhysNode> FiveWhysNodes => Set<FiveWhysNode>();

    // Phase 10d: Material Review Board
    public DbSet<MrbReview> MrbReviews => Set<MrbReview>();
    public DbSet<MrbParticipant> MrbParticipants => Set<MrbParticipant>();

    // Phase 15: Tiered Accountability & Action Tracking
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<ManagementReview> ManagementReviews => Set<ManagementReview>();

    // Phase 16: Training & Competency Management
    public DbSet<CompetencyRecord> CompetencyRecords => Set<CompetencyRecord>();
    public DbSet<ProcessTrainingRequirement> ProcessTrainingRequirements => Set<ProcessTrainingRequirement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Kind ---
        modelBuilder.Entity<Kind>(e =>
        {
            e.HasKey(k => k.Id);
            e.HasIndex(k => k.Code).IsUnique();
            e.Property(k => k.Code).HasMaxLength(50).IsRequired();
            e.Property(k => k.Name).HasMaxLength(200).IsRequired();
        });

        // --- Grade ---
        modelBuilder.Entity<Grade>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => new { g.KindId, g.Code }).IsUnique();
            e.Property(g => g.Code).HasMaxLength(50).IsRequired();
            e.Property(g => g.Name).HasMaxLength(200).IsRequired();

            e.HasOne(g => g.Kind)
                .WithMany(k => k.Grades)
                .HasForeignKey(g => g.KindId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- DomainVocabulary ---
        modelBuilder.Entity<DomainVocabulary>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Name).IsUnique();
            e.Property(d => d.Name).HasMaxLength(200).IsRequired();
            e.Property(d => d.TermKind).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermKindCode).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermGrade).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermItem).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermItemId).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermBatch).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermBatchId).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermJob).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermWorkflow).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermProcess).HasMaxLength(100).IsRequired();
            e.Property(d => d.TermStep).HasMaxLength(100).IsRequired();
        });

        // --- StepTemplate ---
        modelBuilder.Entity<StepTemplate>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Code).HasMaxLength(50).IsRequired();
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
            e.Property(s => s.Pattern).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        });

        // --- StepTemplateImage ---
        modelBuilder.Entity<StepTemplateImage>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.FileName).HasMaxLength(200).IsRequired();
            e.Property(i => i.OriginalFileName).HasMaxLength(200).IsRequired();
            e.Property(i => i.MimeType).HasMaxLength(100).IsRequired();

            e.HasOne(i => i.StepTemplate)
                .WithMany(s => s.Images)
                .HasForeignKey(i => i.StepTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Port ---
        modelBuilder.Entity<Port>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Direction).HasConversion<string>().HasMaxLength(10);
            e.Property(p => p.PortType).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.QtyRuleMode).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.DataType).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Units).HasMaxLength(50);
            e.Property(p => p.NominalValue).HasMaxLength(200);
            e.Property(p => p.LowerTolerance).HasMaxLength(100);
            e.Property(p => p.UpperTolerance).HasMaxLength(100);

            e.HasOne(p => p.StepTemplate)
                .WithMany(s => s.Ports)
                .HasForeignKey(p => p.StepTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Kind)
                .WithMany()
                .HasForeignKey(p => p.KindId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Grade)
                .WithMany()
                .HasForeignKey(p => p.GradeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Process ---
        modelBuilder.Entity<Process>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(50).IsRequired();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.ProcessRole).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.RevisionCode).HasMaxLength(20);
            e.Property(p => p.ChangeDescription).HasMaxLength(2000);

            e.HasOne(p => p.ParentProcess)
                .WithMany()
                .HasForeignKey(p => p.ParentProcessId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.ApprovalProcess)
                .WithMany()
                .HasForeignKey(p => p.ApprovalProcessId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ProcessStep ---
        modelBuilder.Entity<ProcessStep>(e =>
        {
            e.HasKey(ps => ps.Id);
            e.HasIndex(ps => new { ps.ProcessId, ps.Sequence }).IsUnique();

            e.Property(ps => ps.NameOverride).HasMaxLength(200);
            e.Property(ps => ps.PatternOverride)
                .HasConversion<string?>()
                .HasMaxLength(20);

            e.HasOne(ps => ps.Process)
                .WithMany(p => p.ProcessSteps)
                .HasForeignKey(ps => ps.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ps => ps.StepTemplate)
                .WithMany()
                .HasForeignKey(ps => ps.StepTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ProcessStepPortOverride ---
        modelBuilder.Entity<ProcessStepPortOverride>(e =>
        {
            e.HasKey(po => po.Id);
            e.HasIndex(po => new { po.ProcessStepId, po.PortId }).IsUnique();

            e.Property(po => po.NameOverride).HasMaxLength(200);
            e.Property(po => po.DirectionOverride)
                .HasConversion<string?>()
                .HasMaxLength(20);
            e.Property(po => po.QtyRuleModeOverride)
                .HasConversion<string?>()
                .HasMaxLength(20);

            e.HasOne(po => po.ProcessStep)
                .WithMany(ps => ps.PortOverrides)
                .HasForeignKey(po => po.ProcessStepId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(po => po.Port)
                .WithMany()
                .HasForeignKey(po => po.PortId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(po => po.KindOverride)
                .WithMany()
                .HasForeignKey(po => po.KindIdOverride)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(po => po.GradeOverride)
                .WithMany()
                .HasForeignKey(po => po.GradeIdOverride)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- RunChartWidget ---
        modelBuilder.Entity<RunChartWidget>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Label).HasMaxLength(300).IsRequired();

            e.HasOne(w => w.StepTemplate)
                .WithMany(st => st.RunChartWidgets)
                .HasForeignKey(w => w.StepTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Preserve widgets if the source prompt is deleted (set null handled in app layer)
            e.HasOne(w => w.SourceContent)
                .WithMany()
                .HasForeignKey(w => w.SourceContentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(w => w.StepTemplateId);
        });

        // --- StepTemplateContent ---
        modelBuilder.Entity<StepTemplateContent>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ContentType).HasConversion<string>().HasMaxLength(10);
            e.Property(c => c.Body).HasMaxLength(10000);
            e.Property(c => c.FileName).HasMaxLength(200);
            e.Property(c => c.OriginalFileName).HasMaxLength(200);
            e.Property(c => c.MimeType).HasMaxLength(100);
            e.Property(c => c.PromptType).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Label).HasMaxLength(500);
            e.Property(c => c.Units).HasMaxLength(50);
            e.Property(c => c.Choices).HasMaxLength(4000);
            e.Property(c => c.ContentCategory).HasConversion<string>().HasMaxLength(20);

            e.HasOne(c => c.StepTemplate)
                .WithMany(st => st.Contents)
                .HasForeignKey(c => c.StepTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => c.StepTemplateId);
        });

        // --- ProcessStepContent ---
        modelBuilder.Entity<ProcessStepContent>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ContentType).HasConversion<string>().HasMaxLength(10);
            e.Property(c => c.Body).HasMaxLength(10000);
            e.Property(c => c.FileName).HasMaxLength(200);
            e.Property(c => c.OriginalFileName).HasMaxLength(200);
            e.Property(c => c.MimeType).HasMaxLength(100);
            e.Property(c => c.PromptType).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Label).HasMaxLength(500);
            e.Property(c => c.Units).HasMaxLength(50);
            e.Property(c => c.Choices).HasMaxLength(4000);
            e.Property(c => c.ContentCategory).HasConversion<string>().HasMaxLength(20);

            e.HasOne(c => c.ProcessStep)
                .WithMany(ps => ps.Contents)
                .HasForeignKey(c => c.ProcessStepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PromptResponse ---
        modelBuilder.Entity<PromptResponse>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.ResponseValue).HasMaxLength(1000);
            e.Property(r => r.OverrideNote).HasMaxLength(2000);

            e.HasOne(r => r.StepExecution)
                .WithMany(se => se.PromptResponses)
                .HasForeignKey(r => r.StepExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Preserve historical responses if a prompt block is deleted
            e.HasOne(r => r.ProcessStepContent)
                .WithMany()
                .HasForeignKey(r => r.ProcessStepContentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(r => r.StepTemplateContent)
                .WithMany()
                .HasForeignKey(r => r.StepTemplateContentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(r => r.StepExecutionId);
        });

        // --- Flow ---
        modelBuilder.Entity<Flow>(e =>
        {
            e.HasKey(f => f.Id);

            e.HasOne(f => f.Process)
                .WithMany(p => p.Flows)
                .HasForeignKey(f => f.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.SourceProcessStep)
                .WithMany()
                .HasForeignKey(f => f.SourceProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.SourcePort)
                .WithMany()
                .HasForeignKey(f => f.SourcePortId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.TargetProcessStep)
                .WithMany()
                .HasForeignKey(f => f.TargetProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.TargetPort)
                .WithMany()
                .HasForeignKey(f => f.TargetPortId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Workflow ---
        modelBuilder.Entity<Workflow>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.Code).IsUnique();
            e.Property(w => w.Code).HasMaxLength(50).IsRequired();
            e.Property(w => w.Name).HasMaxLength(200).IsRequired();
        });

        // --- WorkflowProcess ---
        modelBuilder.Entity<WorkflowProcess>(e =>
        {
            e.HasKey(wp => wp.Id);
            e.HasIndex(wp => new { wp.WorkflowId, wp.ProcessId });

            e.HasOne(wp => wp.Workflow)
                .WithMany(w => w.WorkflowProcesses)
                .HasForeignKey(wp => wp.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(wp => wp.Process)
                .WithMany()
                .HasForeignKey(wp => wp.ProcessId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- WorkflowLink ---
        modelBuilder.Entity<WorkflowLink>(e =>
        {
            e.HasKey(wl => wl.Id);
            e.HasIndex(wl => new { wl.SourceWorkflowProcessId, wl.TargetWorkflowProcessId }).IsUnique();
            e.Property(wl => wl.Name).HasMaxLength(200);
            e.Property(wl => wl.RoutingType).HasConversion<string>().HasMaxLength(20);

            e.HasOne(wl => wl.Workflow)
                .WithMany(w => w.WorkflowLinks)
                .HasForeignKey(wl => wl.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(wl => wl.SourceWorkflowProcess)
                .WithMany(wp => wp.OutgoingLinks)
                .HasForeignKey(wl => wl.SourceWorkflowProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(wl => wl.TargetWorkflowProcess)
                .WithMany(wp => wp.IncomingLinks)
                .HasForeignKey(wl => wl.TargetWorkflowProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- WorkflowLinkCondition ---
        modelBuilder.Entity<WorkflowLinkCondition>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.WorkflowLinkId, c.GradeId }).IsUnique();

            e.HasOne(c => c.WorkflowLink)
                .WithMany(wl => wl.Conditions)
                .HasForeignKey(c => c.WorkflowLinkId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Grade)
                .WithMany()
                .HasForeignKey(c => c.GradeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Job ---
        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => j.Code).IsUnique();
            e.Property(j => j.Code).HasMaxLength(50).IsRequired();
            e.Property(j => j.Name).HasMaxLength(200).IsRequired();
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(j => j.Process)
                .WithMany()
                .HasForeignKey(j => j.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentApprovalRequestId is stored for quick lookup but FK is enforced via the DAR entity.
            e.Property(j => j.DocumentApprovalRequestId).IsRequired(false);
        });

        // --- Item ---
        modelBuilder.Entity<Item>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => new { i.KindId, i.SerialNumber })
                .IsUnique()
                .HasFilter(null); // InMemory provider doesn't support filtered indexes
            e.Property(i => i.SerialNumber).HasMaxLength(100);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(i => i.Kind)
                .WithMany()
                .HasForeignKey(i => i.KindId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Grade)
                .WithMany()
                .HasForeignKey(i => i.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Job)
                .WithMany(j => j.Items)
                .HasForeignKey(i => i.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Batch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Batch ---
        modelBuilder.Entity<Batch>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.Code).IsUnique();
            e.Property(b => b.Code).HasMaxLength(50).IsRequired();
            e.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(b => b.Kind)
                .WithMany()
                .HasForeignKey(b => b.KindId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Grade)
                .WithMany()
                .HasForeignKey(b => b.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Job)
                .WithMany(j => j.Batches)
                .HasForeignKey(b => b.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- StepExecution ---
        modelBuilder.Entity<StepExecution>(e =>
        {
            e.HasKey(se => se.Id);
            e.HasIndex(se => new { se.JobId, se.ProcessStepId }).IsUnique();
            e.Property(se => se.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(se => se.AssignedToUserId).HasMaxLength(450);

            e.HasOne(se => se.Job)
                .WithMany(j => j.StepExecutions)
                .HasForeignKey(se => se.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(se => se.ProcessStep)
                .WithMany()
                .HasForeignKey(se => se.ProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PortTransaction ---
        modelBuilder.Entity<PortTransaction>(e =>
        {
            e.HasKey(pt => pt.Id);

            e.HasOne(pt => pt.StepExecution)
                .WithMany(se => se.PortTransactions)
                .HasForeignKey(pt => pt.StepExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pt => pt.Port)
                .WithMany()
                .HasForeignKey(pt => pt.PortId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(pt => pt.Item)
                .WithMany(i => i.PortTransactions)
                .HasForeignKey(pt => pt.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(pt => pt.Batch)
                .WithMany(b => b.PortTransactions)
                .HasForeignKey(pt => pt.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ExecutionData ---
        modelBuilder.Entity<ExecutionData>(e =>
        {
            e.HasKey(ed => ed.Id);
            e.Property(ed => ed.Key).HasMaxLength(200).IsRequired();
            e.Property(ed => ed.Value).HasMaxLength(1000).IsRequired();
            e.Property(ed => ed.DataType).HasConversion<string>().HasMaxLength(20);
            e.Property(ed => ed.UnitOfMeasure).HasMaxLength(50);

            e.HasOne(ed => ed.StepExecution)
                .WithMany(se => se.ExecutionData)
                .HasForeignKey(ed => ed.StepExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ed => ed.Batch)
                .WithMany(b => b.ExecutionData)
                .HasForeignKey(ed => ed.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ed => ed.Item)
                .WithMany(i => i.ExecutionData)
                .HasForeignKey(ed => ed.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 7: Quality Engineering Tools ──────────────────────────────

        // --- Pfmea ---
        modelBuilder.Entity<Pfmea>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(100).IsRequired();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.StalenessClearedBy).HasMaxLength(200);
            e.Property(p => p.StalenessClearanceNotes).HasMaxLength(2000);

            e.HasOne(p => p.Process)
                .WithMany()
                .HasForeignKey(p => p.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PfmeaFailureMode ---
        modelBuilder.Entity<PfmeaFailureMode>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.StepFunction).HasMaxLength(500).IsRequired();
            e.Property(f => f.FailureMode).HasMaxLength(500).IsRequired();
            e.Property(f => f.FailureEffect).HasMaxLength(500).IsRequired();
            e.Property(f => f.FailureCause).HasMaxLength(500);
            e.Property(f => f.PreventionControls).HasMaxLength(1000);
            e.Property(f => f.DetectionControls).HasMaxLength(1000);
            // RPN is computed in entity, not stored
            e.Ignore(f => f.Rpn);

            e.HasOne(f => f.Pfmea)
                .WithMany(p => p.FailureModes)
                .HasForeignKey(f => f.PfmeaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(f => f.ProcessStep)
                .WithMany()
                .HasForeignKey(f => f.ProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PfmeaAction ---
        modelBuilder.Entity<PfmeaAction>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Description).HasMaxLength(1000).IsRequired();
            e.Property(a => a.ResponsiblePerson).HasMaxLength(200);
            e.Property(a => a.CompletionNotes).HasMaxLength(2000);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            // RevisedRpn is computed in entity, not stored
            e.Ignore(a => a.RevisedRpn);

            e.HasOne(a => a.FailureMode)
                .WithMany(f => f.Actions)
                .HasForeignKey(a => a.FailureModeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- CeMatrix ---
        modelBuilder.Entity<CeMatrix>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(200).IsRequired();

            e.HasOne(m => m.ProcessStep)
                .WithMany()
                .HasForeignKey(m => m.ProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CeInput ---
        modelBuilder.Entity<CeInput>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).HasMaxLength(200).IsRequired();
            e.Property(i => i.Category).HasConversion<string>().HasMaxLength(30);

            e.HasOne(i => i.CeMatrix)
                .WithMany(m => m.Inputs)
                .HasForeignKey(i => i.CeMatrixId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Port)
                .WithMany()
                .HasForeignKey(i => i.PortId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // --- CeOutput ---
        modelBuilder.Entity<CeOutput>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.Category).HasConversion<string>().HasMaxLength(30);

            e.HasOne(o => o.CeMatrix)
                .WithMany(m => m.Outputs)
                .HasForeignKey(o => o.CeMatrixId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(o => o.Port)
                .WithMany()
                .HasForeignKey(o => o.PortId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // --- CeCorrelation ---
        modelBuilder.Entity<CeCorrelation>(e =>
        {
            e.HasKey(c => c.Id);
            // Unique constraint: one score per input/output pair
            e.HasIndex(c => new { c.CeInputId, c.CeOutputId }).IsUnique();

            e.HasOne(c => c.Input)
                .WithMany(i => i.Correlations)
                .HasForeignKey(c => c.CeInputId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Output)
                .WithMany(o => o.Correlations)
                .HasForeignKey(c => c.CeOutputId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- PowerBiDashboard ---
        modelBuilder.Entity<PowerBiDashboard>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Name).IsUnique();
            e.Property(d => d.Name).HasMaxLength(200).IsRequired();
            e.Property(d => d.EmbedUrl).HasMaxLength(2000).IsRequired();
            e.Property(d => d.Description).HasMaxLength(1000);
        });

        // --- NonConformance ---
        modelBuilder.Entity<NonConformance>(e =>
        {
            e.HasKey(nc => nc.Id);
            e.Property(nc => nc.LimitType).HasConversion<string>().HasMaxLength(20);
            e.Property(nc => nc.DispositionStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(nc => nc.ActualValue).HasMaxLength(500);
            e.Property(nc => nc.DisposedBy).HasMaxLength(200);
            e.Property(nc => nc.JustificationText).HasMaxLength(2000);

            e.HasOne(nc => nc.StepExecution)
                .WithMany(se => se.NonConformances)
                .HasForeignKey(nc => nc.StepExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(nc => nc.ContentBlock)
                .WithMany()
                .HasForeignKey(nc => nc.ContentBlockId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ApprovalRecord ---
        modelBuilder.Entity<ApprovalRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityType).HasMaxLength(20).IsRequired();
            e.Property(a => a.SubmittedBy).HasMaxLength(200).IsRequired();
            e.Property(a => a.ReviewedBy).HasMaxLength(200);
            e.Property(a => a.Decision).HasMaxLength(20).IsRequired();
            e.Property(a => a.Notes).HasMaxLength(2000);

            e.HasOne(a => a.Process)
                .WithMany(p => p.ApprovalRecords)
                .HasForeignKey(a => a.ProcessId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.StepTemplate)
                .WithMany(s => s.ApprovalRecords)
                .HasForeignKey(a => a.StepTemplateId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => new { a.EntityType, a.EntityId });
        });

        // ── Phase 14: Document Control & QMS ────────────────────────────────

        // --- DocumentApprovalRequest ---
        modelBuilder.Entity<DocumentApprovalRequest>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(d => d.SubmittedBy).HasMaxLength(200).IsRequired();

            e.HasOne(d => d.Process)
                .WithMany(p => p.DocumentApprovalRequests)
                .HasForeignKey(d => d.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApprovalJobId is a FK to Job — configured as unidirectional to avoid circular FK.
            e.HasOne(d => d.ApprovalJob)
                .WithMany()
                .HasForeignKey(d => d.ApprovalJobId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(d => d.ProcessId);
            e.HasIndex(d => d.ApprovalJobId);
        });

        // ── Phase 7c: Control Plan ───────────────────────────────────────────

        // --- ControlPlan ---
        modelBuilder.Entity<ControlPlan>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.HasIndex(cp => cp.Code).IsUnique();
            e.Property(cp => cp.Code).HasMaxLength(100).IsRequired();
            e.Property(cp => cp.Name).HasMaxLength(200).IsRequired();
            e.Property(cp => cp.StalenessClearedBy).HasMaxLength(200);
            e.Property(cp => cp.StalenessClearanceNotes).HasMaxLength(2000);

            e.HasOne(cp => cp.Process)
                .WithMany()
                .HasForeignKey(cp => cp.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ControlPlanEntry ---
        modelBuilder.Entity<ControlPlanEntry>(e =>
        {
            e.HasKey(ce => ce.Id);
            e.Property(ce => ce.CharacteristicName).HasMaxLength(300).IsRequired();
            e.Property(ce => ce.CharacteristicType).HasConversion<string>().HasMaxLength(20);
            e.Property(ce => ce.SpecificationOrTolerance).HasMaxLength(500);
            e.Property(ce => ce.MeasurementTechnique).HasMaxLength(300);
            e.Property(ce => ce.SampleSize).HasMaxLength(200);
            e.Property(ce => ce.SampleFrequency).HasMaxLength(200);
            e.Property(ce => ce.ControlMethod).HasMaxLength(500);
            e.Property(ce => ce.ReactionPlan).HasMaxLength(1000);

            e.HasOne(ce => ce.ControlPlan)
                .WithMany(cp => cp.Entries)
                .HasForeignKey(ce => ce.ControlPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ce => ce.ProcessStep)
                .WithMany()
                .HasForeignKey(ce => ce.ProcessStepId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ce => ce.LinkedPfmeaFailureMode)
                .WithMany()
                .HasForeignKey(ce => ce.LinkedPfmeaFailureModeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(ce => ce.LinkedPort)
                .WithMany()
                .HasForeignKey(ce => ce.LinkedPortId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(ce => ce.ControlPlanId);
        });

        // ── Phase 10a: Root Cause Library ────────────────────────────────────

        // --- RootCauseEntry ---
        modelBuilder.Entity<RootCauseEntry>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(200).IsRequired();
            e.Property(r => r.Description).HasMaxLength(2000);
            e.Property(r => r.Category).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Tags).HasMaxLength(500);
            e.Property(r => r.CorrectiveActionTemplate).HasMaxLength(2000);
            e.HasIndex(r => r.Category);
        });

        // ── Phase 10b: Ishikawa Diagrams ─────────────────────────────────────

        // --- IshikawaDiagram ---
        modelBuilder.Entity<IshikawaDiagram>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Title).HasMaxLength(300).IsRequired();
            e.Property(d => d.ProblemStatement).HasMaxLength(2000).IsRequired();
            e.Property(d => d.LinkedEntityType).HasConversion<string>().HasMaxLength(20);
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(10);
            e.Property(d => d.ClosureNotes).HasMaxLength(2000);
            e.Property(d => d.CreatedBy).HasMaxLength(200);
            e.HasIndex(d => d.LinkedEntityId);
        });

        // --- IshikawaCause ---
        modelBuilder.Entity<IshikawaCause>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CauseText).HasMaxLength(500).IsRequired();
            e.Property(c => c.Category).HasConversion<string>().HasMaxLength(20);

            e.HasOne(c => c.Diagram)
                .WithMany(d => d.Causes)
                .HasForeignKey(c => c.DiagramId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.ParentCause)
                .WithMany(c => c.SubCauses)
                .HasForeignKey(c => c.ParentCauseId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.RootCauseLibraryEntry)
                .WithMany()
                .HasForeignKey(c => c.RootCauseLibraryEntryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(c => c.DiagramId);
        });

        // ── Phase 10c: Branching 5 Whys ──────────────────────────────────────

        // --- FiveWhysAnalysis ---
        modelBuilder.Entity<FiveWhysAnalysis>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Title).HasMaxLength(300).IsRequired();
            e.Property(a => a.ProblemStatement).HasMaxLength(2000).IsRequired();
            e.Property(a => a.LinkedEntityType).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(10);
            e.Property(a => a.ClosureNotes).HasMaxLength(2000);
            e.Property(a => a.CreatedBy).HasMaxLength(200);
            e.HasIndex(a => a.LinkedEntityId);
        });

        // --- FiveWhysNode ---
        modelBuilder.Entity<FiveWhysNode>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.WhyStatement).HasMaxLength(1000).IsRequired();
            e.Property(n => n.CorrectiveAction).HasMaxLength(2000);

            e.HasOne(n => n.Analysis)
                .WithMany(a => a.Nodes)
                .HasForeignKey(n => n.AnalysisId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.ParentNode)
                .WithMany(n => n.ChildNodes)
                .HasForeignKey(n => n.ParentNodeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(n => n.RootCauseLibraryEntry)
                .WithMany()
                .HasForeignKey(n => n.RootCauseLibraryEntryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(n => n.AnalysisId);
        });

        // ── Phase 10d: Material Review Board ─────────────────────────────────

        // --- MrbReview ---
        modelBuilder.Entity<MrbReview>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.ItemDescription).HasMaxLength(1000).IsRequired();
            e.Property(m => m.QuantityAffected).HasMaxLength(200);
            e.Property(m => m.ProblemStatement).HasMaxLength(2000).IsRequired();
            e.Property(m => m.DispositionDecision).HasConversion<string>().HasMaxLength(30);
            e.Property(m => m.DispositionJustification).HasMaxLength(2000);
            e.Property(m => m.DecidedBy).HasMaxLength(200);
            e.Property(m => m.LinkedRcaAnalysisType).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.CreatedBy).HasMaxLength(200);

            e.HasOne(m => m.NonConformance)
                .WithMany()
                .HasForeignKey(m => m.NonConformanceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: one MRB per NC
            e.HasIndex(m => m.NonConformanceId).IsUnique();
            e.HasIndex(m => m.Status);
        });

        // --- MrbParticipant ---
        modelBuilder.Entity<MrbParticipant>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.UserId).HasMaxLength(450).IsRequired();
            e.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Role).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.Assessment).HasMaxLength(2000);

            e.HasOne(p => p.MrbReview)
                .WithMany(m => m.Participants)
                .HasForeignKey(p => p.MrbReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => p.MrbReviewId);
        });

        // Update NonConformance to handle MrbReviewId (optional field added in Phase 10d)
        // The FK relationship is owned by MrbReview (HasForeignKey(m => m.NonConformanceId));
        // NonConformance.MrbReviewId is a stored convenience lookup — not a FK constraint.
        modelBuilder.Entity<NonConformance>()
            .Property(nc => nc.MrbReviewId).IsRequired(false);

        // ── Phase 15: Tiered Accountability & Action Tracking ─────────────────

        // --- ActionItem ---
        modelBuilder.Entity<ActionItem>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Title).HasMaxLength(500).IsRequired();
            e.Property(a => a.Description).HasMaxLength(2000);
            e.Property(a => a.AssignedToUserId).HasMaxLength(450).IsRequired();
            e.Property(a => a.AssignedToDisplayName).HasMaxLength(200).IsRequired();
            e.Property(a => a.AssignedByUserId).HasMaxLength(450).IsRequired();
            e.Property(a => a.AssignedByDisplayName).HasMaxLength(200).IsRequired();
            e.Property(a => a.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.SourceType).HasConversion<string>().HasMaxLength(30);
            e.Property(a => a.CompletedBy).HasMaxLength(200);
            e.Property(a => a.CompletionNotes).HasMaxLength(2000);
            e.Property(a => a.VerifiedBy).HasMaxLength(200);
            e.Property(a => a.CreatedBy).HasMaxLength(200);
            e.HasIndex(a => a.AssignedToUserId);
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.DueDate);
            e.HasIndex(a => new { a.SourceType, a.SourceEntityId });
        });

        // --- ManagementReview ---
        modelBuilder.Entity<ManagementReview>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(500).IsRequired();
            e.Property(r => r.ReviewType).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.ConductedBy).HasMaxLength(200);
            e.Property(r => r.NcSummary).HasMaxLength(2000);
            e.Property(r => r.ActionCloseRateSummary).HasMaxLength(500);
            e.Property(r => r.MrbSummary).HasMaxLength(500);
            e.Property(r => r.TrainingComplianceSummary).HasMaxLength(500);
            e.Property(r => r.CustomerComplaintsNotes).HasMaxLength(2000);
            e.Property(r => r.SupplierQualityNotes).HasMaxLength(2000);
            e.Property(r => r.InternalAuditStatus).HasMaxLength(2000);
            e.Property(r => r.PriorActionsSummary).HasMaxLength(2000);
            e.Property(r => r.Decisions).HasMaxLength(4000);
            e.Property(r => r.NextCycleTargets).HasMaxLength(2000);
            e.Property(r => r.CreatedBy).HasMaxLength(200);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.ScheduledDate);
        });

        // ── Phase 16: Training & Competency Management ───────────────────────

        // --- CompetencyRecord ---
        modelBuilder.Entity<CompetencyRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.UserId).HasMaxLength(450).IsRequired();
            e.Property(c => c.UserDisplayName).HasMaxLength(200).IsRequired();
            e.Property(c => c.InstructorUserId).HasMaxLength(450);
            e.Property(c => c.InstructorDisplayName).HasMaxLength(200);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.Property(c => c.CreatedBy).HasMaxLength(200);
            e.HasIndex(c => c.UserId);
            e.HasIndex(c => c.Status);
            e.HasIndex(c => new { c.UserId, c.TrainingProcessId, c.Status });

            e.HasOne(c => c.TrainingProcess)
                .WithMany()
                .HasForeignKey(c => c.TrainingProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Job)
                .WithMany()
                .HasForeignKey(c => c.JobId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- ProcessTrainingRequirement ---
        modelBuilder.Entity<ProcessTrainingRequirement>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.SubjectType).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.CreatedBy).HasMaxLength(200);
            e.HasIndex(r => new { r.SubjectType, r.SubjectEntityId });

            e.HasOne(r => r.RequiredTrainingProcess)
                .WithMany()
                .HasForeignKey(r => r.RequiredTrainingProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var now  = DateTime.UtcNow;
        var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.CreatedBy = user;
                entry.Entity.UpdatedBy = user;
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = user;
            }
        }
    }
}
