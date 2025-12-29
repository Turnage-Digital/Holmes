using System.Reflection;

namespace Holmes.Architecture.Tests;

public sealed class DomainReferenceRulesTests
{
    private static readonly string[] DomainAssemblyNames =
    [
        "Holmes.Core.Domain",
        "Holmes.Customers.Domain",
        "Holmes.IntakeSessions.Domain",
        "Holmes.Notifications.Domain",
        "Holmes.Orders.Domain",
        "Holmes.Services.Domain",
        "Holmes.SlaClocks.Domain",
        "Holmes.Subjects.Domain",
        "Holmes.Users.Domain"
    ];

    [Test]
    public void Domain_projects_only_reference_core_domain()
    {
        foreach (var assemblyName in DomainAssemblyNames)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var holmesReferences = assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(name => name is not null && name.StartsWith("Holmes.", StringComparison.Ordinal))
                .ToArray();

            var allowed = assemblyName == "Holmes.Core.Domain"
                ? Array.Empty<string>()
                : ["Holmes.Core.Domain"];

            var forbidden = holmesReferences
                .Where(reference => !allowed.Contains(reference, StringComparer.Ordinal))
                .ToArray();

            Assert.That(
                forbidden,
                Is.Empty,
                $"{assemblyName} references other Holmes assemblies: {string.Join(", ", forbidden)}");
        }
    }
}
