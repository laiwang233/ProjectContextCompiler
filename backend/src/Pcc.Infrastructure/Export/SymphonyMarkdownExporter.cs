using System.Text;
using Pcc.Domain;
using Pcc.Infrastructure.Json;

namespace Pcc.Infrastructure.Export;

public static class SymphonyMarkdownExporter
{
    public static string Render(
        Project project,
        IReadOnlyList<Requirement> requirements,
        IReadOnlyList<OrchestrationTask> tasks,
        IReadOnlyList<TaskDependency> dependencies)
    {
        var approvedRequirements = requirements
            .Where(requirement => requirement.Status == RequirementStatus.Approved)
            .ToDictionary(requirement => requirement.Id);
        var exportableTasks = tasks
            .Where(task => approvedRequirements.ContainsKey(task.RequirementId))
            .OrderBy(task => task.CreatedAt)
            .ToArray();
        var exportableTaskIds = exportableTasks.Select(task => task.Id).ToHashSet();
        var exportableDependencies = dependencies
            .Where(dependency => exportableTaskIds.Contains(dependency.UpstreamTaskId)
                && exportableTaskIds.Contains(dependency.DownstreamTaskId))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("# Symphony Task Bundle");
        builder.AppendLine();
        builder.AppendLine($"Project: {project.Name}");
        builder.AppendLine($"GeneratedAt: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine();
        builder.AppendLine("## Task Graph");
        builder.AppendLine();
        builder.AppendLine("```mermaid");
        builder.AppendLine("graph TD");
        if (exportableDependencies.Length == 0)
        {
            foreach (var task in exportableTasks)
            {
                builder.AppendLine($"{NodeId(task.Id)}[{EscapeMermaid(task.Title)}]");
            }
        }
        else
        {
            foreach (var dependency in exportableDependencies)
            {
                var upstream = exportableTasks.Single(task => task.Id == dependency.UpstreamTaskId);
                var downstream = exportableTasks.Single(task => task.Id == dependency.DownstreamTaskId);
                builder.AppendLine($"{NodeId(upstream.Id)}[{EscapeMermaid(upstream.Title)}] --> {NodeId(downstream.Id)}[{EscapeMermaid(downstream.Title)}]");
            }
        }

        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Tasks");
        builder.AppendLine();

        foreach (var task in exportableTasks)
        {
            var requirement = approvedRequirements[task.RequirementId];
            var version = requirement.Versions.Single(item => item.Id == task.RequirementVersionId);
            var content = version.GetContent();

            builder.AppendLine($"# TASK: {task.Title}");
            builder.AppendLine();
            builder.AppendLine("## Source Requirement");
            builder.AppendLine($"Requirement: REQ-{requirement.Id}");
            builder.AppendLine($"Version: {version.Version}");
            builder.AppendLine($"Status: {requirement.Status}");
            builder.AppendLine();
            builder.AppendLine("## Objective");
            builder.AppendLine(task.Description);
            builder.AppendLine();
            builder.AppendLine("## Context");
            builder.AppendLine("该任务来自已审核通过的需求对象。");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            foreach (var criterion in JsonDefaults.DeserializeStringList(task.AcceptanceCriteriaJson))
            {
                builder.AppendLine($"- {criterion}");
            }
            builder.AppendLine();
            builder.AppendLine("## Out of Scope");
            WriteList(builder, content.OutOfScope);
            builder.AppendLine();
            builder.AppendLine("## Acceptance Criteria");
            WriteList(builder, JsonDefaults.DeserializeStringList(task.AcceptanceCriteriaJson));
            builder.AppendLine();
            builder.AppendLine("## Evidence");
            WriteList(builder, JsonDefaults.DeserializeStringList(task.EvidenceJson));
            builder.AppendLine();
            builder.AppendLine("## Missing Context");
            WriteList(builder, JsonDefaults.DeserializeStringList(task.MissingContextJson));
            builder.AppendLine();
            builder.AppendLine("## Assumptions");
            WriteList(builder, JsonDefaults.DeserializeStringList(task.AssumptionsJson));
            builder.AppendLine();
            builder.AppendLine("## Agent Prompt");
            builder.AppendLine(task.AgentPrompt);
            builder.AppendLine();
            builder.AppendLine("## Handoff");
            builder.AppendLine("完成后进入 Human Review。");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void WriteList(StringBuilder builder, IEnumerable<string> items)
    {
        var wroteAny = false;
        foreach (var item in items.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            wroteAny = true;
            builder.AppendLine($"- {item}");
        }

        if (!wroteAny)
        {
            builder.AppendLine("- 无");
        }
    }

    private static string NodeId(Guid id) => "T" + id.ToString("N")[..8];

    private static string EscapeMermaid(string text)
    {
        return text.Replace("[", "(", StringComparison.Ordinal)
            .Replace("]", ")", StringComparison.Ordinal)
            .Replace("\"", "'", StringComparison.Ordinal);
    }
}
