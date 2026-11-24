using System.Xml;
using System.Xml.Schema;

namespace FpmlToolKit.Cli;

/// <summary>The FpML schemas shipped with the tool (schemas/&lt;project&gt;/xsd/*.xsd), compiled on first use.</summary>
internal sealed class SchemaLibrary(string root)
{
    private readonly Dictionary<string, XmlSchemaSet> compiled = new(StringComparer.Ordinal);

    public static SchemaLibrary Default { get; } = new(Path.Combine(AppContext.BaseDirectory, "schemas"));

    public IEnumerable<string> Projects =>
        Directory.Exists(root) ? Directory.GetDirectories(root).Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal) : [];

    public bool Has(string project) => Directory.Exists(Path.Combine(root, project, "xsd"));

    public XmlSchemaSet For(string project)
    {
        if (compiled.TryGetValue(project, out var schemas))
            return schemas;

        var main = Directory.GetFiles(Path.Combine(root, project, "xsd"), "fpml-main-*.xsd").Single();
        var resolver = new LocalFileResolver();
        schemas = new XmlSchemaSet { XmlResolver = resolver };
        using (var reader = XmlReader.Create(main, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = resolver }))
            schemas.Add(null, reader);
        schemas.Compile();
        return compiled[project] = schemas;
    }

    // Schemas include and import each other by relative path; nothing is ever fetched from the network.
    private sealed class LocalFileResolver : XmlUrlResolver
    {
        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
            absoluteUri.IsFile
                ? base.GetEntity(absoluteUri, role, ofObjectToReturn)
                : throw new XmlException($"Refusing to load {absoluteUri}: only the schemas shipped with the tool are used.");
    }
}
