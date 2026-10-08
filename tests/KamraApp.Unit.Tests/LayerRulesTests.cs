using System.Reflection;
using System.Xml.Linq;

namespace KamraApp.Unit.Tests;

// ADR-0004: "no forbidden project or package reference" in the Domain and Application layers.
// The assembly tests catch references used in code; the project-file tests catch declared but unused ones.
public class LayerRulesTests
{
    private static readonly string[] ExternalReferenceItems = ["PackageReference", "FrameworkReference"];

    [Fact]
    public void Domain_references_only_system_assemblies()
    {
        var references = ReferencedAssemblyNames("KamraApp.Domain");

        references.Should().OnlyContain(name => IsSystemAssembly(name),
            "ADR-0004: the Domain depends on nothing");
    }

    [Fact]
    public void Application_references_only_system_assemblies_and_the_domain()
    {
        var references = ReferencedAssemblyNames("KamraApp.Application");

        references.Should().OnlyContain(name => IsSystemAssembly(name) || name == "KamraApp.Domain",
            "ADR-0004: the Application depends only on the Domain");
    }

    [Fact]
    public void Domain_project_declares_no_project_or_external_references()
    {
        var project = LoadProject("KamraApp.Domain");

        ItemIncludes(project, "ProjectReference").Should().BeEmpty("ADR-0004: the Domain depends on nothing");
        ItemIncludes(project, ExternalReferenceItems).Should().BeEmpty("ADR-0004: the Domain depends on nothing");
    }

    [Fact]
    public void Application_project_declares_only_the_domain_project_reference()
    {
        var project = LoadProject("KamraApp.Application");

        ItemIncludes(project, "ProjectReference")
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .Should().Equal(["KamraApp.Domain"], "ADR-0004: the Application depends only on the Domain");
        ItemIncludes(project, ExternalReferenceItems).Should().BeEmpty("ADR-0004: the Application depends only on the Domain");
    }

    private static bool IsSystemAssembly(string name) =>
        name == "netstandard" || name.StartsWith("System", StringComparison.Ordinal);

    private static string[] ReferencedAssemblyNames(string assemblyName) =>
        Assembly.Load(assemblyName).GetReferencedAssemblies().Select(a => a.Name!).ToArray();

    private static XDocument LoadProject(string projectName)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "KamraApp.slnx")))
        {
            root = root.Parent ?? throw new InvalidOperationException("KamraApp.slnx not found above the test output directory.");
        }

        return XDocument.Load(Path.Combine(root.FullName, "src", "backend", projectName, $"{projectName}.csproj"));
    }

    private static string[] ItemIncludes(XDocument project, params string[] itemNames) =>
        project.Descendants()
            .Where(e => itemNames.Contains(e.Name.LocalName))
            .Select(e => (string?)e.Attribute("Include") ?? "")
            .ToArray();
}
