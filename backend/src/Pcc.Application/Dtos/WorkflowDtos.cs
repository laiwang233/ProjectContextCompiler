using Pcc.Domain;

namespace Pcc.Application.Dtos;

public sealed record CreateProjectRequest(string Name, string? Description);

public sealed record UpdateProjectRequest(string Name, string? Description);

public sealed record PasteArtifactRequest(
    string Title,
    string Text,
    ArtifactType Type = ArtifactType.PastedText);

public sealed record ReviewRequirementRequest(
    RequirementReviewDecision Decision,
    string ReviewerName,
    string? Comment);

public sealed record UpdateRequirementRequest(
    string Title,
    string Summary,
    string? Module,
    RequirementType RequirementType,
    RequirementContent Content,
    string ChangeReason,
    string CreatedBy);

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    OrchestrationTaskType TaskType,
    decimal ReadinessScore,
    string AgentPrompt,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> MissingContext,
    IReadOnlyList<string> Assumptions);

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ProjectStatsDto Stats);

public sealed record ProjectStatsDto(
    int ArtifactCount,
    int ContentBlockCount,
    int ClaimCount,
    int PendingReviewRequirementCount,
    int ApprovedRequirementCount,
    int TaskCount,
    DateTimeOffset? LastExportedAt);

public sealed record ArtifactDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    ArtifactType Type,
    string? OriginalFileName,
    string? MimeType,
    string? FileExtension,
    string? StoragePath,
    string? Sha256,
    string? RawText,
    ArtifactReadStatus ReadStatus,
    string? ReadError,
    DateTimeOffset CreatedAt);

public sealed record ContentBlockDto(
    Guid Id,
    Guid ProjectId,
    Guid ArtifactId,
    ContentBlockType Type,
    string Text,
    string? LocationLabel,
    int OrderIndex,
    decimal Confidence,
    string? MetadataJson,
    DateTimeOffset CreatedAt);

public sealed record ClaimEvidenceDto(
    Guid Id,
    Guid ClaimId,
    Guid ContentBlockId,
    string EvidenceText);

public sealed record ClaimDto(
    Guid Id,
    Guid ProjectId,
    ClaimType Type,
    string Subject,
    string Text,
    decimal Confidence,
    decimal AmbiguityScore,
    decimal FreshnessScore,
    decimal SourceAuthorityScore,
    ClaimStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ClaimEvidenceDto> Evidence);

public sealed record RequirementDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Summary,
    string? Module,
    RequirementType RequirementType,
    RequirementStatus Status,
    decimal Confidence,
    int CurrentVersion,
    Guid? CurrentVersionId,
    RequirementContent Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<RequirementVersionDto> Versions,
    IReadOnlyList<RequirementReviewDto> Reviews);

public sealed record RequirementVersionDto(
    Guid Id,
    Guid RequirementId,
    int Version,
    RequirementContent Content,
    string? ChangeReason,
    string CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record RequirementReviewDto(
    Guid Id,
    Guid RequirementId,
    Guid RequirementVersionId,
    RequirementReviewDecision Decision,
    string ReviewerName,
    string? Comment,
    DateTimeOffset CreatedAt);

public sealed record OrchestrationTaskDto(
    Guid Id,
    Guid ProjectId,
    Guid RequirementId,
    Guid RequirementVersionId,
    string Title,
    string Description,
    OrchestrationTaskType TaskType,
    OrchestrationTaskStatus Status,
    decimal ReadinessScore,
    string AgentPrompt,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> MissingContext,
    IReadOnlyList<string> Assumptions,
    DateTimeOffset CreatedAt);

public sealed record TaskDependencyDto(
    Guid Id,
    Guid UpstreamTaskId,
    Guid DownstreamTaskId,
    string DependencyType);

public sealed record ExportBundleDto(
    Guid Id,
    Guid ProjectId,
    ExportFormat Format,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record ModelRunDto(
    Guid Id,
    Guid ProjectId,
    string Purpose,
    string ProviderName,
    string? ModelName,
    string? WorkingDirectory,
    string InputJson,
    string? OutputJson,
    string? RawOutput,
    ModelRunStatus Status,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt);
