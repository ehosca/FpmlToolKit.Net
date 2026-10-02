using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Schema;
using Xml.Schema.Linq;

namespace FpmlToolKit.Tests;

// Official FpML example documents (Examples/<project>/..., imported by scripts/add-schema-version.cs).
// Each must validate against its view's schema, load through the generated XRoot API, and round-trip unchanged.
public class OfficialExampleTests
{
    private static readonly string ExamplesRoot = Path.Combine(AppContext.BaseDirectory, "Examples");
    private static readonly ConcurrentDictionary<string, XmlSchemaSet> SchemaCache = new();

    public static TheoryData<string, string> Examples()
    {
        var data = new TheoryData<string, string>();
        foreach (var projectDir in Directory.GetDirectories(ExamplesRoot).Order(StringComparer.Ordinal))
        {
            foreach (var file in Directory.GetFiles(projectDir, "*.xml", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                data.Add(Path.GetFileName(projectDir), Path.GetRelativePath(projectDir, file).Replace('\\', '/'));
        }
        return data;
    }

    [Fact]
    public void Every_example_folder_has_a_schema_project()
    {
        var schemaProjects = Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Schemas")).Select(Path.GetFileName).ToHashSet();

        Assert.All(Directory.GetDirectories(ExamplesRoot).Select(Path.GetFileName), p => Assert.Contains(p, schemaProjects));
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void Example_is_schema_valid(string project, string example)
    {
        var document = XDocument.Load(PathOf(project, example));

        Assert.Empty(SchemaValidator.Validate(document, SchemasFor(project)));
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void Example_loads_typed_and_round_trips(string project, string example)
    {
        var path = PathOf(project, example);

        var root = LoadTyped(project, path);

        Assert.NotNull(root);
        Assert.True(XNode.DeepEquals(XElement.Load(path), XElement.Parse(root.Untyped.ToString())),
            "the typed document does not serialize back to the original XML");
    }

    private static string PathOf(string project, string example) => Path.Combine(ExamplesRoot, project, example);

    private static XmlSchemaSet SchemasFor(string project) =>
        SchemaCache.GetOrAdd(project, p =>
            SchemaValidator.Load(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Schemas", p, "xsd"), "fpml-main-*.xsd").Single()));

    // XRoot.Load(path).Root, via reflection: every schema project generates its own XRoot type.
    private static XTypedElement LoadTyped(string project, string path)
    {
        var xroot = Assembly.Load(project).GetType(project.Replace('-', '_') + ".XRoot", throwOnError: true)!;
        var loaded = xroot.GetMethod("Load", [typeof(string)])!.Invoke(null, [path]);
        return (XTypedElement)xroot.GetProperty("Root")!.GetValue(loaded)!;
    }
}
