using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Schema;

namespace FpmlToolKit.Tests;

// The generated <view namespace>.FpmlSchema class in every schema assembly: embedded schemas and validation.
public class FpmlSchemaTests
{
    private static readonly string ExamplesRoot = Path.Combine(AppContext.BaseDirectory, "Examples");

    private static IEnumerable<string> ProjectNames() =>
        Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Schemas")).Select(d => Path.GetFileName(d)!).OrderBy(x => x, StringComparer.Ordinal);

    public static TheoryData<string> Projects() => new(ProjectNames());

    private static Type FpmlSchemaType(string project) =>
        Assembly.Load(project).GetType(project.Replace('-', '_') + ".FpmlSchema", throwOnError: true)!;

    [Theory]
    [MemberData(nameof(Projects))]
    public void Embedded_schema_files_match_the_project_xsd_folder(string project)
    {
        var type = FpmlSchemaType(project);
        var expected = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Schemas", project, "xsd"), "*.xsd")
            .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal);

        var embedded = (IReadOnlyList<string>)type.GetProperty("SchemaFileNames")!.GetValue(null)!;
        var mainSchema = (string)type.GetField("MainSchemaFileName")!.GetValue(null)!;

        Assert.Equal(expected, embedded);
        Assert.Contains(mainSchema, embedded);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Schema_set_compiles_from_embedded_resources_only(string project)
    {
        var schemas = (XmlSchemaSet)FpmlSchemaType(project).GetMethod("CreateSchemaSet")!.Invoke(null, null)!;

        Assert.True(schemas.IsCompiled);
        Assert.Contains(schemas.Schemas().Cast<XmlSchema>(), s => s.TargetNamespace?.StartsWith("http://www.fpml.org/") == true);
    }

    [Theory]
    [MemberData(nameof(OfficialExampleTests.Examples), MemberType = typeof(OfficialExampleTests))]
    public void Official_example_is_valid_according_to_FpmlSchema(string project, string example)
    {
        var validate = FpmlSchemaType(project).GetMethod("Validate", [typeof(XDocument)])!;
        var document = XDocument.Load(Path.Combine(ExamplesRoot, project, example));

        var problems = (IReadOnlyList<ValidationEventArgs>)validate.Invoke(null, [document])!;

        Assert.Empty(problems.Select(p => $"{p.Severity}: {p.Message}"));
    }

    [Fact]
    public void Built_document_is_valid_and_an_incomplete_one_is_not()
    {
        var valid = new fpml_5_3_confirmation.dataDocument
        {
            fpmlVersion = "5-3",
            party = [new fpml_5_3_confirmation.Party { id = "p1", partyId = [new fpml_5_3_confirmation.PartyId { TypedValue = "P1" }] }],
        };
        var missingVersion = new fpml_5_3_confirmation.dataDocument
        {
            party = [new fpml_5_3_confirmation.Party { id = "p1", partyId = [new fpml_5_3_confirmation.PartyId { TypedValue = "P1" }] }],
        };

        Assert.True(fpml_5_3_confirmation.FpmlSchema.IsValid(valid));
        Assert.False(fpml_5_3_confirmation.FpmlSchema.IsValid(missingVersion));
        Assert.Contains(fpml_5_3_confirmation.FpmlSchema.Validate(missingVersion), e => e.Message.Contains("fpmlVersion"));
    }

    [Fact]
    public void Validation_works_in_parallel()
    {
        var examples = Directory.GetFiles(Path.Combine(ExamplesRoot, "fpml-5-13-confirmation"), "*.xml", SearchOption.AllDirectories)
            .Select(XDocument.Load).ToArray();
        var failures = new ConcurrentBag<string>();

        Parallel.For(0, 200, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            var problems = fpml_5_13_confirmation.FpmlSchema.Validate(examples[i % examples.Length]);
            if (problems.Count > 0)
                failures.Add(problems[0].Message);
        });

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_does_not_modify_the_document()
    {
        var path = Directory.GetFiles(Path.Combine(ExamplesRoot, "fpml-5-13-confirmation"), "*.xml", SearchOption.AllDirectories).First();
        var document = XDocument.Load(path);
        var before = document.ToString();

        fpml_5_13_confirmation.FpmlSchema.Validate(document);

        Assert.Equal(before, document.ToString());
        Assert.Null(document.Root!.GetSchemaInfo());
    }

    [Fact]
    public void OpenSchema_returns_the_schema_bytes_or_null()
    {
        using var stream = fpml_4_9.FpmlSchema.OpenSchema(fpml_4_9.FpmlSchema.MainSchemaFileName);

        Assert.NotNull(stream);
        Assert.Equal(new FileInfo(Path.Combine(AppContext.BaseDirectory, "Schemas", "fpml-4-9", "xsd", "fpml-main-4-9.xsd")).Length, stream!.Length);
        Assert.Null(fpml_4_9.FpmlSchema.OpenSchema("no-such-file.xsd"));
    }
}
