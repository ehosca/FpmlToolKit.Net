using System.Reflection;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using FpmlToolKit.Validation;

namespace FpmlToolKit.Cli;

internal static class FpmlCommand
{
    public const int Valid = 0, Invalid = 1, Error = 2;

    private const string Usage = """
        usage:
          fpml validate <file|folder>... [--format text|json]
              Validates each FpML document against the schema of its version and view, then against FpML's business
              rules (FpML 5.x confirmation, reporting, recordkeeping and transparency). Folders are searched for *.xml.
              Exit code: 0 all valid, 1 some invalid, 2 a file could not be read or is not FpML.
          fpml detect <file|folder>...
              Prints each document's FpML version and view.
          fpml rules [--format text|json]
              Lists the business rules that validate checks.
          fpml --version
        """;

    public static int Run(string[] args, TextWriter output, TextWriter errors)
    {
        try
        {
            return args switch
            {
                ["validate", .. var rest] => Validate(rest, output, errors),
                ["detect", .. var rest] => Detect(rest, output, errors),
                ["rules", .. var rest] => Rules(rest, output, errors),
                ["--version" or "-v"] => PrintVersion(output),
                ["--help" or "-h" or "help"] => PrintUsage(output, Valid),
                _ => PrintUsage(errors, Error),
            };
        }
        catch (UsageException e)
        {
            errors.WriteLine($"fpml: {e.Message}");
            return PrintUsage(errors, Error);
        }
    }

    private static int Validate(string[] args, TextWriter output, TextWriter errors)
    {
        var (paths, json) = ParseOptions(args);
        var files = ExpandFiles(paths, errors, out var missing);
        var library = SchemaLibrary.Default;
        var results = new List<FileResult>();
        foreach (var file in files)
            results.Add(ValidateFile(file, library));

        if (json)
            WriteJson(results, output);
        else
            WriteText(results, output);

        if (missing || results.Any(r => r.Problem != null))
            return Error;
        return results.All(r => r.Report!.IsValid) ? Valid : Invalid;
    }

    internal static FileResult ValidateFile(string file, SchemaLibrary library)
    {
        try
        {
            var info = FpmlDocumentInfo.Detect(file, out var problem);
            if (info == null)
                return new FileResult(file, null, null, problem);
            if (!library.Has(info.Project))
                return new FileResult(file, info, null, $"{info.Display} is not supported (no schema {info.Project})");
            var document = XDocument.Load(file, LoadOptions.SetLineInfo);
            return new FileResult(file, info, FpmlValidator.Validate(document, library.For(info.Project)), null);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return new FileResult(file, null, null, e.Message);
        }
    }

    private static void WriteText(List<FileResult> results, TextWriter output)
    {
        foreach (var r in results)
        {
            if (r.Problem != null)
            {
                output.WriteLine($"{r.File}: error: {r.Problem}");
                continue;
            }
            var report = r.Report!;
            foreach (var e in report.SchemaErrors)
                output.WriteLine($"{Location(r.File, e.LineNumber)}: {(e.Severity == XmlSeverityType.Error ? "error" : "warning")} xsd: {e.Message}{At(e.Path)}");
            foreach (var v in report.RuleViolations)
                output.WriteLine($"{Location(r.File, v.LineNumber)}: error {v.RuleId}: {v.Rule.Description}{At(v.Path)}");

            var errorCount = report.SchemaErrors.Count(e => e.Severity == XmlSeverityType.Error) + report.RuleViolations.Count;
            var rules = report.RulesApplied ? "" : " (no business rules for this view)";
            output.WriteLine(errorCount == 0
                ? $"{r.File}: {r.Info!.Display}: valid{rules}"
                : $"{r.File}: {r.Info!.Display}: {errorCount} error{(errorCount == 1 ? "" : "s")}{rules}");
        }
        if (results.Count > 1)
        {
            var valid = results.Count(r => r.Report?.IsValid == true);
            output.WriteLine($"{results.Count} files: {valid} valid, {results.Count - valid} with errors");
        }
    }

    private static string Location(string file, int? line) => line == null ? file : $"{file}({line})";

    private static string At(string? path) => path == null ? "" : $" [{path}]";

