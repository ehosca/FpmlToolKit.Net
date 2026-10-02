// Creates an fpml-<version>-<view> project from an FpML schema download and adds it to FpmlToolKit.slnx.
//
//   dotnet run scripts/add-schema-version.cs -- <folder> [<folder> ...]
//
// Each folder is searched recursively for fpml-main-*.xsd; every match is one view (confirmation, reporting, ...).
// The XSDs next to it are copied into the new project's xsd/ folder. The version comes from the main schema's
// file name (fpml-main-5-13.xsd -> 5-13) and the view from its target namespace
// (http://www.fpml.org/FpML-5/confirmation -> confirmation). Existing projects are left untouched.

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var repoRoot = FindRepoRoot();
var sources = args.Where(a => !a.StartsWith("--")).ToList();
if (sources.Count == 0)
{
    Console.Error.WriteLine("usage: dotnet run scripts/add-schema-version.cs -- <folder> [<folder> ...]");
    return 1;
}

foreach (var missing in sources.Where(s => !Directory.Exists(s)))
{
    Console.Error.WriteLine($"folder not found: {missing}");
    return 1;
}

var mainSchemas = sources
    .SelectMany(s => Directory.GetFiles(s, "fpml-main-*.xsd", SearchOption.AllDirectories))
    .Order()
    .ToList();
if (mainSchemas.Count == 0)
{
    Console.Error.WriteLine("no fpml-main-*.xsd found");
    return 1;
}

var failed = 0;
foreach (var mainSchema in mainSchemas)
{
    try
    {
        Add(mainSchema);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{mainSchema}: {ex.Message}");
        failed++;
    }
}
return failed == 0 ? 0 : 1;

void Add(string mainSchema)
{
    var version = Regex.Match(Path.GetFileName(mainSchema), @"^fpml-main-(\d+-\d+)\.xsd$") is { Success: true } m
        ? m.Groups[1].Value
        : throw new InvalidOperationException("main schema name is not fpml-main-<major>-<minor>.xsd");

    var targetNamespace = (string)XDocument.Load(mainSchema).Root!.Attribute("targetNamespace")
        ?? throw new InvalidOperationException("main schema has no targetNamespace");
    var view = Regex.Match(targetNamespace, @"^http://www\.fpml\.org/FpML-5/([a-z]+)$") is { Success: true } v
        ? v.Groups[1].Value
        : throw new InvalidOperationException($"unexpected target namespace '{targetNamespace}' (only FpML 5.x views are supported)");

    var name = $"fpml-{version}-{view}";
    var projectDir = Path.Combine(repoRoot, name);
    if (Directory.Exists(projectDir))
    {
        Console.WriteLine($"{name}: already exists, skipped");
        return;
    }

    var xsdDir = Directory.CreateDirectory(Path.Combine(projectDir, "xsd")).FullName;
    var schemaFiles = Directory.GetFiles(Path.GetDirectoryName(mainSchema)!, "*.xsd");
    foreach (var file in schemaFiles)
        File.Copy(file, Path.Combine(xsdDir, Path.GetFileName(file)));

    var rootNamespace = name.Replace('-', '_');
    File.WriteAllText(Path.Combine(xsdDir, "namespaceconfig.xml"), $"""
        <?xml version="1.0" encoding="utf-8" ?>
        <Configuration xmlns="http://www.microsoft.com/xml/schema/linq">
          <Namespaces>
            <Namespace Schema="{targetNamespace}" Clr="{rootNamespace}"/>
            <Namespace Schema="http://www.w3.org/2000/09/xmldsig#" Clr="org.w3.xmldsig"/>
          </Namespaces>
        </Configuration>

        """);

    var dotted = version.Replace('-', '.');
    var packageId = $"FpmlToolKit.Fpml{version.Replace("-", "")}.{char.ToUpperInvariant(view[0])}{view[1..]}";
    var csprojPath = Path.Combine(projectDir, name + ".csproj");
    File.WriteAllText(csprojPath, $"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
            <RootNamespace>{rootNamespace}</RootNamespace>
            <AssemblyName>{name}</AssemblyName>
            <PackageId>{packageId}</PackageId>
            <Description>Strongly typed LINQ to XSD classes for the FpML {dotted} {view} view.</Description>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="XObjectsCore" Version="3.4.23" />
          </ItemGroup>

          <ItemGroup>
            <LinqToXsdSchema Include="xsd\{Path.GetFileName(mainSchema)}" />
            <LinqToXsdConfiguration Include="xsd\namespaceconfig.xml" />
            <None Include="xsd\**" Pack="true" PackagePath="xsd\" />
          </ItemGroup>

        </Project>

        """);

    Run("dotnet", $"sln FpmlToolKit.slnx add \"{csprojPath}\"");
    Console.WriteLine($"{name}: added ({packageId}, {schemaFiles.Length} schema files)");
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
