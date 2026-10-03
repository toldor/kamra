using System.Reflection;

namespace KamraApp.Unit.Tests;

// ADR-0004: dependency direction is enforced by project references; these tests catch a
// forbidden reference that is actually used. Unused references never reach the IL and are harmless.
public class LayerRulesTests
{
    private static readonly string[] ForbiddenForApplication =
    [
        "KamraApp.Infrastructure",
        "KamraApp.Api",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
    ];

    [Fact]
    public void Domain_references_only_system_assemblies()
    {
        var references = ReferencedAssemblyNames("KamraApp.Domain");

        references.Should().OnlyContain(name => name == "netstandard" || name.StartsWith("System", StringComparison.Ordinal),
            "ADR-0004: the Domain depends on nothing");
    }

    [Fact]
    public void Application_does_not_reference_outer_layers_or_frameworks()
    {
        var references = ReferencedAssemblyNames("KamraApp.Application");

        references.Should().NotContain(name => ForbiddenForApplication.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)),
            "ADR-0004: the Application depends only on the Domain");
    }

    private static string[] ReferencedAssemblyNames(string assemblyName) =>
        Assembly.Load(assemblyName).GetReferencedAssemblies().Select(a => a.Name!).ToArray();
}
