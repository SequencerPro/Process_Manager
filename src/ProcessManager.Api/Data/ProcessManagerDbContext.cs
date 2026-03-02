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

    // Phase 4: Workflow Composition
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowProcess> WorkflowProcesses => Set<WorkflowProcess>();
    public DbSet<WorkflowLink> WorkflowLinks => Set<WorkflowLink>();
    public DbSet<WorkflowLinkCondition> WorkflowLinkConditions => Set<WorkflowLinkCondition>();

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
        });

        // --- ProcessStep ---
        modelBuilder.Entity<ProcessStep>(e =>
        {
            e.HasKey(ps => ps.Id);
            e.HasIndex(ps => new { ps.ProcessId, ps.Sequence }).IsUnique();

            e.Property(ps => ps.NameOverride).HasMaxLength(200);

            e.HasOne(ps => ps.Process)
                .WithMany(p => p.ProcessSteps)
                .HasForeignKey(ps => ps.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ps => ps.StepTemplate)
                .WithMany()
                .HasForeignKey(ps => ps.StepTemplateId)
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
            e.HasIndex(wp => new { wp.WorkflowId, wp.ProcessId }).IsUnique();

            e.HasOne(wp => wp.Workflow)
                .WithMany(w => w.WorkflowProcesses)
                .HasForeignKey(wp => wp.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(wp => wp.Process)
                .WithMany()
                .HasForeignKey(wp => wp.ProcessId)
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
