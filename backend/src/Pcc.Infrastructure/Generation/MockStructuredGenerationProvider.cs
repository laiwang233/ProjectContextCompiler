using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pcc.Application.Generation;
using Pcc.Domain;
using Pcc.Infrastructure.Json;

namespace Pcc.Infrastructure.Generation;

public sealed class MockStructuredGenerationProvider : IStructuredGenerationProvider
{
    public Task<StructuredGenerationResult> GenerateAsync(
        StructuredGenerationRequest request,
        CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        var outputJson = request.Purpose switch
        {
            GenerationPurposes.ArtifactRead => BuildContentBlocks(request.InputJson),
            GenerationPurposes.ClaimExtraction => BuildClaims(request.InputJson),
            GenerationPurposes.RequirementSynthesis => BuildRequirements(request.InputJson),
            GenerationPurposes.TaskGeneration => BuildTasks(request.InputJson),
            _ => "{}"
        };
        watch.Stop();

        return Task.FromResult(new StructuredGenerationResult
        {
            OutputJson = outputJson,
            ProviderName = "MockStructuredGenerationProvider",
            ModelName = "mock-rules-v1",
            SchemaValid = true,
            RawOutput = outputJson,
            Duration = watch.Elapsed
        });
    }

    private static string BuildContentBlocks(string inputJson)
    {
        var input = JsonDefaults.Deserialize<ArtifactReadInput>(inputJson) ?? new ArtifactReadInput();
        var lines = Regex.Split(input.RawText ?? "", @"\r?\n")
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length == 0)
        {
            lines = [
                $"无法轻量提取 {input.Title} 的正文，保留原始文件供 LLM-first provider 读取。"
            ];
        }

        var blocks = lines.Select((line, index) => new ContentBlockDraft(
            "Paragraph",
            line,
            $"{input.Title} / 第 {index + 1} 段",
            index + 1,
            input.RawText is null ? 0.55m : 0.92m,
            new Dictionary<string, object?>()))
            .ToArray();

