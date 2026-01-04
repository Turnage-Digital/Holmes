using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Holmes.Architecture.Tests;

public sealed class MediatorUsageRulesTests
{
    [Test]
    public void Non_get_controller_actions_call_mediator_send_once()
    {
        var repoRoot = FindRepoRoot();
        var controllersRoot = Path.Combine(repoRoot, "src");

        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(controllersRoot, "*Controller.cs", SearchOption.AllDirectories))
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!HasHttpVerbAttribute(method, "HttpPost", "HttpPut", "HttpDelete", "HttpPatch"))
                {
                    continue;
                }

                var sendCount = method
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Count(IsMediatorSendInvocation);

                if (sendCount != 1)
                {
                    violations.Add(
                        $"{Path.GetRelativePath(repoRoot, file)}: {method.Identifier.Text} has {sendCount} Send() calls");
                }
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Non-GET controller actions must call mediator.Send exactly once.\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Command_handlers_do_not_call_mediator_send()
    {
        var repoRoot = FindRepoRoot();
        var modulesRoot = Path.Combine(repoRoot, "src", "Modules");

        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (!file.Contains(".Application", StringComparison.Ordinal))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (var @class in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (!@class.Identifier.Text.EndsWith("CommandHandler", StringComparison.Ordinal))
                {
                    continue;
                }

                var sendCount = @class
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Count(IsMediatorSendInvocation);

                if (sendCount == 0)
                {
                    continue;
                }

                violations.Add(
                    $"{Path.GetRelativePath(repoRoot, file)}: {@class.Identifier.Text} has {sendCount} Send() calls");
            }
        }

        Assert.That(
            violations,
            Is.Empty,
            "Command handlers must not call mediator.Send.\n" +
            string.Join(Environment.NewLine, violations));
    }

    private static bool HasHttpVerbAttribute(MethodDeclarationSyntax method, params string[] names)
    {
        foreach (var attribute in method.AttributeLists.SelectMany(list => list.Attributes))
        {
            var name = attribute.Name.ToString();
            foreach (var target in names)
            {
                if (name.EndsWith(target, StringComparison.Ordinal) ||
                    name.EndsWith($"{target}Attribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsMediatorSendInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text == "Send",
            IdentifierNameSyntax identifier => identifier.Identifier.Text == "Send",
            _ => false
        };
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
}