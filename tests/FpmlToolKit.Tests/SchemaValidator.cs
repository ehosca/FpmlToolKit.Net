using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace FpmlToolKit.Tests;

internal static class SchemaValidator
{
    public static XmlSchemaSet Load(string mainSchemaPath)
    {
        var schemas = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        using (var reader = XmlReader.Create(mainSchemaPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse }))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();
        return schemas;
    }

    public static IReadOnlyList<string> Validate(XDocument document, XmlSchemaSet schemas)
    {
        var errors = new List<string>();
        document.Validate(schemas, (_, e) => errors.Add($"{e.Severity}: {e.Message}"));
        return errors;
    }
}
