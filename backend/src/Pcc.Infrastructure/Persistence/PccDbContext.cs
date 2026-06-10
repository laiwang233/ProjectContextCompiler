using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pcc.Domain;

namespace Pcc.Infrastructure.Persistence;

public sealed class PccDbContext(DbContextOptions<PccDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimEvidence> ClaimEvidence => Set<ClaimEvidence>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<RequirementVersion> RequirementVersions => Set<RequirementVersion>();
    public DbSet<RequirementReview> RequirementReviews => Set<RequirementReview>();
    public DbSet<RequirementConflict> RequirementConflicts => Set<RequirementConflict>();
    public DbSet<OrchestrationTask> OrchestrationTasks => Set<OrchestrationTask>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<ExportBundle> ExportBundles => Set<ExportBundle>();
    public DbSet<ModelRun> ModelRuns => Set<ModelRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureEnums(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Artifact>(entity =>
        {
            entity.ToTable("artifacts");
            entity.HasKey(artifact => artifact.Id);
            entity.Property(artifact => artifact.Title).HasMaxLength(300).IsRequired();
            entity.Property(artifact => artifact.RawText).HasColumnType("text");
            entity.Property(artifact => artifact.ReadError).HasColumnType("text");
            entity.HasIndex(artifact => artifact.ProjectId);
            entity.HasIndex(artifact => new { artifact.ProjectId, artifact.CreatedAt });
            entity.HasIndex(artifact => new { artifact.ProjectId, artifact.ReadStatus });
            entity.HasIndex(artifact => new { artifact.ProjectId, artifact.Type });
        });

        modelBuilder.Entity<ContentBlock>(entity =>
        {
            entity.ToTable("content_blocks");
            entity.HasKey(block => block.Id);
            entity.Property(block => block.Text).HasColumnType("text").IsRequired();
            entity.Property(block => block.MetadataJson).HasColumnType("text");
            entity.Property(block => block.ReviewedBy).HasMaxLength(160);
            entity.HasIndex(block => block.ProjectId);
            entity.HasIndex(block => block.ArtifactId);
            entity.HasIndex(block => new { block.ProjectId, block.CreatedAt });
            entity.HasIndex(block => new { block.ProjectId, block.Type });
            entity.HasIndex(block => new { block.ProjectId, block.VerificationStatus });
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.ToTable("claims");
            entity.HasKey(claim => claim.Id);
            entity.Property(claim => claim.Subject).HasMaxLength(300).IsRequired();
            entity.Property(claim => claim.Text).HasColumnType("text").IsRequired();
            entity.HasMany(claim => claim.Evidence)
                .WithOne()
                .HasForeignKey(evidence => evidence.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(claim => claim.ProjectId);
            entity.HasIndex(claim => new { claim.ProjectId, claim.CreatedAt });
            entity.HasIndex(claim => new { claim.ProjectId, claim.Status });
            entity.HasIndex(claim => new { claim.ProjectId, claim.Type });
        });

        modelBuilder.Entity<ClaimEvidence>(entity =>
        {
            entity.ToTable("claim_evidence");
            entity.HasKey(evidence => evidence.Id);
            entity.Property(evidence => evidence.EvidenceText).HasColumnType("text").IsRequired();
            entity.HasIndex(evidence => evidence.ContentBlockId);
        });

        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.ToTable("requirements");
            entity.HasKey(requirement => requirement.Id);
            entity.Property(requirement => requirement.Title).HasMaxLength(300).IsRequired();
            entity.Property(requirement => requirement.Summary).HasColumnType("text").IsRequired();
            entity.Ignore(requirement => requirement.CurrentVersionEntity);
            entity.Ignore(requirement => requirement.CurrentVersionId);
            entity.HasMany(requirement => requirement.Versions)
                .WithOne()
                .HasForeignKey(version => version.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(requirement => requirement.Reviews)
                .WithOne()
                .HasForeignKey(review => review.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(requirement => requirement.ProjectId);
            entity.HasIndex(requirement => new { requirement.ProjectId, requirement.UpdatedAt });
            entity.HasIndex(requirement => new { requirement.ProjectId, requirement.Status });
            entity.HasIndex(requirement => new { requirement.ProjectId, requirement.RequirementType });
        });

        modelBuilder.Entity<RequirementVersion>(entity =>
        {
            entity.ToTable("requirement_versions");
            entity.HasKey(version => version.Id);
            entity.Property(version => version.ContentJson).HasColumnType("text").IsRequired();
        });

        modelBuilder.Entity<RequirementReview>(entity =>
        {
            entity.ToTable("requirement_reviews");
            entity.HasKey(review => review.Id);
            entity.Property(review => review.ReviewerName).HasMaxLength(160).IsRequired();
        });

        modelBuilder.Entity<RequirementConflict>(entity =>
        {
            entity.ToTable("requirement_conflicts");
            entity.HasKey(conflict => conflict.Id);
            entity.Property(conflict => conflict.Description).HasColumnType("text").IsRequired();
            entity.Property(conflict => conflict.ProposedResolution).HasColumnType("text");
        });

        modelBuilder.Entity<OrchestrationTask>(entity =>
        {
            entity.ToTable("orchestration_tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(300).IsRequired();
            entity.Property(task => task.Description).HasColumnType("text").IsRequired();
            entity.Property(task => task.AgentPrompt).HasColumnType("text").IsRequired();
            entity.Property(task => task.AcceptanceCriteriaJson).HasColumnType("text").IsRequired();
            entity.Property(task => task.EvidenceJson).HasColumnType("text").IsRequired();
            entity.Property(task => task.MissingContextJson).HasColumnType("text").IsRequired();
            entity.Property(task => task.AssumptionsJson).HasColumnType("text").IsRequired();
            entity.HasIndex(task => task.ProjectId);
            entity.HasIndex(task => task.RequirementId);
            entity.HasIndex(task => new { task.ProjectId, task.CreatedAt });
            entity.HasIndex(task => new { task.ProjectId, task.Status });
            entity.HasIndex(task => new { task.ProjectId, task.TaskType });
        });

        modelBuilder.Entity<TaskDependency>(entity =>
        {
            entity.ToTable("task_dependencies");
            entity.HasKey(dependency => dependency.Id);
            entity.Property(dependency => dependency.DependencyType).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<ExportBundle>(entity =>
        {
            entity.ToTable("export_bundles");
            entity.HasKey(bundle => bundle.Id);
            entity.Property(bundle => bundle.Content).HasColumnType("text").IsRequired();
            entity.HasIndex(bundle => new { bundle.ProjectId, bundle.CreatedAt });
            entity.HasIndex(bundle => new { bundle.ProjectId, bundle.Format });
        });

        modelBuilder.Entity<ModelRun>(entity =>
        {
            entity.ToTable("model_runs");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Purpose).HasMaxLength(160).IsRequired();
            entity.Property(run => run.ProviderName).HasMaxLength(160).IsRequired();
            entity.Property(run => run.InputJson).HasColumnType("text").IsRequired();
            entity.Property(run => run.OutputJson).HasColumnType("text");
            entity.Property(run => run.RawOutput).HasColumnType("text");
            entity.Property(run => run.Error).HasColumnType("text");
            entity.HasIndex(run => run.ProjectId);
            entity.HasIndex(run => new { run.ProjectId, run.CreatedAt });
            entity.HasIndex(run => new { run.ProjectId, run.Status });
        });
    }

    private static void ConfigureEnums(ModelBuilder modelBuilder)
    {
        var artifactType = new EnumToStringConverter<ArtifactType>();
        var readStatus = new EnumToStringConverter<ArtifactReadStatus>();
        var blockType = new EnumToStringConverter<ContentBlockType>();
        var blockVerificationStatus = new EnumToStringConverter<ContentBlockVerificationStatus>();
        var claimType = new EnumToStringConverter<ClaimType>();
        var claimStatus = new EnumToStringConverter<ClaimStatus>();
        var requirementType = new EnumToStringConverter<RequirementType>();
        var requirementStatus = new EnumToStringConverter<RequirementStatus>();
        var reviewDecision = new EnumToStringConverter<RequirementReviewDecision>();
        var conflictStatus = new EnumToStringConverter<ConflictStatus>();
        var taskType = new EnumToStringConverter<OrchestrationTaskType>();
        var taskStatus = new EnumToStringConverter<OrchestrationTaskStatus>();
        var exportFormat = new EnumToStringConverter<ExportFormat>();
        var runStatus = new EnumToStringConverter<ModelRunStatus>();

        modelBuilder.Entity<Artifact>().Property(entity => entity.Type).HasConversion(artifactType);
        modelBuilder.Entity<Artifact>().Property(entity => entity.ReadStatus).HasConversion(readStatus);
        modelBuilder.Entity<ContentBlock>().Property(entity => entity.Type).HasConversion(blockType);
        modelBuilder.Entity<ContentBlock>().Property(entity => entity.VerificationStatus).HasConversion(blockVerificationStatus);
        modelBuilder.Entity<Claim>().Property(entity => entity.Type).HasConversion(claimType);
        modelBuilder.Entity<Claim>().Property(entity => entity.Status).HasConversion(claimStatus);
        modelBuilder.Entity<Requirement>().Property(entity => entity.RequirementType).HasConversion(requirementType);
        modelBuilder.Entity<Requirement>().Property(entity => entity.Status).HasConversion(requirementStatus);
        modelBuilder.Entity<RequirementReview>().Property(entity => entity.Decision).HasConversion(reviewDecision);
        modelBuilder.Entity<RequirementConflict>().Property(entity => entity.Status).HasConversion(conflictStatus);
        modelBuilder.Entity<OrchestrationTask>().Property(entity => entity.TaskType).HasConversion(taskType);
        modelBuilder.Entity<OrchestrationTask>().Property(entity => entity.Status).HasConversion(taskStatus);
        modelBuilder.Entity<ExportBundle>().Property(entity => entity.Format).HasConversion(exportFormat);
        modelBuilder.Entity<ModelRun>().Property(entity => entity.Status).HasConversion(runStatus);
    }
}
