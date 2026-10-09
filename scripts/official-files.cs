// Records and checks where every official FpML file in the repository comes from.
//
//   dotnet run scripts/official-files.cs -- write <zips-folder>
//   dotnet run scripts/official-files.cs -- verify [<zips-folder>]
//
// The FpML schemas (fpml-*/xsd/*.xsd) and example documents (tests/FpmlToolKit.Tests/Examples/**) are copies of files
// in FpML's "Schema and Examples" downloads, unchanged. official-files.txt lists each one with its SHA-256 and the zip
// and entry it was copied from; the zips are listed with their own SHA-256.
//
// <zips-folder> holds the downloads under their path on fpml.org, without the https://www.fpml.org/spec/ prefix
// (for example <zips-folder>/fpml-5-13-8-rec-2/xml/confirmation-5-13_xml.zip).
//
// write   Finds, for every project, the download folder its schemas were copied from: the folder holding a
//         byte-identical copy of its main schema and of every other schema, and nothing else. When several downloads
//         have one, the newest build is used. The project's examples must be byte-identical files in that folder.
//         Fails, without writing, if any file has no such source.
// verify  Checks that every official file in the repository is listed with its current SHA-256 (the test project
//         checks the same). With <zips-folder>, or after downloading the listed zips from fpml.org when it is
//         omitted, also checks each zip and each listed entry against the SHA-256 recorded for it.

#nullable enable

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

const string ManifestName = "official-files.txt";
const string BaseUrl = "https://www.fpml.org/spec/";

var repoRoot = FindRepoRoot();
var manifestPath = Path.Combine(repoRoot, ManifestName);

return args switch
{
    ["write", var zips] => Write(zips),
    ["verify"] => Verify(null),
    ["verify", var zips] => Verify(zips),
    _ => Usage(),
};

int Usage()
{
    Console.Error.WriteLine("usage: dotnet run scripts/official-files.cs -- write <zips-folder> | verify [<zips-folder>]");
    return 2;
}

