using System.Reflection;
using System.Xml.Schema;

namespace FpmlToolKit.Tests;

// Smoke tests over every fpml-* project: its schemas compile and its generated assembly loads.
public class AllSchemaProjectsTests
{
    private static IEnumerable<string> ProjectNames() =>
        Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Schemas")).Select(d => Path.GetFileName(d)!).Order();

    public static TheoryData<string> Projects() => new(ProjectNames());

    // Guards the wildcard schema links in the test project: every referenced fpml-* assembly needs its schemas.
    [Fact]
    public void Every_schema_project_is_covered()
    {
        var assemblies = Directory.GetFiles(AppContext.BaseDirectory, "fpml-*.dll").Select(f => Path.GetFileNameWithoutExtension(f)!).Order();

        Assert.Equal(assemblies, ProjectNames());
        Assert.True(ProjectNames().Count() >= 20);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Schema_compiles(string project)
    {
        var mainSchema = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Schemas", project, "xsd"), "fpml-main-*.xsd").Single();

        XmlSchemaSet schemas = SchemaValidator.Load(mainSchema);

        Assert.True(schemas.IsCompiled);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Generated_assembly_loads_all_types(string project)
    {
        var assembly = Assembly.Load(project);
        var rootNamespace = project.Replace('-', '_');

        var types = assembly.GetTypes();

        Assert.Contains(types, t => t.Namespace == rootNamespace);
        Assert.Contains(types, t => t.Namespace == "org.w3.xmldsig");
    }
}
