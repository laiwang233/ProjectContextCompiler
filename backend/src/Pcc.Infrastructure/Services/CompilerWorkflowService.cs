using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pcc.Application.Dtos;
using Pcc.Application.Generation;
using Pcc.Application.Services;
using Pcc.Domain;
using Pcc.Infrastructure.Export;
using Pcc.Infrastructure.Generation;
using Pcc.Infrastructure.Json;
using Pcc.Infrastructure.Persistence;
using Pcc.Infrastructure.Storage;

namespace Pcc.Infrastructure.Services;

public sealed class CompilerWorkflowService(
    PccDbContext db,
    IStructuredGenerationProvider provider,
    IFileStorage fileStorage,
    IJsonSchemaValidator? schemaValidator = null) : ICompilerWorkflow
{
    private readonly IJsonSchemaValidator _schemaValidator = schemaValidator ?? new JsonSchemaValidator();

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Project name is required.", nameof(request));
        }

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return await ToProjectDtoAsync(project, ct);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken ct)
    {
        var projects = await db.Projects.OrderByDescending(project => project.UpdatedAt).ToListAsync(ct);
        var dtos = new List<ProjectDto>();
        foreach (var project in projects)
        {
            dtos.Add(await ToProjectDtoAsync(project, ct));
        }

        return dtos;
    }

    public async Task<ProjectDto> GetProjectAsync(Guid projectId, CancellationToken ct)
    {
        var project = await FindProjectAsync(projectId, ct);
        return await ToProjectDtoAsync(project, ct);
    }

    public async Task<ProjectDto> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await FindProjectAsync(projectId, ct);
        project.Name = request.Name.Trim();
        project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await ToProjectDtoAsync(project, ct);
    }

    public async Task DeleteProjectAsync(Guid projectId, CancellationToken ct)
    {
        var project = await FindProjectAsync(projectId, ct);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ArtifactDto> PasteArtifactAsync(Guid projectId, PasteArtifactRequest request, CancellationToken ct)
    {
        await EnsureProjectExistsAsync(projectId, ct);
        var artifact = new Artifact
        {
            ProjectId = projectId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "粘贴文本" : request.Title.Trim(),
            Type = request.Type,
            RawText = request.Text,
            MimeType = "text/plain",
            FileExtension = ".txt",
            ReadStatus = ArtifactReadStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Artifacts.Add(artifact);
        await TouchProjectAsync(projectId, ct);
        await db.SaveChangesAsync(ct);
        return artifact.ToDto();
    }

    public async Task<ArtifactDto> UploadArtifactAsync(Guid projectId, Stream stream, string fileName, string? mimeType, CancellationToken ct)
    {
        await EnsureProjectExistsAsync(projectId, ct);
        var stored = await fileStorage.SaveAsync(stream, fileName, ct);
        var artifact = new Artifact
        {
            ProjectId = projectId,
            Title = Path.GetFileNameWithoutExtension(fileName),
            Type = DetectArtifactType(stored.FileExtension),
            OriginalFileName = fileName,
            MimeType = mimeType,
            FileExtension = stored.FileExtension,
            StoragePath = stored.StoragePath,
            Sha256 = stored.Sha256,
            RawText = await fileStorage.TryReadTextAsync(stored.StoragePath, ct),
            ReadStatus = ArtifactReadStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Artifacts.Add(artifact);
        await TouchProjectAsync(projectId, ct);
        await db.SaveChangesAsync(ct);
        return artifact.ToDto();
    }

    public async Task<IReadOnlyList<ArtifactDto>> GetArtifactsAsync(Guid projectId, CancellationToken ct)
    {
        return await db.Artifacts
            .Where(artifact => artifact.ProjectId == projectId)
            .OrderByDescending(artifact => artifact.CreatedAt)
            .Select(artifact => artifact.ToDto())
            .ToListAsync(ct);
    }

    public async Task<ArtifactDto> GetArtifactAsync(Guid artifactId, CancellationToken ct)
    {
        return (await FindArtifactAsync(artifactId, ct)).ToDto();
    }

    public async Task DeleteArtifactAsync(Guid artifactId, CancellationToken ct)
    {
        var artifact = await FindArtifactAsync(artifactId, ct);
        db.Artifacts.Remove(artifact);
        await TouchProjectAsync(artifact.ProjectId, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ContentBlockDto>> ReadArtifactAsync(Guid artifactId, CancellationToken ct)
    {
        var artifact = await FindArtifactAsync(artifactId, ct);
        var existing = await db.ContentBlocks
            .Where(block => block.ArtifactId == artifactId)
            .OrderBy(block => block.OrderIndex)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            return existing.Select(block => block.ToDto()).ToArray();
        }

        artifact.ReadStatus = ArtifactReadStatus.Reading;
        await db.SaveChangesAsync(ct);

        var rawText = artifact.RawText;
        if (string.IsNullOrWhiteSpace(rawText) && artifact.StoragePath is not null)
        {
            rawText = await fileStorage.TryReadTextAsync(artifact.StoragePath, ct);
        }

        var input = JsonDefaults.Serialize(new ArtifactReadInput(
            artifact.ProjectId,
            artifact.Id,
            artifact.Title,
            artifact.OriginalFileName,
            rawText));
        var resultJson = await GenerateAndRecordAsync(
            artifact.ProjectId,
            GenerationPurposes.ArtifactRead,
            input,
            GenerationSchemas.ContentBlocks,
            ct);

        var envelope = JsonDefaults.Deserialize<ContentBlocksEnvelope>(resultJson)
            ?? new ContentBlocksEnvelope([]);
        var blocks = envelope.ContentBlocks.Select(draft => new ContentBlock
        {
            ProjectId = artifact.ProjectId,
            ArtifactId = artifact.Id,
            Type = Enum.TryParse<ContentBlockType>(draft.Type, ignoreCase: true, out var type) ? type : ContentBlockType.Unknown,
            Text = draft.Text,
            LocationLabel = draft.LocationLabel ?? "位置不明",
            OrderIndex = draft.OrderIndex,
            Confidence = draft.Confidence,
            MetadataJson = JsonDefaults.Serialize(draft.Metadata),
            CreatedAt = DateTimeOffset.UtcNow
        }).ToArray();

        db.ContentBlocks.AddRange(blocks);
        artifact.ReadStatus = ArtifactReadStatus.Read;
        artifact.ReadError = null;
        await TouchProjectAsync(artifact.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return blocks.Select(block => block.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<ContentBlockDto>> GetContentBlocksAsync(Guid artifactId, CancellationToken ct)
    {
        return await db.ContentBlocks
            .Where(block => block.ArtifactId == artifactId)
            .OrderBy(block => block.OrderIndex)
            .Select(block => block.ToDto())
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ContentBlockDto>> GetProjectContentBlocksAsync(Guid projectId, CancellationToken ct)
    {
        return await db.ContentBlocks
            .Where(block => block.ProjectId == projectId)
            .OrderBy(block => block.CreatedAt)
            .ThenBy(block => block.OrderIndex)
            .Select(block => block.ToDto())
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ClaimDto>> ExtractClaimsAsync(Guid projectId, CancellationToken ct)
    {
        var existing = await db.Claims
            .Include(claim => claim.Evidence)
            .Where(claim => claim.ProjectId == projectId)
            .OrderBy(claim => claim.CreatedAt)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            return existing.Select(claim => claim.ToDto()).ToArray();
        }

        var blocks = await db.ContentBlocks
            .Where(block => block.ProjectId == projectId)
            .OrderBy(block => block.OrderIndex)
            .ToListAsync(ct);
        if (blocks.Count == 0)
        {
            return [];
        }

        var input = JsonDefaults.Serialize(new ClaimExtractionInput(projectId)
        {
            ContentBlocks = blocks.Select(block => new ContentBlockInput(
                block.Id,
                block.OrderIndex,
                block.Text,
                block.LocationLabel)).ToArray()
        });
        var resultJson = await GenerateAndRecordAsync(
            projectId,
            GenerationPurposes.ClaimExtraction,
            input,
            GenerationSchemas.Claims,
            ct);
        var envelope = JsonDefaults.Deserialize<ClaimsEnvelope>(resultJson)
            ?? new ClaimsEnvelope([]);
        var blockById = blocks.ToDictionary(block => block.Id);
        var claims = envelope.Claims
            .Where(draft => draft.ContentBlockIds.Count > 0)
            .Select(draft => ToClaim(projectId, draft, blockById))
            .ToArray();

        db.Claims.AddRange(claims);
        await TouchProjectAsync(projectId, ct);
        await db.SaveChangesAsync(ct);
        return claims.Select(claim => claim.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<ClaimDto>> GetClaimsAsync(Guid projectId, CancellationToken ct)
    {
        return await db.Claims
            .Include(claim => claim.Evidence)
            .Where(claim => claim.ProjectId == projectId)
            .OrderBy(claim => claim.CreatedAt)
            .Select(claim => claim.ToDto())
            .ToListAsync(ct);
    }

    public async Task<ClaimDto> GetClaimAsync(Guid claimId, CancellationToken ct)
    {
        return (await FindClaimAsync(claimId, ct)).ToDto();
    }

    public async Task<ClaimDto> IgnoreClaimAsync(Guid claimId, CancellationToken ct)
    {
        var claim = await FindClaimAsync(claimId, ct);
        claim.Status = ClaimStatus.Ignored;
        await TouchProjectAsync(claim.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return claim.ToDto();
    }

    public async Task<ClaimDto> RestoreClaimAsync(Guid claimId, CancellationToken ct)
    {
        var claim = await FindClaimAsync(claimId, ct);
        claim.Status = ClaimStatus.Extracted;
        await TouchProjectAsync(claim.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return claim.ToDto();
    }

    public async Task<IReadOnlyList<RequirementDto>> SynthesizeRequirementsAsync(Guid projectId, CancellationToken ct)
    {
        var existing = await LoadRequirements(projectId).ToListAsync(ct);
        if (existing.Count > 0)
        {
            return existing.Select(requirement => requirement.ToDto()).ToArray();
        }

        var claims = await db.Claims
            .Include(claim => claim.Evidence)
            .Where(claim => claim.ProjectId == projectId && claim.Status == ClaimStatus.Extracted && claim.Evidence.Count > 0)
            .OrderBy(claim => claim.CreatedAt)
            .ToListAsync(ct);
        if (claims.Count == 0)
        {
            return [];
        }

        var input = JsonDefaults.Serialize(new RequirementSynthesisInput(projectId)
        {
            Claims = claims.Select(claim => new ClaimInput(
                claim.Id,
                claim.Type.ToString(),
                claim.Subject,
                claim.Text,
                claim.Evidence.Select(evidence => evidence.ContentBlockId).ToArray())).ToArray()
        });
        var resultJson = await GenerateAndRecordAsync(
            projectId,
            GenerationPurposes.RequirementSynthesis,
            input,
            GenerationSchemas.Requirements,
            ct);
        var envelope = JsonDefaults.Deserialize<RequirementsEnvelope>(resultJson)
            ?? new RequirementsEnvelope([]);

        var requirements = envelope.Requirements.Select(draft =>
        {
            var type = Enum.TryParse<RequirementType>(draft.RequirementType, ignoreCase: true, out var parsedType)
                ? parsedType
                : RequirementType.Unknown;
            return Requirement.CreateSynthesized(
                projectId,
                draft.Title,
                draft.Summary,
                draft.Module,
                type,
                draft.Confidence,
                draft.Content,
                provider.GetType().Name);
        }).ToArray();

        db.Requirements.AddRange(requirements);

        foreach (var requirement in requirements)
        {
            var draft = envelope.Requirements.First(item => item.Title == requirement.Title);
            foreach (var conflict in draft.Conflicts)
            {
                db.RequirementConflicts.Add(new RequirementConflict
                {
                    ProjectId = projectId,
                    RequirementId = requirement.Id,
                    Description = conflict.Description,
                    ProposedResolution = conflict.ProposedResolution,
                    Status = ConflictStatus.Open,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await TouchProjectAsync(projectId, ct);
        await db.SaveChangesAsync(ct);
        return requirements.Select(requirement => requirement.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<RequirementDto>> GetRequirementsAsync(Guid projectId, CancellationToken ct)
    {
        return await LoadRequirements(projectId)
            .OrderByDescending(requirement => requirement.UpdatedAt)
            .Select(requirement => requirement.ToDto())
            .ToListAsync(ct);
    }

    public async Task<RequirementDto> GetRequirementAsync(Guid requirementId, CancellationToken ct)
    {
        return (await FindRequirementAsync(requirementId, ct)).ToDto();
    }

    public async Task<RequirementDto> UpdateRequirementAsync(Guid requirementId, UpdateRequirementRequest request, CancellationToken ct)
    {
        var requirement = await FindRequirementAsync(requirementId, ct);
        requirement.Title = request.Title.Trim();
        requirement.Summary = request.Summary.Trim();
        requirement.Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim();
        requirement.RequirementType = request.RequirementType;
        requirement.ResetToPendingReviewForNewVersion(
            request.Content,
            request.ChangeReason,
            request.CreatedBy);
        await TouchProjectAsync(requirement.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return requirement.ToDto();
    }

    public async Task<RequirementDto> ReviewRequirementAsync(Guid requirementId, ReviewRequirementRequest request, CancellationToken ct)
    {
        var requirement = await FindRequirementAsync(requirementId, ct);
        var currentVersion = requirement.CurrentVersionEntity
            ?? throw new DomainRuleViolationException("Requirement has no current version.");
        var review = requirement.ApplyReview(
            currentVersion.Id,
            request.Decision,
            request.ReviewerName,
            request.Comment);
        if (db.Entry(review).State == EntityState.Detached)
        {
            db.RequirementReviews.Add(review);
        }

        await TouchProjectAsync(requirement.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return requirement.ToDto();
    }

    public async Task<IReadOnlyList<OrchestrationTaskDto>> GenerateTasksAsync(Guid requirementId, CancellationToken ct)
    {
        var requirement = await FindRequirementAsync(requirementId, ct);
        if (requirement.Status != RequirementStatus.Approved)
        {
            throw new RequirementNotApprovedException(requirement.Id);
        }

        var existing = await db.OrchestrationTasks
            .Where(task => task.RequirementId == requirement.Id)
            .OrderBy(task => task.CreatedAt)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            return existing.Select(task => task.ToDto()).ToArray();
        }

        var version = requirement.CurrentVersionEntity
            ?? throw new DomainRuleViolationException("Requirement has no current version.");
        var content = version.GetContent();
        var evidence = await ResolveEvidenceAsync(content.SourceClaimIds, ct);
        var input = JsonDefaults.Serialize(new TaskGenerationInput(requirement.Id, version.Id, requirement.Title, content));
        var resultJson = await GenerateAndRecordAsync(
            requirement.ProjectId,
            GenerationPurposes.TaskGeneration,
            input,
            GenerationSchemas.Tasks,
            ct);
        var envelope = JsonDefaults.Deserialize<TasksEnvelope>(resultJson)
            ?? new TasksEnvelope([]);

        var tasks = envelope.Tasks.Select(draft =>
            OrchestrationTask.CreateFromRequirement(
                requirement,
                version,
                draft.Title,
                draft.Description,
                ParseEnum(draft.TaskType, OrchestrationTaskType.Implementation),
                draft.ReadinessScore,
                draft.AgentPrompt,
                JsonDefaults.Serialize(draft.AcceptanceCriteria),
                JsonDefaults.Serialize(evidence),
                JsonDefaults.Serialize(draft.MissingContext),
                JsonDefaults.Serialize(draft.Assumptions)))
            .ToArray();

        db.OrchestrationTasks.AddRange(tasks);
        await db.SaveChangesAsync(ct);

        var byTitle = tasks.ToDictionary(task => task.Title);
        var dependencies = new List<TaskDependency>();
        foreach (var draft in envelope.Tasks)
        {
            if (!byTitle.TryGetValue(draft.Title, out var downstream))
            {
                continue;
            }

            foreach (var upstreamTitle in draft.DependsOnTitles)
            {
                if (!byTitle.TryGetValue(upstreamTitle, out var upstream))
                {
                    continue;
                }

                dependencies.Add(new TaskDependency
                {
                    UpstreamTaskId = upstream.Id,
                    DownstreamTaskId = downstream.Id,
                    DependencyType = "blocks"
                });
            }
        }

        db.TaskDependencies.AddRange(dependencies);
        await TouchProjectAsync(requirement.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return tasks.Select(task => task.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<OrchestrationTaskDto>> GetTasksAsync(Guid projectId, CancellationToken ct)
    {
        return await db.OrchestrationTasks
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.CreatedAt)
            .Select(task => task.ToDto())
            .ToListAsync(ct);
    }

    public async Task<OrchestrationTaskDto> GetTaskAsync(Guid taskId, CancellationToken ct)
    {
        return (await FindTaskAsync(taskId, ct)).ToDto();
    }

    public async Task<OrchestrationTaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await FindTaskAsync(taskId, ct);
        task.Title = request.Title.Trim();
        task.Description = request.Description.Trim();
        task.TaskType = request.TaskType;
        task.ReadinessScore = request.ReadinessScore;
        task.AgentPrompt = request.AgentPrompt.Trim();
        task.AcceptanceCriteriaJson = JsonDefaults.Serialize(request.AcceptanceCriteria);
        task.MissingContextJson = JsonDefaults.Serialize(request.MissingContext);
        task.AssumptionsJson = JsonDefaults.Serialize(request.Assumptions);
        await TouchProjectAsync(task.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return task.ToDto();
    }

    public async Task<OrchestrationTaskDto> CancelTaskAsync(Guid taskId, CancellationToken ct)
    {
        var task = await FindTaskAsync(taskId, ct);
        task.Status = OrchestrationTaskStatus.Cancelled;
        await TouchProjectAsync(task.ProjectId, ct);
        await db.SaveChangesAsync(ct);
        return task.ToDto();
    }

    public async Task<IReadOnlyList<TaskDependencyDto>> GetTaskDependenciesAsync(Guid projectId, CancellationToken ct)
    {
        var taskIds = await db.OrchestrationTasks
            .Where(task => task.ProjectId == projectId)
            .Select(task => task.Id)
            .ToListAsync(ct);
        return await db.TaskDependencies
            .Where(dependency => taskIds.Contains(dependency.UpstreamTaskId)
                || taskIds.Contains(dependency.DownstreamTaskId))
            .Select(dependency => dependency.ToDto())
            .ToListAsync(ct);
    }

    public async Task<ExportBundleDto> ExportSymphonyMarkdownAsync(Guid projectId, CancellationToken ct)
    {
        var project = await FindProjectAsync(projectId, ct);
        var requirements = await LoadRequirements(projectId).ToListAsync(ct);
        var tasks = await db.OrchestrationTasks
            .Where(task => task.ProjectId == projectId && task.Status != OrchestrationTaskStatus.Cancelled)
            .OrderBy(task => task.CreatedAt)
            .ToListAsync(ct);
        var taskIds = tasks.Select(task => task.Id).ToHashSet();
        var dependencies = await db.TaskDependencies
            .Where(dependency => taskIds.Contains(dependency.UpstreamTaskId)
                || taskIds.Contains(dependency.DownstreamTaskId))
            .ToListAsync(ct);
        var content = SymphonyMarkdownExporter.Render(project, requirements, tasks, dependencies);
        var bundle = new ExportBundle
        {
            ProjectId = projectId,
            Format = ExportFormat.SymphonyMarkdown,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ExportBundles.Add(bundle);
        foreach (var task in tasks)
        {
            if (task.Status == OrchestrationTaskStatus.ReadyForExport)
            {
                task.Status = OrchestrationTaskStatus.Exported;
            }
        }

        await TouchProjectAsync(projectId, ct);
        await db.SaveChangesAsync(ct);
        return bundle.ToDto();
    }

    public async Task<IReadOnlyList<ExportBundleDto>> GetExportsAsync(Guid projectId, CancellationToken ct)
    {
        return await db.ExportBundles
            .Where(bundle => bundle.ProjectId == projectId)
            .OrderByDescending(bundle => bundle.CreatedAt)
            .Select(bundle => bundle.ToDto())
            .ToListAsync(ct);
    }

    public async Task<ExportBundleDto> GetExportAsync(Guid exportId, CancellationToken ct)
    {
        var export = await db.ExportBundles.FindAsync([exportId], ct)
            ?? throw new KeyNotFoundException("Export not found.");
        return export.ToDto();
    }

    public async Task<IReadOnlyList<ModelRunDto>> GetModelRunsAsync(Guid projectId, CancellationToken ct)
    {
        return await db.ModelRuns
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.CreatedAt)
            .Select(run => run.ToDto())
            .ToListAsync(ct);
    }

    public async Task<ModelRunDto> GetModelRunAsync(Guid modelRunId, CancellationToken ct)
    {
        var run = await db.ModelRuns.FindAsync([modelRunId], ct)
            ?? throw new KeyNotFoundException("ModelRun not found.");
        return run.ToDto();
    }

    private async Task<string> GenerateAndRecordAsync(
        Guid projectId,
        string purpose,
        string inputJson,
        string schemaJson,
        CancellationToken ct)
    {
        var modelRun = new ModelRun
        {
            ProjectId = projectId,
            Purpose = purpose,
            ProviderName = provider.GetType().Name,
            InputJson = inputJson,
            Status = ModelRunStatus.Running,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ModelRuns.Add(modelRun);
        await db.SaveChangesAsync(ct);

        var result = await provider.GenerateAsync(new StructuredGenerationRequest
        {
            Purpose = purpose,
            SystemInstruction = PromptForPurpose(purpose),
            UserInstruction = "输出必须是 JSON，不要编造，必须保留证据引用。",
            InputJson = inputJson,
            OutputSchemaJson = schemaJson,
            Options = new GenerationOptions { RequireValidJson = true }
        }, ct);

        modelRun.ProviderName = result.ProviderName;
        modelRun.ModelName = result.ModelName;
        modelRun.WorkingDirectory = result.WorkingDirectory;
        modelRun.OutputJson = result.OutputJson;
        modelRun.RawOutput = result.RawOutput;
        modelRun.Error = result.Error;
        modelRun.FinishedAt = DateTimeOffset.UtcNow;

        var schemaResult = await _schemaValidator.ValidateAsync(result.OutputJson, schemaJson, ct);
        if (!result.SchemaValid || !schemaResult.Valid)
        {
            modelRun.Status = ModelRunStatus.SchemaInvalid;
            modelRun.Error = result.Error ?? schemaResult.Error;
            await db.SaveChangesAsync(ct);
            throw new DomainRuleViolationException($"Provider output schema invalid: {modelRun.Error}");
        }

        modelRun.Status = ModelRunStatus.Succeeded;
        await db.SaveChangesAsync(ct);
        return result.OutputJson;
    }

    private static string PromptForPurpose(string purpose)
    {
        return purpose switch
        {
            GenerationPurposes.ArtifactRead => "你是项目资料读取器，只做转写、摘录、结构化，不做需求判断。",
            GenerationPurposes.ClaimExtraction => "你是项目需求分析器，从 ContentBlock 抽取 Claim，每个 Claim 必须引用 ContentBlockId。",
            GenerationPurposes.RequirementSynthesis => "你是候选需求合成器，只能从 Claim 合成 PendingReview Requirement。",
            GenerationPurposes.TaskGeneration => "你是 agent-ready 任务拆解器，只能使用已批准需求内容。",
            _ => "输出结构化 JSON。"
        };
    }

    private static Claim ToClaim(Guid projectId, ClaimDraft draft, IReadOnlyDictionary<Guid, ContentBlock> blockById)
    {
        var claim = new Claim
        {
            ProjectId = projectId,
            Type = ParseEnum(draft.Type, ClaimType.Requirement),
            Subject = draft.Subject,
            Text = draft.Text,
            Confidence = draft.Confidence,
            AmbiguityScore = draft.AmbiguityScore,
            FreshnessScore = draft.FreshnessScore,
            SourceAuthorityScore = draft.SourceAuthorityScore,
            Status = ClaimStatus.Extracted,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var blockId in draft.ContentBlockIds.Distinct())
        {
            if (!blockById.TryGetValue(blockId, out var block))
            {
                continue;
            }

            claim.Evidence.Add(new ClaimEvidence
            {
                ClaimId = claim.Id,
                ContentBlockId = blockId,
                EvidenceText = $"{block.LocationLabel}: {block.Text}"
            });
        }

        if (claim.Evidence.Count == 0)
        {
            throw new DomainRuleViolationException("Claim 必须引用 ContentBlock");
        }

        return claim;
    }

    private async Task<IReadOnlyList<string>> ResolveEvidenceAsync(IReadOnlyList<Guid> sourceClaimIds, CancellationToken ct)
    {
        if (sourceClaimIds.Count == 0)
        {
            return [];
        }

        var claims = await db.Claims
            .Include(claim => claim.Evidence)
            .Where(claim => sourceClaimIds.Contains(claim.Id))
            .ToListAsync(ct);
        return claims
            .SelectMany(claim => claim.Evidence)
            .Select(evidence => evidence.EvidenceText)
            .Distinct()
            .ToArray();
    }

    private async Task<ProjectDto> ToProjectDtoAsync(Project project, CancellationToken ct)
    {
        var stats = new ProjectStatsDto(
            await db.Artifacts.CountAsync(artifact => artifact.ProjectId == project.Id, ct),
            await db.ContentBlocks.CountAsync(block => block.ProjectId == project.Id, ct),
            await db.Claims.CountAsync(claim => claim.ProjectId == project.Id, ct),
            await db.Requirements.CountAsync(requirement => requirement.ProjectId == project.Id
                && requirement.Status == RequirementStatus.PendingReview, ct),
            await db.Requirements.CountAsync(requirement => requirement.ProjectId == project.Id
                && requirement.Status == RequirementStatus.Approved, ct),
            await db.OrchestrationTasks.CountAsync(task => task.ProjectId == project.Id, ct),
            await db.ExportBundles
                .Where(bundle => bundle.ProjectId == project.Id)
                .OrderByDescending(bundle => bundle.CreatedAt)
                .Select(bundle => (DateTimeOffset?)bundle.CreatedAt)
                .FirstOrDefaultAsync(ct));

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            project.CreatedAt,
            project.UpdatedAt,
            stats);
    }

    private async Task<Project> FindProjectAsync(Guid projectId, CancellationToken ct)
    {
        return await db.Projects.FindAsync([projectId], ct)
            ?? throw new KeyNotFoundException("Project not found.");
    }

    private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(project => project.Id == projectId, ct))
        {
            throw new KeyNotFoundException("Project not found.");
        }
    }

    private async Task<Artifact> FindArtifactAsync(Guid artifactId, CancellationToken ct)
    {
        return await db.Artifacts.FindAsync([artifactId], ct)
            ?? throw new KeyNotFoundException("Artifact not found.");
    }

    private async Task<Claim> FindClaimAsync(Guid claimId, CancellationToken ct)
    {
        return await db.Claims
            .Include(claim => claim.Evidence)
            .SingleOrDefaultAsync(claim => claim.Id == claimId, ct)
            ?? throw new KeyNotFoundException("Claim not found.");
    }

    private async Task<Requirement> FindRequirementAsync(Guid requirementId, CancellationToken ct)
    {
        return await db.Requirements
            .Include(requirement => requirement.Versions)
            .Include(requirement => requirement.Reviews)
            .SingleOrDefaultAsync(requirement => requirement.Id == requirementId, ct)
            ?? throw new KeyNotFoundException("Requirement not found.");
    }

    private IQueryable<Requirement> LoadRequirements(Guid projectId)
    {
        return db.Requirements
            .Include(requirement => requirement.Versions)
            .Include(requirement => requirement.Reviews)
            .Where(requirement => requirement.ProjectId == projectId);
    }

    private async Task<OrchestrationTask> FindTaskAsync(Guid taskId, CancellationToken ct)
    {
        return await db.OrchestrationTasks.FindAsync([taskId], ct)
            ?? throw new KeyNotFoundException("Task not found.");
    }

    private async Task TouchProjectAsync(Guid projectId, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is not null)
        {
            project.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static ArtifactType DetectArtifactType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => ArtifactType.Text,
            ".md" => ArtifactType.Markdown,
            ".csv" => ArtifactType.Csv,
            ".xlsx" => ArtifactType.Excel,
            ".docx" => ArtifactType.Docx,
            ".pdf" => ArtifactType.Pdf,
            _ => ArtifactType.Other
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }
}
