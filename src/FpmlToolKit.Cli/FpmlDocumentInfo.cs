using System.Xml;

namespace FpmlToolKit.Cli;

/// <summary>An FpML document's version and view, read from its root element.</summary>
internal sealed record FpmlDocumentInfo(string RootElement, string Namespace, string Version, string? View)
{
    private const string Fpml5Prefix = "http://www.fpml.org/FpML-5/";

    /// <summary>The schema project for this document, for example <c>fpml-5-13-confirmation</c> or <c>fpml-4-4</c>.</summary>
    public string Project => View == null ? $"fpml-{Version}" : $"fpml-{Version}-{View}";

    public string Display => View == null ? $"FpML {Version.Replace('-', '.')}" : $"FpML {Version.Replace('-', '.')} {View}";

    /// <summary>Reads only as far as the root element. Returns null with a reason when it is not an FpML document.</summary>
    public static FpmlDocumentInfo? Detect(string path, out string? problem)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
        reader.MoveToContent();
        // FpML 5 has fpmlVersion on every root element; FpML 4 has version on its single FpML root element.
        var version = reader.NamespaceURI.StartsWith(Fpml5Prefix, StringComparison.Ordinal) ? reader.GetAttribute("fpmlVersion") : reader.GetAttribute("version");
        return Detect(reader.LocalName, reader.NamespaceURI, version, out problem);
    }

    internal static FpmlDocumentInfo? Detect(string root, string ns, string? fpmlVersion, out string? problem)
    {
        problem = null;
        if (!ns.StartsWith("http://www.fpml.org/", StringComparison.Ordinal))
        {
            problem = $"not an FpML document (root element {root} is in namespace '{ns}')";
            return null;
        }
        var fpml5 = ns.StartsWith(Fpml5Prefix, StringComparison.Ordinal);
        if (string.IsNullOrEmpty(fpmlVersion))
        {
            problem = $"the root element has no {(fpml5 ? "fpmlVersion" : "version")} attribute";
            return null;
        }
        if (fpml5)
        {
            if (!fpmlVersion.StartsWith("5-", StringComparison.Ordinal))
            {
                problem = $"fpmlVersion '{fpmlVersion}' does not match the FpML 5 namespace";
                return null;
            }
            return new FpmlDocumentInfo(root, ns, fpmlVersion, ns[Fpml5Prefix.Length..]);
        }
        if (!fpmlVersion.StartsWith("4-", StringComparison.Ordinal))
        {
            problem = $"unsupported fpmlVersion '{fpmlVersion}' in namespace '{ns}'";
            return null;
        }
        return new FpmlDocumentInfo(root, ns, fpmlVersion, null);
    }
}
