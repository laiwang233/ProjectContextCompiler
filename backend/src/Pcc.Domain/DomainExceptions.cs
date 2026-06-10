namespace Pcc.Domain;

public class DomainRuleViolationException(string message) : InvalidOperationException(message);

public sealed class RequirementNotApprovedException(Guid requirementId)
    : DomainRuleViolationException("只有已审核通过的需求才能生成可执行任务")
{
    public Guid RequirementId { get; } = requirementId;
}

public sealed class ContentBlocksNotVerifiedException(Guid projectId)
    : DomainRuleViolationException("所有证据块必须先被确认或忽略，才能抽取 Claim")
{
    public Guid ProjectId { get; } = projectId;
}

public sealed class NoConfirmedContentBlocksException(Guid projectId)
    : DomainRuleViolationException("至少需要一个已确认的证据块才能抽取 Claim")
{
    public Guid ProjectId { get; } = projectId;
}
