using System.Reflection;
using System.Xml.Schema;

namespace FpmlToolKit.Tests;

// Smoke tests over every fpml-* project: its schemas compile and its generated assembly loads.
public class AllSchemaProjectsTests
{
    public static TheoryData<string> Projects()
    {
        var data = new TheoryData<string>();
        foreach (var dir in Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Schemas")).Order())
            data.Add(Path.GetFileName(dir));
        return data;
    }

    [Fact]
    public void Every_schema_project_is_covered()
    {
        Assert.Equal(20, Projects().Count);
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
