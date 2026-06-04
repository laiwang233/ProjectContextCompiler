namespace Pcc.Domain;

public sealed record RequirementContent
{
    public IReadOnlyList<string> Actors { get; init; } = [];
    public IReadOnlyList<string> Preconditions { get; init; } = [];
    public IReadOnlyList<string> BusinessRules { get; init; } = [];
    public IReadOnlyList<string> MainFlow { get; init; } = [];
    public IReadOnlyList<string> ExceptionFlows { get; init; } = [];
    public IReadOnlyList<string> AcceptanceCriteria { get; init; } = [];
    public IReadOnlyList<string> OpenQuestions { get; init; } = [];
    public IReadOnlyList<string> OutOfScope { get; init; } = [];
    public IReadOnlyList<string> DeferredScope { get; init; } = [];
    public IReadOnlyList<string> Risks { get; init; } = [];
    public IReadOnlyList<string> Assumptions { get; init; } = [];
    public IReadOnlyList<Guid> SourceClaimIds { get; init; } = [];

    public static RequirementContent Empty { get; } = new();
}
