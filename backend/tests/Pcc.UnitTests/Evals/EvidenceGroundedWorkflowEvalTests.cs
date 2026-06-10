using Microsoft.EntityFrameworkCore;
using Pcc.Application.Dtos;
using Pcc.Application.Generation;
using Pcc.Domain;
using Pcc.Infrastructure.Generation;
using Pcc.Infrastructure.Json;
using Pcc.Infrastructure.Persistence;
using Pcc.Infrastructure.Services;
using Pcc.Infrastructure.Storage;

namespace Pcc.UnitTests.Evals;

public sealed class EvidenceGroundedWorkflowEvalTests
{
    private const string EvidenceNotes = """
        第一版只做管理员通过邮箱邀请成员。
        普通成员不能邀请。
        真实邮件服务暂时不接。
        """;

    [Fact]
    public async Task Claim_extraction_sends_only_confirmed_blocks_to_provider()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        provider.Enqueue(GenerationPurposes.ArtifactRead, BuildContentBlocksFromInput);
        provider.Enqueue(GenerationPurposes.ClaimExtraction, BuildClaimsFromInput);

        var project = await CreateProjectAsync(service);
        var artifact = await service.PasteArtifactAsync(
            project.Id,
            new PasteArtifactRequest(
                "证据资料",
                $"{EvidenceNotes}{Environment.NewLine}SSO 登录稍后再说。",
                ArtifactType.MeetingNotes),
            CancellationToken.None);
        var blocks = await service.ReadArtifactAsync(artifact.Id, CancellationToken.None);
        var ignoredBlock = Assert.Single(blocks, block => block.Text.Contains("SSO", StringComparison.Ordinal));
        await service.ReviewContentBlockAsync(
            ignoredBlock.Id,
            new ReviewContentBlockRequest(ContentBlockVerificationStatus.Ignored, "PM"),
            CancellationToken.None);
        await service.ReviewContentBlocksAsync(
            project.Id,
            new ReviewContentBlocksRequest(
                blocks.Where(block => block.Id != ignoredBlock.Id).Select(block => block.Id).ToArray(),
                ContentBlockVerificationStatus.Confirmed,
                "PM"),
            CancellationToken.None);

        var claims = await service.ExtractClaimsAsync(project.Id, CancellationToken.None);

