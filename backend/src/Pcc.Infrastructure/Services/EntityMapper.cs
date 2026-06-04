using Pcc.Application.Dtos;
using Pcc.Domain;
using Pcc.Infrastructure.Json;

namespace Pcc.Infrastructure.Services;

public static class EntityMapper
{
    public static ArtifactDto ToDto(this Artifact artifact)
    {
        return new ArtifactDto(
            artifact.Id,
            artifact.ProjectId,
            artifact.Title,
            artifact.Type,
            artifact.OriginalFileName,
            artifact.MimeType,
            artifact.FileExtension,
            artifact.StoragePath,
            artifact.Sha256,
            artifact.RawText,
            artifact.ReadStatus,
            artifact.ReadError,
            artifact.CreatedAt);
    }

    public static ContentBlockDto ToDto(this ContentBlock block)
    {
        return new ContentBlockDto(
            block.Id,
            block.ProjectId,
            block.ArtifactId,
            block.Type,
            block.Text,
            block.LocationLabel,
            block.OrderIndex,
            block.Confidence,
            block.MetadataJson,
            block.CreatedAt);
    }

    public static ClaimDto ToDto(this Claim claim)
    {
        return new ClaimDto(
            claim.Id,
            claim.ProjectId,
            claim.Type,
            claim.Subject,
            claim.Text,
            claim.Confidence,
            claim.AmbiguityScore,
            claim.FreshnessScore,
            claim.SourceAuthorityScore,
            claim.Status,
            claim.CreatedAt,
            claim.Evidence.Select(evidence => evidence.ToDto()).ToArray());
    }

    public static ClaimEvidenceDto ToDto(this ClaimEvidence evidence)
    {
        return new ClaimEvidenceDto(
            evidence.Id,
            evidence.ClaimId,
            evidence.ContentBlockId,
            evidence.EvidenceText);
    }

    public static RequirementDto ToDto(this Requirement requirement)
    {
        var current = requirement.CurrentVersionEntity ?? requirement.Versions
            .OrderByDescending(version => version.Version)
            .FirstOrDefault();
        return new RequirementDto(
            requirement.Id,
            requirement.ProjectId,
            requirement.Title,
            requirement.Summary,
            requirement.Module,
            requirement.RequirementType,
            requirement.Status,
            requirement.Confidence,
            requirement.CurrentVersion,
            current?.Id,
            current?.GetContent() ?? RequirementContent.Empty,
            requirement.CreatedAt,
            requirement.UpdatedAt,
            requirement.Versions.OrderBy(version => version.Version).Select(version => version.ToDto()).ToArray(),
            requirement.Reviews.OrderBy(review => review.CreatedAt).Select(review => review.ToDto()).ToArray());
    }

    public static RequirementVersionDto ToDto(this RequirementVersion version)
    {
        return new RequirementVersionDto(
            version.Id,
            version.RequirementId,
            version.Version,
            version.GetContent(),
            version.ChangeReason,
            version.CreatedBy,
            version.CreatedAt);
    }

    public static RequirementReviewDto ToDto(this RequirementReview review)
    {
        return new RequirementReviewDto(
            review.Id,
            review.RequirementId,
            review.RequirementVersionId,
            review.Decision,
            review.ReviewerName,
            review.Comment,
            review.CreatedAt);
    }

    public static OrchestrationTaskDto ToDto(this OrchestrationTask task)
    {
        return new OrchestrationTaskDto(
            task.Id,
            task.ProjectId,
            task.RequirementId,
            task.RequirementVersionId,
            task.Title,
            task.Description,
            task.TaskType,
            task.Status,
            task.ReadinessScore,
            task.AgentPrompt,
            JsonDefaults.DeserializeStringList(task.AcceptanceCriteriaJson),
            JsonDefaults.DeserializeStringList(task.EvidenceJson),
            JsonDefaults.DeserializeStringList(task.MissingContextJson),
            JsonDefaults.DeserializeStringList(task.AssumptionsJson),
            task.CreatedAt);
    }

    public static TaskDependencyDto ToDto(this TaskDependency dependency)
    {
        return new TaskDependencyDto(
            dependency.Id,
            dependency.UpstreamTaskId,
            dependency.DownstreamTaskId,
            dependency.DependencyType);
    }

    public static ExportBundleDto ToDto(this ExportBundle bundle)
    {
        return new ExportBundleDto(
            bundle.Id,
            bundle.ProjectId,
            bundle.Format,
            bundle.Content,
            bundle.CreatedAt);
    }

    public static ModelRunDto ToDto(this ModelRun run)
    {
        return new ModelRunDto(
            run.Id,
            run.ProjectId,
            run.Purpose,
            run.ProviderName,
            run.ModelName,
            run.WorkingDirectory,
            run.InputJson,
            run.OutputJson,
            run.RawOutput,
            run.Status,
            run.Error,
            run.CreatedAt,
            run.FinishedAt);
    }
}
