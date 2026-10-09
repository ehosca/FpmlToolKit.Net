using System.Security.Cryptography;

namespace FpmlToolKit.Tests;

// The FpML schemas and examples are unchanged copies of FpML's downloads, as listed in official-files.txt
// (scripts/official-files.cs). Fails when one is edited, re-encoded (line endings) or added without being listed.
public class OfficialFilesTests
{
    private static readonly string[] ManifestFiles = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "official-files.txt"))
        .Where(l => l.StartsWith("file ", StringComparison.Ordinal))
        .ToArray();

    public static TheoryData<string, string> Listed()
    {
        var data = new TheoryData<string, string>();
        foreach (var line in ManifestFiles)
        {
            var parts = line.Split(' ');
            data.Add(parts[2], parts[1]);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Listed))]
    public void File_is_unchanged(string repoPath, string sha256)
    {
        var bytes = File.ReadAllBytes(OutputPath(repoPath));

        Assert.Equal(sha256, ToHex(SHA256.Create().ComputeHash(bytes)));
    }

    [Fact]
    public void Every_schema_and_example_is_listed()
    {
        var listed = Listed().Select(row => OutputPath((string)row[0])).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var present = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Schemas"), "*.xsd", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Examples"), "*", SearchOption.AllDirectories))
            .Select(Path.GetFullPath);

        Assert.All(present, p => Assert.Contains(p, listed));
    }

    // fpml-5-13-legal/xsd/x.xsd -> Schemas/fpml-5-13-legal/xsd/x.xsd; tests/FpmlToolKit.Tests/Examples/... -> Examples/...
    private static string OutputPath(string repoPath)
    {
        const string examples = "tests/FpmlToolKit.Tests/";
        var relative = repoPath.StartsWith(examples, StringComparison.Ordinal) ? repoPath.Substring(examples.Length) : "Schemas/" + repoPath;
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));
    }

    private static string ToHex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
}