        return JsonDefaults.Serialize(new ContentBlocksEnvelope(blocks));
    }

    private static string BuildClaims(string inputJson)
    {
        var input = JsonDefaults.Deserialize<ClaimExtractionInput>(inputJson) ?? new ClaimExtractionInput();
        var claims = new List<ClaimDraft>();

        foreach (var block in input.ContentBlocks.OrderBy(block => block.OrderIndex))
        {
            var text = block.Text;
            if (text.Contains("管理员") && text.Contains("邀请"))
            {
                claims.Add(CreateClaim(
                    "Requirement",
                    "成员邀请",
                    "第一版支持管理员通过邮箱邀请成员",
                    0.88m,
                    block));
            }

            if (text.Contains("普通成员") && text.Contains("不能邀请"))
            {
                claims.Add(CreateClaim(
                    "BusinessRule",
                    "邀请权限",
                    "普通成员不能邀请",
                    0.9m,
                    block));
            }

            if (text.Contains("暂时不接"))
            {
                claims.Add(CreateClaim(
                    "DeferredScope",
                    "邮件服务",
                    "真实邮件服务暂时不接",
                    0.86m,
                    block));
            }

            if (text.Contains("先 mock", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(CreateClaim(
                    "Decision",
                    "邮件发送",
                    "第一版邮件先 mock",
                    0.86m,
                    block));
            }

            if (text.Contains("重复邀请"))
            {
                claims.Add(CreateClaim(
                    "BusinessRule",
                    "重复邀请",
                    "重复邀请不创建重复记录",
                    0.89m,
                    block));
            }

            if (text.Contains("还没确定"))
            {
                claims.Add(CreateClaim(
                    "OpenQuestion",
                    "席位数",
                    "邀请是否计入席位数",
                    0.81m,
                    block,
                    ambiguityScore: 0.72m));
            }

            if (text.Contains("需要补充") && text.Contains("测试"))
            {
                claims.Add(CreateClaim(
                    "TestRequirement",
                    "权限测试",
                    "补充非管理员不能邀请的 e2e 测试",
                    0.84m,
                    block));
            }
        }

        return JsonDefaults.Serialize(new ClaimsEnvelope(claims));
    }

    private static string BuildRequirements(string inputJson)
    {
        var input = JsonDefaults.Deserialize<RequirementSynthesisInput>(inputJson) ?? new RequirementSynthesisInput();
        var sourceClaimIds = input.Claims.Select(claim => claim.Id).Distinct().ToArray();
        var content = new RequirementContent
        {
            Actors = ["workspace_admin"],
            BusinessRules = input.Claims
                .Where(claim => claim.Type == "BusinessRule")
                .Select(claim => claim.Text)
                .Distinct()
                .ToArray(),
            MainFlow = ["进入成员管理页", "输入邮箱", "创建邀请"],
            ExceptionFlows = ["重复邀请时提示已存在并不创建重复记录"],
            AcceptanceCriteria = ["管理员可以创建邀请", "普通成员不能创建邀请", "重复邀请不会创建重复记录"],
            OpenQuestions = input.Claims
                .Where(claim => claim.Type == "OpenQuestion")
                .Select(claim => claim.Text)
                .Distinct()
                .ToArray(),
            DeferredScope = input.Claims
                .Where(claim => claim.Type == "DeferredScope")
                .Select(claim => claim.Text.Replace("暂时不接", "").Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .DefaultIfEmpty("真实邮件服务")
                .Distinct()
                .ToArray(),
            Assumptions = input.Claims.Any(claim => claim.Type == "Decision" && claim.Text.Contains("mock"))
                ? ["第一版邮件发送使用 mock"]
                : [],
            SourceClaimIds = sourceClaimIds
        };

        var requirements = new[]
        {
            new RequirementDraft(
                "成员邀请",
                "管理员可以通过邮箱邀请成员加入工作区",
                "成员管理",
                "Functional",
                0.82m,
                content,
                [])
        };

        return JsonDefaults.Serialize(new RequirementsEnvelope(requirements));
    }

    private static string BuildTasks(string inputJson)
    {
        var input = JsonDefaults.Deserialize<TaskGenerationInput>(inputJson) ?? new TaskGenerationInput();
        var missingContext = input.Content.OpenQuestions;
        var assumptions = input.Content.Assumptions;
        var tasks = new[]
        {
            new TaskDraft(
                "设计邀请数据模型",
                "为成员邀请功能设计 invitation 数据模型和状态字段。",
                "Design",
                87m,
                "请基于现有代码设计 invitation 数据模型，保留 pending 状态和唯一性约束。",
                ["存在 invitation 实体或表设计", "支持 pending 状态", "记录邀请邮箱"],
                missingContext,
                assumptions,
                []),
            new TaskDraft(
                "实现邀请创建 API",
                "实现管理员通过邮箱创建成员邀请的后端 API。",
                "Implementation",
                84m,
                "请基于已批准需求实现邀请创建 API，不要接入真实邮件服务。",
                ["管理员可以创建邀请", "邮箱必填", "返回创建结果"],
                missingContext,
                assumptions,
                ["设计邀请数据模型"]),
            new TaskDraft(
                "实现重复邀请处理",
                "重复邀请时不要创建重复记录。",
                "Implementation",
                86m,
                "请实现重复邀请检测，重复邮箱返回已存在语义，不创建新记录。",
                ["重复邀请不会创建重复记录", "重复邀请有明确响应"],
                missingContext,
                assumptions,
                ["实现邀请创建 API"]),
            new TaskDraft(
                "添加权限校验",
                "确保普通成员不能邀请成员。",
                "Implementation",
                88m,
                "请补充邀请 API 的管理员权限校验，普通成员必须被拒绝。",
                ["管理员可以邀请", "普通成员不能邀请"],
                missingContext,
                assumptions,
                ["实现邀请创建 API"]),
            new TaskDraft(
                "添加非管理员邀请 e2e 测试",
                "补充非管理员不能邀请的 e2e 测试。",
                "Test",
                78m,
                "请添加非管理员不能邀请的 e2e 或集成测试，覆盖拒绝路径。",
                ["非管理员邀请测试失败路径存在", "测试能稳定复现权限限制"],
                missingContext,
                assumptions,
                ["添加权限校验"])
        };

        return JsonDefaults.Serialize(new TasksEnvelope(tasks));
    }

    private static ClaimDraft CreateClaim(
        string type,
        string subject,
        string text,
        decimal confidence,
        ContentBlockInput block,
        decimal ambiguityScore = 0.1m)
    {
        return new ClaimDraft(
            type,
            subject,
            text,
            confidence,
            ambiguityScore,
            0.8m,
            0.75m,
            [block.Id]);
    }
}

public static class GenerationPurposes
{
    public const string ArtifactRead = "ArtifactRead";
    public const string ClaimExtraction = "ClaimExtraction";
    public const string RequirementSynthesis = "RequirementSynthesis";
    public const string TaskGeneration = "TaskGeneration";
}

public sealed record ArtifactReadInput(
    Guid ProjectId = default,
    Guid ArtifactId = default,
    string Title = "",
    string? FileName = null,
    string? RawText = null);

public sealed record ContentBlockDraft(
    string Type,
    string Text,
    string? LocationLabel,
    int OrderIndex,
    decimal Confidence,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record ContentBlocksEnvelope(IReadOnlyList<ContentBlockDraft> ContentBlocks);

public sealed record ContentBlockInput(
    Guid Id,
    int OrderIndex,
    string Text,
    string? LocationLabel);

public sealed record ClaimExtractionInput(
    Guid ProjectId = default,
    IReadOnlyList<ContentBlockInput>? Blocks = null)
{
    public IReadOnlyList<ContentBlockInput> ContentBlocks { get; init; } = Blocks ?? [];
}

public sealed record ClaimDraft(
    string Type,
    string Subject,
    string Text,
    decimal Confidence,
    decimal AmbiguityScore,
    decimal FreshnessScore,
    decimal SourceAuthorityScore,
    IReadOnlyList<Guid> ContentBlockIds);

public sealed record ClaimsEnvelope(IReadOnlyList<ClaimDraft> Claims);

public sealed record ClaimInput(
    Guid Id,
    string Type,
    string Subject,
    string Text,
    IReadOnlyList<Guid> ContentBlockIds);

public sealed record RequirementSynthesisInput(
    Guid ProjectId = default,
    IReadOnlyList<ClaimInput>? Items = null)
{
    public IReadOnlyList<ClaimInput> Claims { get; init; } = Items ?? [];
}

public sealed record RequirementDraft(
    string Title,
    string Summary,
    string? Module,
    string RequirementType,
    decimal Confidence,
    RequirementContent Content,
    IReadOnlyList<RequirementConflictDraft> Conflicts);

public sealed record RequirementConflictDraft(string Description, string? ProposedResolution);

public sealed record RequirementsEnvelope(IReadOnlyList<RequirementDraft> Requirements);

public sealed record TaskGenerationInput(
    Guid RequirementId = default,
    Guid RequirementVersionId = default,
    string Title = "",
    RequirementContent? ContentDraft = null)
{
    public RequirementContent Content { get; init; } = ContentDraft ?? RequirementContent.Empty;
}

public sealed record TaskDraft(
    string Title,
    string Description,
    string TaskType,
    decimal ReadinessScore,
    string AgentPrompt,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> MissingContext,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> DependsOnTitles);

public sealed record TasksEnvelope(IReadOnlyList<TaskDraft> Tasks);
