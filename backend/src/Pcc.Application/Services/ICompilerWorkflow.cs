using Pcc.Application.Dtos;

namespace Pcc.Application.Services;

public interface ICompilerWorkflow
{
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct);
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken ct);
    Task<ProjectDto> GetProjectAsync(Guid projectId, CancellationToken ct);
    Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct);
    Task DeleteProjectAsync(Guid projectId, CancellationToken ct);

    Task<ArtifactDto> PasteArtifactAsync(Guid projectId, PasteArtifactRequest request, CancellationToken ct);
    Task<ArtifactDto> UploadArtifactAsync(Guid projectId, Stream stream, string fileName, string? mimeType, CancellationToken ct);
    Task<IReadOnlyList<ArtifactDto>> GetArtifactsAsync(Guid projectId, CancellationToken ct);
    Task<ArtifactDto> GetArtifactAsync(Guid artifactId, CancellationToken ct);
    Task DeleteArtifactAsync(Guid artifactId, CancellationToken ct);

    Task<IReadOnlyList<ContentBlockDto>> ReadArtifactAsync(Guid artifactId, CancellationToken ct);
    Task<IReadOnlyList<ContentBlockDto>> GetContentBlocksAsync(Guid artifactId, CancellationToken ct);
    Task<IReadOnlyList<ContentBlockDto>> GetProjectContentBlocksAsync(Guid projectId, CancellationToken ct);

    Task<IReadOnlyList<ClaimDto>> ExtractClaimsAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<ClaimDto>> GetClaimsAsync(Guid projectId, CancellationToken ct);
    Task<ClaimDto> GetClaimAsync(Guid claimId, CancellationToken ct);
    Task<ClaimDto> IgnoreClaimAsync(Guid claimId, CancellationToken ct);
    Task<ClaimDto> RestoreClaimAsync(Guid claimId, CancellationToken ct);

    Task<IReadOnlyList<RequirementDto>> SynthesizeRequirementsAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<RequirementDto>> GetRequirementsAsync(Guid projectId, CancellationToken ct);
    Task<RequirementDto> GetRequirementAsync(Guid requirementId, CancellationToken ct);
    Task<RequirementDto> UpdateRequirementAsync(Guid requirementId, UpdateRequirementRequest request, CancellationToken ct);
    Task<RequirementDto> ReviewRequirementAsync(Guid requirementId, ReviewRequirementRequest request, CancellationToken ct);

    Task<IReadOnlyList<OrchestrationTaskDto>> GenerateTasksAsync(Guid requirementId, CancellationToken ct);
    Task<IReadOnlyList<OrchestrationTaskDto>> GetTasksAsync(Guid projectId, CancellationToken ct);
    Task<OrchestrationTaskDto> GetTaskAsync(Guid taskId, CancellationToken ct);
    Task<OrchestrationTaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct);
    Task<OrchestrationTaskDto> CancelTaskAsync(Guid taskId, CancellationToken ct);
    Task<IReadOnlyList<TaskDependencyDto>> GetTaskDependenciesAsync(Guid projectId, CancellationToken ct);

    Task<ExportBundleDto> ExportSymphonyMarkdownAsync(Guid projectId, CancellationToken ct);
    Task<IReadOnlyList<ExportBundleDto>> GetExportsAsync(Guid projectId, CancellationToken ct);
    Task<ExportBundleDto> GetExportAsync(Guid exportId, CancellationToken ct);

    Task<IReadOnlyList<ModelRunDto>> GetModelRunsAsync(Guid projectId, CancellationToken ct);
    Task<ModelRunDto> GetModelRunAsync(Guid modelRunId, CancellationToken ct);
}
