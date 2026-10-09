using System.Reflection;
using System.Xml.Schema;

namespace FpmlToolKit.Tests;

// Smoke tests over every fpml-* project: its schemas compile and its generated assembly loads.
public class AllSchemaProjectsTests
{
    private static IEnumerable<string> ProjectNames() =>
        Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Schemas")).Select(d => Path.GetFileName(d)!).OrderBy(x => x, StringComparer.Ordinal);

    public static TheoryData<string> Projects() => new(ProjectNames());

    // Guards the wildcard schema links in the test project: every referenced fpml-* assembly needs its schemas.
    [Fact]
    public void Every_schema_project_is_covered()
    {
        var assemblies = Directory.GetFiles(AppContext.BaseDirectory, "fpml-*.dll").Select(f => Path.GetFileNameWithoutExtension(f)!).OrderBy(x => x, StringComparer.Ordinal);

        Assert.Equal(assemblies, ProjectNames());
        Assert.True(ProjectNames().Count() >= 20);
    }

    // A consumer may reference several schema assemblies at once (every view of a version ships in one package), so a
    // type full name defined by two assemblies makes that name unusable (CS0433).
    [Fact]
    public void No_type_is_defined_by_more_than_one_schema_assembly()
    {
        var duplicates = ProjectNames()
            .SelectMany(p => Assembly.Load(p).GetExportedTypes().Select(t => (Type: t.FullName!, Assembly: p)))
            .GroupBy(t => t.Type)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} ({g.Count()} assemblies, e.g. {string.Join(", ", g.Take(2).Select(t => t.Assembly))})")
            .ToList();

        Assert.True(duplicates.Count == 0, $"{duplicates.Count} type names are defined by more than one assembly:\n" + string.Join("\n", duplicates.Take(10)));
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

        // Not every view uses XML signatures (5.13 legal does not), so only the FpML namespace is required.
        Assert.Contains(types, t => t.Namespace == rootNamespace);
    }
}
