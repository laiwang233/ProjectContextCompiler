namespace Pcc.Domain;

public class DomainRuleViolationException(string message) : InvalidOperationException(message);

public sealed class RequirementNotApprovedException(Guid requirementId)
    : DomainRuleViolationException("只有已审核通过的需求才能生成可执行任务")
{
    public Guid RequirementId { get; } = requirementId;
}
