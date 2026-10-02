// Converts a 2012-era FpML schema project (old-style csproj + LinqToXsdBin) to the SDK-style
// layout used by fpml-5-3-confirmation, and adds it to FpmlToolKit.slnx.
//
//   dotnet run scripts/convert-project.cs -- fpml-5-2-reporting [fpml-4-9 ...]
//   dotnet run scripts/convert-project.cs -- --all
//
// Already-converted projects are skipped, so the script is safe to re-run.

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var repoRoot = FindRepoRoot();
var names = args.Contains("--all")
    ? Directory.GetDirectories(repoRoot, "fpml-*").Select(Path.GetFileName).OfType<string>().Order().ToList()
    : args.Where(a => !a.StartsWith("--")).Select(a => a.TrimEnd('/', '\\')).ToList();

if (names.Count == 0)
{
    Console.Error.WriteLine("usage: dotnet run scripts/convert-project.cs -- <project-dir>... | --all");
    return 1;
}

var failed = 0;
foreach (var name in names)
{
    try
    {
        Convert(name);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{name}: {ex.Message}");
        failed++;
    }
}
return failed == 0 ? 0 : 1;

void Convert(string name)
{
    var projectDir = Path.Combine(repoRoot, name);
    var csprojPath = Path.Combine(projectDir, name + ".csproj");
    if (!File.Exists(csprojPath))
        throw new FileNotFoundException($"no project file at {csprojPath}");

    var old = XDocument.Load(csprojPath);
    if (old.Root!.Attribute("Sdk") is not null)
    {
        Console.WriteLine($"{name}: already SDK-style, skipped");
        return;
    }

    XNamespace ms = "http://schemas.microsoft.com/developer/msbuild/2003";
    string Prop(string prop) =>
        old.Descendants(ms + prop).Select(e => e.Value.Trim()).FirstOrDefault(v => v.Length > 0)
        ?? throw new InvalidOperationException($"missing <{prop}> in {csprojPath}");
    List<string> Items(string item) =>
        old.Descendants(ms + item).Select(e => (string)e.Attribute("Include")!).ToList();

    var rootNamespace = Prop("RootNamespace");
    var assemblyName = Prop("AssemblyName");
    var schemas = Items("LinqToXsdSchema");
    var configs = Items("LinqToXsdConfiguration");
    if (schemas.Count == 0 || configs.Count != 1)
        throw new InvalidOperationException($"expected 1+ LinqToXsdSchema and exactly 1 LinqToXsdConfiguration, found {schemas.Count} and {configs.Count}");

    var (packageId, description) = Describe(name);

    var schemaItems = string.Join(Environment.NewLine,
        schemas.Select(s => $"""    <LinqToXsdSchema Include="{s}" />"""));

    File.WriteAllText(csprojPath, $"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
            <RootNamespace>{rootNamespace}</RootNamespace>
            <AssemblyName>{assemblyName}</AssemblyName>
            <PackageId>{packageId}</PackageId>
            <Description>{description}</Description>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="XObjectsCore" Version="3.4.23" />
          </ItemGroup>

          <ItemGroup>
        {schemaItems}
            <LinqToXsdConfiguration Include="{configs[0]}" />
            <None Include="xsd\**" Pack="true" PackagePath="xsd\" />
          </ItemGroup>

        </Project>

        """);

    var assemblyInfo = Path.Combine(projectDir, "Properties", "AssemblyInfo.cs");
    if (File.Exists(assemblyInfo))
    {
        Run("git", $"rm -q \"{assemblyInfo}\"");
        if (Directory.Exists(Path.GetDirectoryName(assemblyInfo)) && !Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(assemblyInfo)!).Any())
            Directory.Delete(Path.GetDirectoryName(assemblyInfo)!);
    }

    Run("dotnet", $"sln FpmlToolKit.slnx add \"{csprojPath}\"");
    Console.WriteLine($"{name}: converted ({packageId})");
}

// fpml-4-9 -> FpmlToolKit.Fpml49; fpml-5-3-confirmation -> FpmlToolKit.Fpml53.Confirmation
static (string PackageId, string Description) Describe(string name)
{
    var m = Regex.Match(name, @"^fpml-(\d+)-(\d+)(?:-([a-z]+))?$");
    if (!m.Success)
        throw new InvalidOperationException($"unrecognised project name '{name}'");

    var version = $"{m.Groups[1].Value}.{m.Groups[2].Value}";
    var view = m.Groups[3].Value;
    if (view.Length == 0)
        return ($"FpmlToolKit.Fpml{m.Groups[1].Value}{m.Groups[2].Value}",
                $"Strongly typed LINQ to XSD classes for FpML {version}.");

    var viewTitle = char.ToUpperInvariant(view[0]) + view[1..];
    return ($"FpmlToolKit.Fpml{m.Groups[1].Value}{m.Groups[2].Value}.{viewTitle}",
            $"Strongly typed LINQ to XSD classes for the FpML {version} {view} view.");
}

void Run(string file, string arguments)
{
    using var process = Process.Start(new ProcessStartInfo(file, arguments)
    {
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"'{file} {arguments}' failed: {process.StandardError.ReadToEnd().Trim()}");
}

static string FindRepoRoot()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "FpmlToolKit.slnx")))
            return dir.FullName;
    }
    throw new InvalidOperationException("run this from inside the FpmlToolKit.Net repository");
}
