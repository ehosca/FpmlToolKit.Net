using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;
using FpmlToolKit.Validation;

namespace FpmlToolKit.Tests;

// The business rules, checked against FpML's own test data:
// - validation/**/invalid-<rule>-NN.xml must raise exactly <rule>, if implemented, and nothing else;
// - every other official 5.x example (products, business processes, valid-* test cases) must raise nothing.
// The second check is what catches a rule that is too strict.
public class ValidationRuleTests
{
    private static readonly string ExamplesRoot = Path.Combine(AppContext.BaseDirectory, "Examples");
    private static readonly Dictionary<string, XmlSchemaSet> SchemaCache = new();
    private static readonly HashSet<string> Implemented = FpmlRules.All.Select(r => r.Id).ToHashSet();

    private static readonly Regex InvalidTestCase = new(@"^invalid-(?<rules>.+)-\d{2}\.xml$");
    private static readonly Regex RuleIds = new(@"^(?<family>.+?)-(?<numbers>\d+[a-z]?(?:-\d+[a-z]?)*)$");

    public static TheoryData<string, string> Fpml5Examples()
    {
        var data = new TheoryData<string, string>();
        foreach (var projectDir in Directory.GetDirectories(ExamplesRoot, "fpml-5-*").OrderBy(x => x, StringComparer.Ordinal))
            foreach (var file in Directory.GetFiles(projectDir, "*.xml", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
                data.Add(Path.GetFileName(projectDir), file.Substring(projectDir.Length + 1).Replace('\\', '/'));
        return data;
    }

    // invalid-shared-1-01.xml → shared-1; invalid-repo-sftr-26-27-01.xml → repo-sftr-26, repo-sftr-27.
    internal static IReadOnlyList<string> ExpectedRules(string fileName)
    {
        var testCase = InvalidTestCase.Match(fileName);
        if (!testCase.Success)
            return [];
        var ids = RuleIds.Match(testCase.Groups["rules"].Value);
        return ids.Groups["numbers"].Value.Split('-').Select(n => $"{ids.Groups["family"].Value}-{n}").ToArray();
    }

    [Theory]
    [MemberData(nameof(Fpml5Examples))]
    public void Example_raises_exactly_its_rule(string project, string example)
    {
        var expected = ExpectedFor(project, example);

        var report = Validate(project, example);

        // FpML's shared and reference rules don't cover the pretrade and legal views.
        Assert.Equal(!project.EndsWith("-pretrade") && !project.EndsWith("-legal"), report.RulesApplied);
        Assert.Equal(expected.OrderBy(x => x, StringComparer.Ordinal), report.RuleViolations.Select(v => v.RuleId).Distinct().OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void Every_rule_is_raised_by_some_official_file()
    {
        var raised = Fpml5Examples().SelectMany(row => ExpectedFor((string)row[0], (string)row[1])).ToHashSet();

        // ref-10 is the only implemented rule FpML publishes without a test case.
        Assert.Empty(Implemented.Except(raised).Except(["ref-10"]).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void Known_violations_name_existing_files_and_rules()
    {
        Assert.All(KnownRuleViolations.All, e =>
        {
            Assert.True(File.Exists(Path.Combine(ExamplesRoot, e.Project, e.Example)), $"{e.Project}/{e.Example} is missing");
            Assert.All(e.AlsoRaises.Concat(e.DoesNotRaise), r => Assert.Contains(r, Implemented));
        });
    }

    // The rules the file name says it breaks, adjusted by the reviewed differences in KnownRuleViolations.
    private static HashSet<string> ExpectedFor(string project, string example)
    {
        var expected = ExpectedRules(Path.GetFileName(example)).Where(Implemented.Contains).ToHashSet();
        if (KnownRuleViolations.For(project, example) is { } known)
        {
            expected.UnionWith(known.AlsoRaises);
            expected.ExceptWith(known.DoesNotRaise);
        }
        return expected;
    }

    [Fact]
    public void Expected_rules_are_parsed_from_test_case_names()
    {
        Assert.Equal(["shared-1"], ExpectedRules("invalid-shared-1-01.xml"));
        Assert.Equal(["cd-21b"], ExpectedRules("invalid-cd-21b-03.xml"));
        Assert.Equal(["repo-sftr-26", "repo-sftr-27"], ExpectedRules("invalid-repo-sftr-26-27-01.xml"));
        Assert.Empty(ExpectedRules("valid-ref-38-01.xml"));
    }

    [Fact]
    public void Violation_reports_the_callers_element_with_line_and_path()
    {
        var path = Path.Combine(ExamplesRoot, "fpml-5-13-confirmation", "validation", "invalid-testcases", "invalid-shared-5-01.xml");
        var document = XDocument.Load(path, LoadOptions.SetLineInfo);
        var before = document.ToString(SaveOptions.DisableFormatting);

        var violation = Assert.Single(FpmlValidator.Validate(document, SchemasFor("fpml-5-13-confirmation")).RuleViolations);

        Assert.Equal("shared-5", violation.RuleId);
        Assert.Same(document, violation.Element.Document);
        Assert.NotNull(violation.LineNumber);
        Assert.StartsWith("/executionNotification/trade/", violation.Path);
        Assert.Equal(before, document.ToString(SaveOptions.DisableFormatting));
    }

    [Fact]
    public void Rules_are_not_run_on_fpml_4()
    {
        var project = "fpml-4-4";
        var example = Directory.GetFiles(Path.Combine(ExamplesRoot, project), "*.xml", SearchOption.AllDirectories).First();

        var report = FpmlValidator.Validate(XDocument.Load(example), SchemasFor(project));

        Assert.False(report.RulesApplied);
        Assert.Empty(report.RuleViolations);
    }

    [Fact]
    public void Rules_have_unique_ids_and_specification_links()
    {
        Assert.Equal(FpmlRules.All.Count, Implemented.Count);
        Assert.All(FpmlRules.All, r => Assert.EndsWith("#" + r.Id, r.SpecificationUri.ToString()));
        Assert.Same(FpmlRules.All[0], FpmlRules.Find(FpmlRules.All[0].Id.ToUpperInvariant()));
    }

    private static ValidationReport Validate(string project, string example) =>
        FpmlValidator.Validate(XDocument.Load(Path.Combine(ExamplesRoot, project, example)), SchemasFor(project));

    private static XmlSchemaSet SchemasFor(string project)
    {
        lock (SchemaCache)
        {
            if (!SchemaCache.TryGetValue(project, out var schemas))
                SchemaCache[project] = schemas = SchemaValidator.Load(
                    Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Schemas", project, "xsd"), "fpml-main-*.xsd").Single());
            return schemas;
        }
    }
}