    private static void WriteJson(List<FileResult> results, TextWriter output)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartArray();
            foreach (var r in results)
            {
                w.WriteStartObject();
                w.WriteString("file", r.File);
                if (r.Info != null)
                {
                    w.WriteString("fpmlVersion", r.Info.Version.Replace('-', '.'));
                    if (r.Info.View != null) w.WriteString("view", r.Info.View);
                }
                if (r.Problem != null)
                {
                    w.WriteString("error", r.Problem);
                }
                else
                {
                    var report = r.Report!;
                    w.WriteBoolean("valid", report.IsValid);
                    w.WriteBoolean("rulesApplied", report.RulesApplied);
                    w.WriteStartArray("schemaErrors");
                    foreach (var e in report.SchemaErrors)
                    {
                        w.WriteStartObject();
                        w.WriteString("severity", e.Severity == XmlSeverityType.Error ? "error" : "warning");
                        w.WriteString("message", e.Message);
                        WriteLocation(w, e.LineNumber, e.Path);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    w.WriteStartArray("ruleViolations");
                    foreach (var v in report.RuleViolations)
                    {
                        w.WriteStartObject();
                        w.WriteString("rule", v.RuleId);
                        w.WriteString("description", v.Rule.Description);
                        WriteLocation(w, v.LineNumber, v.Path);
                        w.WriteString("specification", v.Rule.SpecificationUri.ToString());
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        output.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void WriteLocation(Utf8JsonWriter w, int? line, string? path)
    {
        if (line != null) w.WriteNumber("line", line.Value);
        if (path != null) w.WriteString("path", path);
    }

    private static int Detect(string[] args, TextWriter output, TextWriter errors)
    {
        var (paths, json) = ParseOptions(args);
        if (json)
            throw new UsageException("detect has no --format option");
        var files = ExpandFiles(paths, errors, out var failed);
        foreach (var file in files)
        {
            try
            {
                var info = FpmlDocumentInfo.Detect(file, out var problem);
                if (info == null)
                {
                    output.WriteLine($"{file}: error: {problem}");
                    failed = true;
                }
                else
                {
                    var supported = SchemaLibrary.Default.Has(info.Project) ? "" : " (not supported)";
                    output.WriteLine($"{file}: {info.Display} <{info.RootElement}>{supported}");
                }
            }
            catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
            {
                output.WriteLine($"{file}: error: {e.Message}");
                failed = true;
            }
        }
        return failed ? Error : Valid;
    }

    private static int Rules(string[] args, TextWriter output, TextWriter errors)
    {
        var (paths, json) = ParseOptions(args);
        if (paths.Count > 0)
            throw new UsageException("rules takes no files");
        if (!json)
        {
            foreach (var rule in FpmlRules.All)
                output.WriteLine($"{rule.Id,-10} {rule.Description}");
            return Valid;
        }
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartArray();
            foreach (var rule in FpmlRules.All)
            {
                w.WriteStartObject();
                w.WriteString("rule", rule.Id);
                w.WriteString("description", rule.Description);
                w.WriteStartArray("contextTypes");
                foreach (var t in rule.ContextTypes) w.WriteStringValue(t);
                w.WriteEndArray();
                w.WriteString("specification", rule.SpecificationUri.ToString());
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        output.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
        return Valid;
    }

    private static (List<string> Paths, bool Json) ParseOptions(string[] args)
    {
        var paths = new List<string>();
        var json = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format" when i + 1 < args.Length:
                    json = args[++i] switch
                    {
                        "json" => true,
                        "text" => false,
                        var f => throw new UsageException($"unknown format '{f}'"),
                    };
                    break;
                case "--format":
                    throw new UsageException("--format needs a value: text or json");
                case var a when a.StartsWith('-') && a.Length > 1:
                    throw new UsageException($"unknown option '{a}'");
                default:
                    paths.Add(args[i]);
                    break;
            }
        }
        return (paths, json);
    }

    private static List<string> ExpandFiles(List<string> paths, TextWriter errors, out bool missing)
    {
        if (paths.Count == 0)
            throw new UsageException("no files given");
        missing = false;
        var files = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
                files.AddRange(Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories).Order(StringComparer.Ordinal));
            else if (File.Exists(path))
                files.Add(path);
            else
            {
                errors.WriteLine($"{path}: error: no such file or folder");
                missing = true;
            }
        }
        return files;
    }

    private static int PrintVersion(TextWriter output)
    {
        var version = typeof(FpmlCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        output.WriteLine($"fpml {version.Split('+')[0]}");
        return Valid;
    }

    private static int PrintUsage(TextWriter writer, int exitCode)
    {
        writer.WriteLine(Usage);
        return exitCode;
    }

    internal sealed record FileResult(string File, FpmlDocumentInfo? Info, ValidationReport? Report, string? Problem);

    private sealed class UsageException(string message) : Exception(message);
}