int Write(string zipsFolder)
{
    // Every zip in the folder, newest build first, with its entries' SHA-256s.
    var zips = Directory.GetFiles(zipsFolder, "*.zip", SearchOption.AllDirectories)
        .Select(p => Path.GetRelativePath(zipsFolder, p).Replace('\\', '/'))
        .OrderByDescending(BuildKey, StringComparer.Ordinal)
        .ThenBy(p => p, StringComparer.Ordinal)
        .Select(p => (Path: p, Sha: Sha(File.ReadAllBytes(Path.Combine(zipsFolder, p))), Entries: ReadEntries(Path.Combine(zipsFolder, p))))
        .ToList();

    var lines = new List<string>();
    var usedZips = new SortedDictionary<string, string>(StringComparer.Ordinal);
    var problems = new List<string>();

    foreach (var project in Projects())
    {
        var xsdDir = Path.Combine(repoRoot, project, "xsd");
        var schemas = Directory.GetFiles(xsdDir, "*.xsd").ToDictionary(f => Path.GetFileName(f), f => Sha(File.ReadAllBytes(f)));
        var main = schemas.Keys.Single(n => n.StartsWith("fpml-main-"));

        // The download folder whose *.xsd files are exactly this project's schemas.
        var source = zips
            .SelectMany(z => z.Entries.Where(e => e.Key.EndsWith("/" + main) || e.Key == main).Select(e => (Zip: z, Dir: DirOf(e.Key))))
            .FirstOrDefault(c =>
            {
                var folder = c.Zip.Entries.Where(e => DirOf(e.Key) == c.Dir && e.Key.EndsWith(".xsd")).ToDictionary(e => e.Key[c.Dir.Length..], e => e.Value);
                return folder.Count == schemas.Count && schemas.All(s => folder.TryGetValue(s.Key, out var sha) && sha == s.Value);
            });
        if (source.Zip.Path is null)
        {
            problems.Add($"{project}: no download has a folder with exactly these schemas");
            continue;
        }
        usedZips[source.Zip.Path] = source.Zip.Sha;

        foreach (var (name, sha) in schemas.OrderBy(s => s.Key, StringComparer.Ordinal))
            lines.Add(Line(sha, $"{project}/xsd/{name}", source.Zip.Path, source.Dir + name));

        var examplesDir = Path.Combine(repoRoot, "tests", "FpmlToolKit.Tests", "Examples", project);
        if (!Directory.Exists(examplesDir))
            continue;
        foreach (var file in Directory.GetFiles(examplesDir, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(examplesDir, file).Replace('\\', '/');
            var sha = Sha(File.ReadAllBytes(file));
            if (source.Zip.Entries.TryGetValue(source.Dir + relative, out var official) && official == sha)
                lines.Add(Line(sha, $"tests/FpmlToolKit.Tests/Examples/{project}/{relative}", source.Zip.Path, source.Dir + relative));
            else
                problems.Add($"{project}: example {relative} is not a byte-identical copy of {source.Zip.Path}!{source.Dir}{relative}");
        }
    }

    if (problems.Count > 0)
    {
        foreach (var problem in problems)
            Console.Error.WriteLine(problem);
        Console.Error.WriteLine($"{ManifestName} not written");
        return 1;
    }

    var text = new StringBuilder();
    text.Append($"""
        # Official FpML files in this repository, unchanged copies of FpML's downloads. Generated by
        # scripts/official-files.cs ("write"); checked by it ("verify") and by the test project. Do not edit.
        #
        # zip <SHA-256> <download, relative to {BaseUrl}>
        # file <SHA-256> <file in this repository> <download> <entry in the download>

        """);
    foreach (var (path, sha) in usedZips)
        text.Append($"zip {sha} {path}\n");
    foreach (var line in lines)
        text.Append(line).Append('\n');
    File.WriteAllText(manifestPath, text.ToString());
    Console.WriteLine($"{ManifestName}: {lines.Count} files from {usedZips.Count} downloads");
    return 0;
}

int Verify(string? zipsFolder)
{
    var (zipShas, files) = ReadManifest();
    var problems = new List<string>();

    var listed = files.Select(f => f.RepoPath).ToHashSet(StringComparer.Ordinal);
    foreach (var path in OfficialFilesInRepo().Where(p => !listed.Contains(p)))
        problems.Add($"{path}: not in {ManifestName}");
    foreach (var f in files)
    {
        var full = Path.Combine(repoRoot, f.RepoPath);
        if (!File.Exists(full))
            problems.Add($"{f.RepoPath}: listed in {ManifestName} but missing");
        else if (Sha(File.ReadAllBytes(full)) != f.Sha)
            problems.Add($"{f.RepoPath}: changed (SHA-256 differs from {ManifestName})");
    }

    var downloaded = zipsFolder is null;
    zipsFolder ??= Download(zipShas.Keys);
    foreach (var (zip, sha) in zipShas)
    {
        var path = Path.Combine(zipsFolder, zip);
        if (!File.Exists(path)) { problems.Add($"{zip}: not found in {zipsFolder}"); continue; }
        if (Sha(File.ReadAllBytes(path)) != sha) { problems.Add($"{zip}: SHA-256 differs from {ManifestName}"); continue; }
        var entries = ReadEntries(path);
        foreach (var f in files.Where(f => f.Zip == zip))
        {
            if (!entries.TryGetValue(f.Entry, out var entrySha))
                problems.Add($"{f.RepoPath}: {zip} has no {f.Entry}");
            else if (entrySha != f.Sha)
                problems.Add($"{f.RepoPath}: differs from {zip}!{f.Entry}");
        }
    }
    if (downloaded)
        Directory.Delete(zipsFolder, recursive: true);

    foreach (var problem in problems)
        Console.Error.WriteLine(problem);
    Console.WriteLine(problems.Count == 0
        ? $"{files.Count} official files match {ManifestName} and {zipShas.Count} FpML downloads"
        : $"{problems.Count} problems");
    return problems.Count == 0 ? 0 : 1;
}

string Download(IEnumerable<string> zips)
{
    var folder = Directory.CreateTempSubdirectory("fpml-official-").FullName;
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd("FpmlToolKit.Net-official-files");
    foreach (var zip in zips)
    {
        var target = Path.Combine(folder, zip);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        Console.WriteLine($"downloading {BaseUrl}{zip}");
        File.WriteAllBytes(target, http.GetByteArrayAsync(BaseUrl + zip).GetAwaiter().GetResult());
    }
    return folder;
}

(Dictionary<string, string> Zips, List<(string Sha, string RepoPath, string Zip, string Entry)> Files) ReadManifest()
{
    var zipShas = new Dictionary<string, string>(StringComparer.Ordinal);
    var files = new List<(string, string, string, string)>();
    foreach (var line in File.ReadAllLines(manifestPath).Where(l => l.Length > 0 && !l.StartsWith('#')))
    {
        var parts = line.Split(' ');
        switch (parts)
        {
            case ["zip", var sha, var zip]: zipShas[zip] = sha; break;
            case ["file", var sha, var repoPath, var zip, var entry]: files.Add((sha, repoPath, zip, entry)); break;
            default: throw new InvalidOperationException($"{ManifestName}: unreadable line '{line}'");
        }
    }
    return (zipShas, files);
}

IEnumerable<string> Projects() =>
    Directory.GetDirectories(repoRoot, "fpml-*")
        .Where(d => Directory.Exists(Path.Combine(d, "xsd")))
        .Select(d => Path.GetFileName(d)!)
        .OrderBy(n => n, StringComparer.Ordinal);

IEnumerable<string> OfficialFilesInRepo()
{
    foreach (var project in Projects())
        foreach (var file in Directory.GetFiles(Path.Combine(repoRoot, project, "xsd"), "*.xsd"))
            yield return $"{project}/xsd/{Path.GetFileName(file)}";
    var examples = Path.Combine(repoRoot, "tests", "FpmlToolKit.Tests", "Examples");
    foreach (var file in Directory.GetFiles(examples, "*", SearchOption.AllDirectories))
        yield return Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
}

static Dictionary<string, string> ReadEntries(string zipPath)
{
    using var archive = ZipFile.OpenRead(zipPath);
    var entries = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var entry in archive.Entries.Where(e => !e.FullName.EndsWith('/')))
    {
        using var stream = entry.Open();
        entries[entry.FullName] = Convert.ToHexStringLower(SHA256.HashData(stream));
    }
    return entries;
}

static string Line(string sha, string repoPath, string zip, string entry) =>
    repoPath.Contains(' ') || entry.Contains(' ')
        ? throw new InvalidOperationException($"space in '{repoPath}' or '{entry}'")
        : $"file {sha} {repoPath} {zip} {entry}";

static string DirOf(string entry) => entry.LastIndexOf('/') is var i and >= 0 ? entry[..(i + 1)] : "";

static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

// fpml-5-13-8-rec-2/... -> "0005-0013-0008": orders downloads by FpML version and build.
static string BuildKey(string zipPath) =>
    string.Join("-", Regex.Matches(zipPath.Split('/').First(s => s.Contains("fpml-")), @"\d+").Select(m => m.Value.PadLeft(4, '0')));

static string FindRepoRoot()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "FpmlToolKit.slnx")))
            return dir.FullName;
    }
    throw new InvalidOperationException("run this from inside the FpmlToolKit.Net repository");
}
