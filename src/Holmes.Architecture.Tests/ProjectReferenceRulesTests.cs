using System.Text.RegularExpressions;

namespace Holmes.Architecture.Tests;

public sealed class ProjectReferenceRulesTests
{
    private static readonly Regex ProjectReferenceRegex = new(
        "ProjectReference Include=\"([^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Test]
    public void Cross_module_references_are_only_allowed_from_application_to_abstractions()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");

        var violations = new List<string>();

        foreach (var csproj in Directory.EnumerateFiles(modulesRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var sourceModule = GetModuleName(modulesRoot, csproj);
            var sourceLayer = GetLayer(Path.GetFileNameWithoutExtension(csproj));
            if (sourceModule is null || sourceLayer is null)
            {
                continue;
            }

            var contents = File.ReadAllText(csproj);
            foreach (Match match in ProjectReferenceRegex.Matches(contents))
            {
                var include = match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
                var targetPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, include));
                if (!targetPath.StartsWith(modulesRoot, StringComparison.Ordinal))
                {
                    continue;
                }

                var targetModule = GetModuleName(modulesRoot, targetPath);
                var targetLayer = GetLayer(Path.GetFileNameWithoutExtension(targetPath));
                if (targetModule is null || targetLayer is null)
                {
                    continue;
                }

                if (string.Equals(sourceModule, targetModule, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(targetModule, "Core", StringComparison.Ordinal))
                {
                    continue;
                }

                var allowed = sourceLayer == "Application" && targetLayer == "Application.Abstractions";
                if (!allowed)
                {
                    var relativeSource = Path.GetRelativePath(modulesRoot, csproj);
                    var relativeTarget = Path.GetRelativePath(modulesRoot, targetPath);
                    violations.Add($"{relativeSource} ({sourceLayer}) -> {relativeTarget} ({targetLayer})");
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Cross-module references are only allowed from Application to Application.Abstractions.\n" +
            string.Join(Environment.NewLine, violations));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Holmes.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Holmes.sln from the test working directory.");
    }

    private static string? GetModuleName(string modulesRoot, string projectPath)
    {
        var relative = Path.GetRelativePath(modulesRoot, projectPath);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length > 1 ? parts[0] : null;
    }

    private static string? GetLayer(string projectName)
    {
        if (projectName.EndsWith(".Application.Abstractions", StringComparison.Ordinal))
        {
            return "Application.Abstractions";
        }

        if (projectName.EndsWith(".Infrastructure.Sql", StringComparison.Ordinal))
        {
            return "Infrastructure.Sql";
        }

        if (projectName.EndsWith(".Application", StringComparison.Ordinal))
        {
            return "Application";
        }

        if (projectName.EndsWith(".Domain", StringComparison.Ordinal))
        {
            return "Domain";
        }

        if (projectName.EndsWith(".Tests", StringComparison.Ordinal))
        {
            return "Tests";
        }

        return null;
    }
}
