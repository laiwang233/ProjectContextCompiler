using System.Text.Json;

namespace Pcc.Domain;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Artifact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public ArtifactType Type { get; set; }
    public string? OriginalFileName { get; set; }
    public string? MimeType { get; set; }
    public string? FileExtension { get; set; }
    public string? StoragePath { get; set; }
    public string? Sha256 { get; set; }
    public string? RawText { get; set; }
    public ArtifactReadStatus ReadStatus { get; set; } = ArtifactReadStatus.Uploaded;
    public string? ReadError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ContentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid ArtifactId { get; set; }
    public ContentBlockType Type { get; set; }
    public string Text { get; set; } = "";
    public string? LocationLabel { get; set; }
    public int OrderIndex { get; set; }
    public decimal Confidence { get; set; }
    public string? MetadataJson { get; set; }
    public ContentBlockVerificationStatus VerificationStatus { get; set; } = ContentBlockVerificationStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void Review(ContentBlockVerificationStatus status, string reviewerName)
    {
        if (status is not (ContentBlockVerificationStatus.Confirmed or ContentBlockVerificationStatus.Ignored))
        {
            throw new DomainRuleViolationException("ContentBlock review status must be Confirmed or Ignored.");
        }

        VerificationStatus = status;
        ReviewedBy = string.IsNullOrWhiteSpace(reviewerName) ? "PM" : reviewerName.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class Claim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ClaimType Type { get; set; }
    public string Subject { get; set; } = "";
    public string Text { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal AmbiguityScore { get; set; }
    public decimal FreshnessScore { get; set; }
    public decimal SourceAuthorityScore { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Extracted;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ClaimEvidence> Evidence { get; set; } = [];
}

public sealed class ClaimEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public Guid ContentBlockId { get; set; }
    public string EvidenceText { get; set; } = "";
}

public sealed class Requirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Module { get; set; }
    public RequirementType RequirementType { get; set; } = RequirementType.Unknown;
    public RequirementStatus Status { get; set; } = RequirementStatus.PendingReview;
    public decimal Confidence { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<RequirementVersion> Versions { get; set; } = [];
    public List<RequirementReview> Reviews { get; set; } = [];

    public RequirementVersion? CurrentVersionEntity => Versions
        .OrderByDescending(version => version.Version)
        .FirstOrDefault(version => version.Version == CurrentVersion);

    public Guid? CurrentVersionId => CurrentVersionEntity?.Id;

    public static Requirement CreateSynthesized(
        Guid projectId,
        string title,
        string summary,
        string? module,
        RequirementType requirementType,
        decimal confidence,
        RequirementContent content,
        string createdBy)
    {
        var requirement = new Requirement
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Summary = summary.Trim(),
            Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim(),
            RequirementType = requirementType,
            Status = RequirementStatus.PendingReview,
            Confidence = confidence,
            CurrentVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        requirement.Versions.Add(RequirementVersion.Create(
            requirement.Id,
            1,
            content,
            "LLM synthesis",
            createdBy));
        return requirement;
    }

    public RequirementVersion ResetToPendingReviewForNewVersion(
        RequirementContent content,
        string? changeReason,
        string createdBy)
    {
        CurrentVersion += 1;
        Status = RequirementStatus.PendingReview;
        UpdatedAt = DateTimeOffset.UtcNow;

        var version = RequirementVersion.Create(
            Id,
            CurrentVersion,
            content,
            changeReason,
            createdBy);
        Versions.Add(version);
        return version;
    }

    public RequirementReview ApplyReview(
        Guid requirementVersionId,
        RequirementReviewDecision decision,
        string reviewerName,
        string? comment)
    {
        if (Versions.All(version => version.Id != requirementVersionId))
        {
            throw new DomainRuleViolationException("审核必须绑定具体 RequirementVersion");
        }

        Status = decision switch
        {
            RequirementReviewDecision.Approve => RequirementStatus.Approved,
            RequirementReviewDecision.ApproveWithAssumptions => RequirementStatus.Approved,
            RequirementReviewDecision.RequestClarification => RequirementStatus.NeedsClarification,
            RequirementReviewDecision.Reject => RequirementStatus.Rejected,
            RequirementReviewDecision.Defer => RequirementStatus.Deferred,
            _ => throw new DomainRuleViolationException($"Unsupported review decision: {decision}")
        };
        UpdatedAt = DateTimeOffset.UtcNow;

        var review = new RequirementReview
        {
            RequirementId = Id,
            RequirementVersionId = requirementVersionId,
            Decision = decision,
            ReviewerName = reviewerName.Trim(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        Reviews.Add(review);
        return review;
    }
}

public sealed class RequirementVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequirementId { get; set; }
    public int Version { get; set; }
    public string ContentJson { get; set; } = "";
    public string? ChangeReason { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static RequirementVersion Create(
        Guid requirementId,
        int version,
        RequirementContent content,
        string? changeReason,
        string createdBy)
    {
        return new RequirementVersion
        {
            RequirementId = requirementId,
            Version = version,
            ContentJson = JsonSerializer.Serialize(content, DomainJson.Options),
            ChangeReason = string.IsNullOrWhiteSpace(changeReason) ? null : changeReason.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public RequirementContent GetContent()
    {
        return JsonSerializer.Deserialize<RequirementContent>(ContentJson, DomainJson.Options)
            ?? RequirementContent.Empty;
    }
}

public sealed class RequirementReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequirementId { get; set; }
    public Guid RequirementVersionId { get; set; }
    public RequirementReviewDecision Decision { get; set; }
    public string ReviewerName { get; set; } = "";
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RequirementConflict
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? RequirementId { get; set; }
    public string Description { get; set; } = "";
    public string? ProposedResolution { get; set; }
    public ConflictStatus Status { get; set; } = ConflictStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OrchestrationTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid RequirementId { get; set; }
    public Guid RequirementVersionId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public OrchestrationTaskType TaskType { get; set; }
    public OrchestrationTaskStatus Status { get; set; } = OrchestrationTaskStatus.Preview;
    public decimal ReadinessScore { get; set; }
    public string AgentPrompt { get; set; } = "";
    public string AcceptanceCriteriaJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "[]";
    public string MissingContextJson { get; set; } = "[]";
    public string AssumptionsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static OrchestrationTask CreateFromRequirement(
        Requirement requirement,
        RequirementVersion version,
        string title,
        string description,
        OrchestrationTaskType taskType,
        decimal readinessScore,
        string agentPrompt,
        string acceptanceCriteriaJson,
        string evidenceJson,
        string missingContextJson,
        string assumptionsJson)
    {
        if (requirement.Status != RequirementStatus.Approved)
        {
            throw new RequirementNotApprovedException(requirement.Id);
        }

        return new OrchestrationTask
        {
            ProjectId = requirement.ProjectId,
            RequirementId = requirement.Id,
            RequirementVersionId = version.Id,
            Title = title.Trim(),
            Description = description.Trim(),
            TaskType = taskType,
            Status = OrchestrationTaskStatus.ReadyForExport,
            ReadinessScore = readinessScore,
            AgentPrompt = agentPrompt.Trim(),
            AcceptanceCriteriaJson = acceptanceCriteriaJson,
            EvidenceJson = evidenceJson,
            MissingContextJson = missingContextJson,
            AssumptionsJson = assumptionsJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class TaskDependency
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UpstreamTaskId { get; set; }
    public Guid DownstreamTaskId { get; set; }
    public string DependencyType { get; set; } = "blocks";
}

public sealed class ExportBundle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ExportFormat Format { get; set; }
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ModelRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Purpose { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string? ModelName { get; set; }
    public string? WorkingDirectory { get; set; }
    public string InputJson { get; set; } = "";
    public string? OutputJson { get; set; }
    public string? RawOutput { get; set; }
    public ModelRunStatus Status { get; set; } = ModelRunStatus.Pending;
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}

public static class DomainJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
