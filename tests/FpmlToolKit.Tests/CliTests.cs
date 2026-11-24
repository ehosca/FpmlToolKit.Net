using System.Text.Json;
using FpmlToolKit.Cli;

namespace FpmlToolKit.Tests;

public class CliTests
{
    private static readonly string ExamplesRoot = Path.Combine(AppContext.BaseDirectory, "Examples");
    private static readonly string C513 = Path.Combine(ExamplesRoot, "fpml-5-13-confirmation");

    public static TheoryData<string> ExampleFolders()
    {
        var data = new TheoryData<string>();
        foreach (var dir in Directory.GetDirectories(ExamplesRoot).OrderBy(x => x, StringComparer.Ordinal))
            data.Add(Path.GetFileName(dir));
        return data;
    }

    [Theory]
    [MemberData(nameof(ExampleFolders))]
    public void Detects_every_official_example_as_its_own_version_and_view(string project)
    {
        Assert.All(Directory.GetFiles(Path.Combine(ExamplesRoot, project), "*.xml", SearchOption.AllDirectories), file =>
        {
            var info = FpmlDocumentInfo.Detect(file, out var problem);
            Assert.True(info != null, $"{file}: {problem}");
            Assert.Equal(project, info.Project);
        });
    }

    [Fact]
    public void Ships_a_schema_for_every_schema_project()
    {
        var shipped = SchemaLibrary.Default.Projects.ToList();

        Assert.Equal(Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Schemas")).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal), shipped);
    }

    [Fact]
    public void Valid_document_exits_0()
    {
        var (exit, output) = Run("validate", Path.Combine(C513, "products", "interest-rate-derivatives", "ird-ex08-fra.xml"));

        Assert.Equal(FpmlCommand.Valid, exit);
        Assert.EndsWith("ird-ex08-fra.xml: FpML 5.13 confirmation: valid", output.Trim());
    }

    [Fact]
    public void Rule_violation_exits_1_with_file_line_and_rule()
    {
        var file = Path.Combine(C513, "validation", "invalid-testcases", "invalid-shared-5-01.xml");

        var (exit, output) = Run("validate", file);

        Assert.Equal(FpmlCommand.Invalid, exit);
        Assert.StartsWith($"{file}(184): error shared-5: ", output);
    }

    [Fact]
    public void Folder_is_searched_and_json_lists_each_file()
    {
        var folder = Path.Combine(C513, "products", "interest-rate-derivatives");

        var (exit, output) = Run("validate", folder, "--format", "json");

        using var json = JsonDocument.Parse(output);
        Assert.Equal(Directory.GetFiles(folder, "*.xml").Length, json.RootElement.GetArrayLength());
        Assert.All(json.RootElement.EnumerateArray(), f => Assert.Equal("5.13", f.GetProperty("fpmlVersion").GetString()));
        // ird-ex29, ird-ex31 and ird-ex33 break reference rules (see KnownRuleViolations).
        Assert.Equal(FpmlCommand.Invalid, exit);
    }

    [Fact]
    public void Fpml4_document_is_schema_validated_without_rules()
    {
        var file = Directory.GetFiles(Path.Combine(ExamplesRoot, "fpml-4-4"), "*.xml", SearchOption.AllDirectories).First();

        var (exit, output) = Run("validate", file);

        Assert.Equal(FpmlCommand.Valid, exit);
        Assert.EndsWith("FpML 4.4: valid (no business rules for this view)", output.Trim());
    }

    [Fact]
    public void Missing_file_and_non_fpml_documents_exit_2()
    {
        var notFpml = Path.Combine(AppContext.BaseDirectory, "Schemas", "fpml-5-13-confirmation", "xsd", "fpml-main-5-13.xsd");

        Assert.Equal(FpmlCommand.Error, Run("validate", "does-not-exist.xml").Exit);
        var (exit, output) = Run("validate", notFpml);
        Assert.Equal(FpmlCommand.Error, exit);
        Assert.Contains("not an FpML document", output);
    }

    [Fact]
    public void Bad_usage_exits_2()
    {
        Assert.Equal(FpmlCommand.Error, Run().Exit);
        Assert.Equal(FpmlCommand.Error, Run("validate").Exit);
        Assert.Equal(FpmlCommand.Error, Run("validate", "x.xml", "--format", "yaml").Exit);
        Assert.Equal(FpmlCommand.Error, Run("frobnicate").Exit);
    }

    [Fact]
    public void Rules_lists_every_rule()
    {
        var (exit, output) = Run("rules", "--format", "json");

        Assert.Equal(FpmlCommand.Valid, exit);
        using var json = JsonDocument.Parse(output);
        Assert.Equal(Validation.FpmlRules.All.Count, json.RootElement.GetArrayLength());
    }

    private static (int Exit, string Output) Run(params string[] args)
    {
        var output = new StringWriter();
        var errors = new StringWriter();
        var exit = FpmlCommand.Run(args, output, errors);
        return (exit, output.ToString() + errors);
    }
}
