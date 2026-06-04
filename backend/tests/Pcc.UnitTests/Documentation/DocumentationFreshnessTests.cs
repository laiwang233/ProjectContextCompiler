using System.Text.RegularExpressions;

namespace Pcc.UnitTests.Documentation;

public sealed class DocumentationFreshnessTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Required_agent_facing_documents_exist()
    {
        string[] requiredPaths =
        [
            "AGENTS.md",
            "README.md",
            "docs/README.md",
            "docs/harness-repoization.md",
            "docs/architecture.md",
            "docs/operations.md",
            "docs/testing.md",
            "docs/troubleshooting.md",
            "docs/decisions.md",
            "docs/design-docs/index.md",
            "docs/design-docs/2026-06-04-project-context-compiler-mvp.md",
            "docs/exec-plans/README.md",
            "docs/exec-plans/active/README.md",
            "docs/exec-plans/completed/README.md",
            "docs/exec-plans/tech-debt-tracker.md"
        ];

        foreach (var relativePath in requiredPaths)
        {
            Assert.True(File.Exists(PathAt(relativePath)), $"Missing required document: {relativePath}");
        }
    }

    [Fact]
    public void Markdown_links_in_agent_facing_documents_resolve()
    {
        var markdownFiles = Directory.EnumerateFiles(RepositoryRoot, "*.md", SearchOption.AllDirectories)
            .Where(IsAgentFacingMarkdown)
            .OrderBy(path => path)
            .ToArray();

        Assert.NotEmpty(markdownFiles);
        foreach (var file in markdownFiles)
        {
            var text = File.ReadAllText(file);
            foreach (Match match in Regex.Matches(text, @"\[[^\]]+\]\(([^)#]+)(?:#[^)]+)?\)"))
            {
                var target = match.Groups[1].Value;
                if (IsExternalLink(target))
                {
                    continue;
                }

                var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, target));
                Assert.True(File.Exists(resolved) || Directory.Exists(resolved),
                    $"{Relative(file)} links to missing target: {target}");
            }
        }
    }

    [Fact]
    public void Readme_harness_entries_are_clickable_links()
    {
        var readme = File.ReadAllText(PathAt("README.md"));
        string[] requiredLinkTargets =
        [
            "AGENTS.md",
            "docs/README.md",
            "docs/harness-repoization.md",
            "docs/architecture.md",
            "docs/operations.md",
            "docs/testing.md"
        ];

        foreach (var target in requiredLinkTargets)
        {
            Assert.Contains($"]({target})", readme, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Completed_documentation_freshness_work_is_not_listed_as_active_tech_debt()
    {
        var tracker = File.ReadAllText(PathAt("docs/exec-plans/tech-debt-tracker.md"));

        Assert.DoesNotContain("新增文档新鲜度测试", tracker, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_superpowers_docs_are_not_a_canonical_source_of_truth()
    {
        Assert.False(Directory.Exists(PathAt("docs/superpowers")),
            "docs/superpowers should not be recreated as a canonical source of truth.");
    }

    private static bool IsAgentFacingMarkdown(string path)
    {
        var relative = Relative(path).Replace('\\', '/');
        return relative is "README.md" or "AGENTS.md"
            || relative.StartsWith("docs/", StringComparison.Ordinal);
    }

    private static bool IsExternalLink(string target)
    {
        return Regex.IsMatch(target, @"^[a-zA-Z][a-zA-Z0-9+.-]*:");
    }

    private static string PathAt(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string Relative(string path)
    {
        return Path.GetRelativePath(RepositoryRoot, path);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProjectContextCompiler.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
