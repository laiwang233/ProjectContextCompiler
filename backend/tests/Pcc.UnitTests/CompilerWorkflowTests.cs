using Microsoft.EntityFrameworkCore;
using Pcc.Application.Dtos;
using Pcc.Domain;
using Pcc.Infrastructure.Generation;
using Pcc.Infrastructure.Persistence;
using Pcc.Infrastructure.Services;
using Pcc.Infrastructure.Storage;

namespace Pcc.UnitTests;

public sealed class CompilerWorkflowTests
{
    private const string MeetingNotes = """
        今天需求评审讨论成员邀请功能。
        第一版只做管理员通过邮箱邀请成员。
        普通成员不能邀请。
        真实邮件服务暂时不接，先 mock。
        重复邀请时不要创建重复记录。
        邀请是否计入席位数还没确定。
        需要补充非管理员不能邀请的 e2e 测试。
        """;

    [Fact]
    public async Task Mock_workflow_keeps_human_review_gate_before_task_export()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var project = await service.CreateProjectAsync(
            new CreateProjectRequest("邀请功能编译", "从会议记录生成可审核需求"),
            CancellationToken.None);
        var artifact = await service.PasteArtifactAsync(
            project.Id,
            new PasteArtifactRequest("会议记录", MeetingNotes, ArtifactType.MeetingNotes),
            CancellationToken.None);

        var blocks = await service.ReadArtifactAsync(artifact.Id, CancellationToken.None);
        Assert.Equal(7, blocks.Count);
        Assert.All(blocks, block => Assert.Equal(project.Id, block.ProjectId));

        var claims = await service.ExtractClaimsAsync(project.Id, CancellationToken.None);
        Assert.Contains(claims, claim => claim.Type == ClaimType.BusinessRule && claim.Text.Contains("普通成员"));
        Assert.Contains(claims, claim => claim.Type == ClaimType.OpenQuestion && claim.Text.Contains("席位数"));
        Assert.All(claims, claim => Assert.NotEmpty(claim.Evidence));

        var requirements = await service.SynthesizeRequirementsAsync(project.Id, CancellationToken.None);
        var requirement = Assert.Single(requirements);
        Assert.Equal(RequirementStatus.PendingReview, requirement.Status);
        Assert.Equal(1, requirement.CurrentVersion);
        Assert.Contains("普通成员不能邀请", requirement.Content.BusinessRules);
        Assert.Contains("真实邮件服务", requirement.Content.DeferredScope);

        await Assert.ThrowsAsync<RequirementNotApprovedException>(
            () => service.GenerateTasksAsync(requirement.Id, CancellationToken.None));

        var approved = await service.ReviewRequirementAsync(
            requirement.Id,
            new ReviewRequirementRequest(RequirementReviewDecision.Approve, "PM", "确认进入开发"),
            CancellationToken.None);
        Assert.Equal(RequirementStatus.Approved, approved.Status);

        var tasks = await service.GenerateTasksAsync(requirement.Id, CancellationToken.None);
        Assert.Contains(tasks, task => task.Title.Contains("权限"));
        Assert.All(tasks, task =>
        {
            Assert.Equal(requirement.Id, task.RequirementId);
            Assert.Equal(approved.CurrentVersionId, task.RequirementVersionId);
        });

        var export = await service.ExportSymphonyMarkdownAsync(project.Id, CancellationToken.None);
        Assert.Contains("# Symphony Task Bundle", export.Content);
        Assert.Contains("```mermaid", export.Content);
        Assert.Contains("## Source Requirement", export.Content);
        Assert.Contains("## Agent Prompt", export.Content);
        Assert.DoesNotContain("Status: PendingReview", export.Content);

        var modelRuns = await service.GetModelRunsAsync(project.Id, CancellationToken.None);
        Assert.True(modelRuns.Count >= 4);
        Assert.All(modelRuns, run => Assert.Equal(ModelRunStatus.Succeeded, run.Status));
    }

    [Fact]
    public void Requirement_review_decisions_move_to_the_specified_statuses()
    {
        var requirement = Requirement.CreateSynthesized(
            Guid.NewGuid(),
            "成员邀请",
            "管理员可以通过邮箱邀请成员",
            "成员管理",
            RequirementType.Functional,
            0.82m,
            RequirementContent.Empty with
            {
                BusinessRules = ["普通成员不能邀请"],
                SourceClaimIds = [Guid.NewGuid()]
            },
            "MockStructuredGenerationProvider");
        var version = requirement.Versions.Single();

        requirement.ApplyReview(version.Id, RequirementReviewDecision.RequestClarification, "PM", "需要补充席位规则");
        Assert.Equal(RequirementStatus.NeedsClarification, requirement.Status);

        requirement.ResetToPendingReviewForNewVersion(
            RequirementContent.Empty with { OpenQuestions = ["邀请是否计入席位数"] },
            "补充问题",
            "PM");
        version = requirement.Versions.Single(v => v.Version == 2);

        requirement.ApplyReview(version.Id, RequirementReviewDecision.ApproveWithAssumptions, "PM", "先按不计入处理");
        Assert.Equal(RequirementStatus.Approved, requirement.Status);
    }

    private static PccDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PccDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PccDbContext(options);
    }

    private static CompilerWorkflowService CreateService(PccDbContext db)
    {
        var storageRoot = Path.Combine(Path.GetTempPath(), "pcc-tests", Guid.NewGuid().ToString("N"));
        return new CompilerWorkflowService(
            db,
            new MockStructuredGenerationProvider(),
            new LocalFileStorage(storageRoot));
    }
}