        var request = Assert.Single(provider.Requests, request => request.Purpose == GenerationPurposes.ClaimExtraction);
        var input = JsonDefaults.Deserialize<ClaimExtractionInput>(request.InputJson)!;
        Assert.DoesNotContain(input.ContentBlocks, block => block.Id == ignoredBlock.Id);
        Assert.DoesNotContain("SSO", request.InputJson, StringComparison.Ordinal);
        Assert.NotEmpty(claims);
        Assert.DoesNotContain(
            claims.SelectMany(claim => claim.Evidence),
            evidence => evidence.ContentBlockId == ignoredBlock.Id);
    }

    [Fact]
    public async Task Claim_extraction_rejects_claims_without_valid_evidence()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        provider.Enqueue(GenerationPurposes.ArtifactRead, BuildContentBlocksFromInput);
        provider.Enqueue(GenerationPurposes.ClaimExtraction, _ => JsonDefaults.Serialize(new ClaimsEnvelope([
            new ClaimDraft(
                "Requirement",
                "成员邀请",
                "第一版支持管理员通过邮箱邀请成员",
                0.88m,
                0.1m,
                0.8m,
                0.75m,
                [Guid.NewGuid()])
        ])));

        var project = await CreateReviewedProjectAsync(service, provider);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => service.ExtractClaimsAsync(project.Id, CancellationToken.None));
        Assert.Empty(await db.Claims.ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Task_generation_does_not_call_provider_before_requirement_approval()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        var requirement = await CreatePendingRequirementAsync(service, provider);

        await Assert.ThrowsAsync<RequirementNotApprovedException>(
            () => service.GenerateTasksAsync(requirement.Id, CancellationToken.None));
        Assert.DoesNotContain(provider.Requests, request => request.Purpose == GenerationPurposes.TaskGeneration);
    }

    [Fact]
    public async Task Approved_task_generation_preserves_requirement_version_and_evidence()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        var requirement = await CreatePendingRequirementAsync(service, provider);
        var approved = await service.ReviewRequirementAsync(
            requirement.Id,
            new ReviewRequirementRequest(RequirementReviewDecision.Approve, "PM", "确认进入开发"),
            CancellationToken.None);
        provider.Enqueue(GenerationPurposes.TaskGeneration, BuildSafeTasks);

        var tasks = await service.GenerateTasksAsync(requirement.Id, CancellationToken.None);

        Assert.NotEmpty(tasks);
        Assert.All(tasks, task =>
        {
            Assert.Equal(requirement.Id, task.RequirementId);
            Assert.Equal(approved.CurrentVersionId, task.RequirementVersionId);
            Assert.NotEmpty(task.Evidence);
            Assert.All(task.Evidence, evidence =>
                Assert.Contains("证据资料", evidence, StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Task_generation_allows_negative_mentions_of_deferred_scope()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        var requirement = await CreatePendingRequirementAsync(service, provider);
        await service.ReviewRequirementAsync(
            requirement.Id,
            new ReviewRequirementRequest(RequirementReviewDecision.Approve, "PM", "确认进入开发"),
            CancellationToken.None);
        provider.Enqueue(GenerationPurposes.TaskGeneration, _ => JsonDefaults.Serialize(new TasksEnvelope([
            CreateTaskDraft(
                "实现邀请创建 API",
                "实现管理员通过邮箱创建成员邀请，不要接入真实邮件服务。",
                "请实现邀请创建 API，暂不实现真实邮件服务。")
        ])));

        var tasks = await service.GenerateTasksAsync(requirement.Id, CancellationToken.None);

        var task = Assert.Single(tasks);
        Assert.Contains("不要接入真实邮件服务", task.Description, StringComparison.Ordinal);
        Assert.Contains("暂不实现真实邮件服务", task.AgentPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Task_generation_rejects_execution_of_deferred_scope()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        var requirement = await CreatePendingRequirementAsync(service, provider);
        await service.ReviewRequirementAsync(
            requirement.Id,
            new ReviewRequirementRequest(RequirementReviewDecision.Approve, "PM", "确认进入开发"),
            CancellationToken.None);
        provider.Enqueue(GenerationPurposes.TaskGeneration, _ => JsonDefaults.Serialize(new TasksEnvelope([
            CreateTaskDraft(
                "接入真实邮件服务",
                "实现真实邮件发送能力。",
                "请接入真实邮件服务并发送邀请邮件。")
        ])));

        await Assert.ThrowsAsync<TaskGenerationNotGroundedException>(
            () => service.GenerateTasksAsync(requirement.Id, CancellationToken.None));
        Assert.Empty(await db.OrchestrationTasks.ToListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Schema_invalid_task_output_does_not_create_tasks()
    {
        await using var db = CreateDbContext();
        var provider = new RecordingStructuredGenerationProvider();
        var service = CreateService(db, provider);
        var requirement = await CreatePendingRequirementAsync(service, provider);
        await service.ReviewRequirementAsync(
            requirement.Id,
            new ReviewRequirementRequest(RequirementReviewDecision.Approve, "PM", "确认进入开发"),
            CancellationToken.None);
        provider.Enqueue(GenerationPurposes.TaskGeneration, _ => """
            {
              "tasks": [
                {
                  "title": "缺字段任务",
                  "description": "缺少 taskType、readinessScore 和 agentPrompt"
                }
              ]
            }
            """);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => service.GenerateTasksAsync(requirement.Id, CancellationToken.None));

        Assert.Empty(await db.OrchestrationTasks.ToListAsync(CancellationToken.None));
        var modelRun = Assert.Single(await db.ModelRuns
            .Where(run => run.Purpose == GenerationPurposes.TaskGeneration)
            .ToListAsync(CancellationToken.None));
        Assert.Equal(ModelRunStatus.SchemaInvalid, modelRun.Status);
    }

    private static async Task<ProjectDto> CreateReviewedProjectAsync(
        CompilerWorkflowService service,
        RecordingStructuredGenerationProvider provider)
    {
        provider.Enqueue(GenerationPurposes.ArtifactRead, BuildContentBlocksFromInput);
        var project = await CreateProjectAsync(service);
        var artifact = await service.PasteArtifactAsync(
            project.Id,
            new PasteArtifactRequest("证据资料", EvidenceNotes, ArtifactType.MeetingNotes),
            CancellationToken.None);
        var blocks = await service.ReadArtifactAsync(artifact.Id, CancellationToken.None);
        await service.ReviewContentBlocksAsync(
            project.Id,
            new ReviewContentBlocksRequest(
                blocks.Select(block => block.Id).ToArray(),
                ContentBlockVerificationStatus.Confirmed,
                "PM"),
            CancellationToken.None);
        return project;
    }

    private static async Task<RequirementDto> CreatePendingRequirementAsync(
        CompilerWorkflowService service,
        RecordingStructuredGenerationProvider provider)
    {
        var project = await CreateReviewedProjectAsync(service, provider);
        provider.Enqueue(GenerationPurposes.ClaimExtraction, BuildClaimsFromInput);
        provider.Enqueue(GenerationPurposes.RequirementSynthesis, BuildRequirementsFromInput);
        await service.ExtractClaimsAsync(project.Id, CancellationToken.None);
        var requirements = await service.SynthesizeRequirementsAsync(project.Id, CancellationToken.None);
        return Assert.Single(requirements);
    }

    private static Task<ProjectDto> CreateProjectAsync(CompilerWorkflowService service)
    {
        return service.CreateProjectAsync(
            new CreateProjectRequest("证据护栏 eval", "验证 provider 输出不能越过业务护栏"),
            CancellationToken.None);
    }

    private static string BuildContentBlocksFromInput(StructuredGenerationRequest request)
    {
        var input = JsonDefaults.Deserialize<ArtifactReadInput>(request.InputJson)!;
        var blocks = (input.RawText ?? "")
            .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select((line, index) => new ContentBlockDraft(
                "Paragraph",
                line,
                $"{input.Title} / 第 {index + 1} 段",
                index + 1,
                0.92m,
                new Dictionary<string, object?>()))
            .ToArray();
        return JsonDefaults.Serialize(new ContentBlocksEnvelope(blocks));
    }

    private static string BuildClaimsFromInput(StructuredGenerationRequest request)
    {
        var input = JsonDefaults.Deserialize<ClaimExtractionInput>(request.InputJson)!;
        var claims = new List<ClaimDraft>();
        foreach (var block in input.ContentBlocks)
        {
            if (block.Text.Contains("管理员", StringComparison.Ordinal) && block.Text.Contains("邀请", StringComparison.Ordinal))
            {
                claims.Add(CreateClaim("Requirement", "成员邀请", "第一版支持管理员通过邮箱邀请成员", block.Id));
            }

            if (block.Text.Contains("普通成员", StringComparison.Ordinal))
            {
                claims.Add(CreateClaim("BusinessRule", "邀请权限", "普通成员不能邀请", block.Id));
            }

            if (block.Text.Contains("真实邮件服务", StringComparison.Ordinal))
            {
                claims.Add(CreateClaim("DeferredScope", "邮件服务", "真实邮件服务暂时不接", block.Id));
            }
        }

        return JsonDefaults.Serialize(new ClaimsEnvelope(claims));
    }

    private static string BuildRequirementsFromInput(StructuredGenerationRequest request)
    {
        var input = JsonDefaults.Deserialize<RequirementSynthesisInput>(request.InputJson)!;
        var sourceClaimIds = input.Claims.Select(claim => claim.Id).ToArray();
        var content = new RequirementContent
        {
            Actors = ["workspace_admin"],
            BusinessRules = input.Claims
                .Where(claim => claim.Type == "BusinessRule")
                .Select(claim => claim.Text)
                .ToArray(),
            MainFlow = ["输入邮箱", "创建邀请"],
            AcceptanceCriteria = ["管理员可以创建邀请", "普通成员不能创建邀请"],
            DeferredScope = ["真实邮件服务"],
            SourceClaimIds = sourceClaimIds
        };
        return JsonDefaults.Serialize(new RequirementsEnvelope([
            new RequirementDraft(
                "成员邀请",
                "管理员可以通过邮箱邀请成员加入工作区",
                "成员管理",
                "Functional",
                0.82m,
                content,
                [])
        ]));
    }

    private static string BuildSafeTasks(StructuredGenerationRequest request)
    {
        return JsonDefaults.Serialize(new TasksEnvelope([
            CreateTaskDraft(
                "实现邀请创建 API",
                "实现管理员通过邮箱创建成员邀请，不要接入真实邮件服务。",
                "请实现邀请创建 API，不要接入真实邮件服务。")
        ]));
    }

    private static ClaimDraft CreateClaim(string type, string subject, string text, Guid blockId)
    {
        return new ClaimDraft(type, subject, text, 0.9m, 0.1m, 0.8m, 0.75m, [blockId]);
    }

    private static TaskDraft CreateTaskDraft(string title, string description, string agentPrompt)
    {
        return new TaskDraft(
            title,
            description,
            "Implementation",
            86m,
            agentPrompt,
            ["管理员可以创建邀请"],
            [],
            [],
            []);
    }

    private static PccDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PccDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PccDbContext(options);
    }

    private static CompilerWorkflowService CreateService(
        PccDbContext db,
        RecordingStructuredGenerationProvider provider)
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), "pcc-eval-tests", Guid.NewGuid().ToString("N"));
        return new CompilerWorkflowService(db, provider, new LocalFileStorage(storageRoot));
    }

    private sealed class RecordingStructuredGenerationProvider : IStructuredGenerationProvider
    {
        private readonly Dictionary<string, Queue<Func<StructuredGenerationRequest, string>>> _responses = [];

        public List<StructuredGenerationRequest> Requests { get; } = [];

        public void Enqueue(string purpose, Func<StructuredGenerationRequest, string> response)
        {
            if (!_responses.TryGetValue(purpose, out var queue))
            {
                queue = new Queue<Func<StructuredGenerationRequest, string>>();
                _responses[purpose] = queue;
            }

            queue.Enqueue(response);
        }

        public Task<StructuredGenerationResult> GenerateAsync(StructuredGenerationRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            if (!_responses.TryGetValue(request.Purpose, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException($"No scripted response for {request.Purpose}.");
            }

            var outputJson = queue.Dequeue()(request);
            return Task.FromResult(new StructuredGenerationResult
            {
                OutputJson = outputJson,
                ProviderName = nameof(RecordingStructuredGenerationProvider),
                ModelName = "scripted-eval",
                SchemaValid = true,
                RawOutput = outputJson,
                Duration = TimeSpan.Zero
            });
        }
    }
}
