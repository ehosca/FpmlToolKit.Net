using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace FpmlToolKit.Validation
{
    /// <summary>
    /// One FpML business validation rule, as published by the FpML Validation Working Group.
    /// </summary>
    public sealed class FpmlRule
    {
        private readonly Func<XElement, RuleContext, IEnumerable<XElement?>> check;

        internal FpmlRule(string id, string description, IReadOnlyList<string> contextTypes, Func<XElement, RuleContext, IEnumerable<XElement?>> check)
        {
            Id = id;
            Description = description;
            ContextTypes = contextTypes;
            this.check = check;
        }

        /// <summary>FpML rule identifier, for example <c>shared-1</c> or <c>ref-29</c>.</summary>
        public string Id { get; }

        /// <summary>What the rule requires, paraphrasing FpML's English description.</summary>
        public string Description { get; }

        /// <summary>The FpML complex types the rule applies to; elements of derived types are included.</summary>
        public IReadOnlyList<string> ContextTypes { get; }

        /// <summary>The rule's page in the FpML 5.13 confirmation view specification.</summary>
        public Uri SpecificationUri
        {
            get
            {
                var page = Id.Substring(0, Id.LastIndexOf('-'));
                return new Uri($"https://www.fpml.org/spec/fpml-5-13-8-rec-2/html/confirmation/validation-rules/rules-english-{page}.html#{Id}");
            }
        }

        /// <summary>Elements violating the rule; null means the context element itself.</summary>
        internal IEnumerable<XElement?> Check(XElement element, RuleContext context) => check(element, context);

        /// <inheritdoc />
        public override string ToString() => $"{Id}: {Description}";

        // Builders used by the rule definitions.

        internal static FpmlRule Assert(string id, string description, string[] contextTypes, Func<XElement, RuleContext, bool> holds) =>
            new FpmlRule(id, description, contextTypes, (e, c) => holds(e, c) ? Array.Empty<XElement?>() : new XElement?[] { null });

        internal static FpmlRule Each(string id, string description, string[] contextTypes, Func<XElement, RuleContext, IEnumerable<XElement>> offenders) =>
            new FpmlRule(id, description, contextTypes, (e, c) => offenders(e, c).Cast<XElement?>());
    }

    /// <summary>Type information and lookups for one document, shared by all rules.</summary>
    internal sealed class RuleContext
    {
        private readonly Dictionary<string, XElement> byId = new Dictionary<string, XElement>(StringComparer.Ordinal);

        public RuleContext(XDocument document)
        {
            Document = document;
            foreach (var element in document.Descendants())
            {
                var id = (string?)element.Attribute("id");
                if (id != null && !byId.ContainsKey(id))
                    byId.Add(id, element);
            }
        }

        public XDocument Document { get; }

        public XElement? ById(string? id) => id != null && byId.TryGetValue(id, out var e) ? e : null;

        /// <summary>True when the element's schema type is, or derives from, one of the named FpML types.</summary>
        public bool IsInstanceOfAny(XElement element, IReadOnlyList<string> typeNames)
        {
            for (var type = element.GetSchemaInfo()?.SchemaType; type != null; type = type.BaseXmlSchemaType)
            {
                var name = type.QualifiedName;
                if (!name.IsEmpty && IsFpmlNamespace(name.Namespace))
                {
                    for (var i = 0; i < typeNames.Count; i++)
                        if (typeNames[i] == name.Name)
                            return true;
                }
            }
            return false;
        }

        public bool IsInstanceOf(XElement? element, string typeName) =>
            element != null && IsInstanceOfAny(element, new[] { typeName });

        /// <summary>
        /// Descendants with the given name that belong to <paramref name="scope"/>: content the schema skipped (such as an
        /// originalMessage wrapper) and nested documents are left out.
        /// </summary>
        public IEnumerable<XElement> DescendantsInScope(XElement scope, string localName)
        {
            var name = scope.Name.Namespace + localName;
            foreach (var child in scope.Elements())
            {
                if (child.GetSchemaInfo()?.SchemaType == null || IsInstanceOf(child, "Document"))
                    continue;
                if (child.Name == name)
                    yield return child;
                foreach (var d in DescendantsInScope(child, localName))
                    yield return d;
            }
        }

        private static bool IsFpmlNamespace(string ns) => ns.StartsWith("http://www.fpml.org/", StringComparison.Ordinal);
    }

    /// <summary>XPath-like helpers over FpML elements (children are in the parent's namespace).</summary>
    internal static class X
    {
        public static XElement? Child(this XElement e, string localName) => e.Element(e.Name.Namespace + localName);

        public static IEnumerable<XElement> Children(this XElement e, string localName) => e.Elements(e.Name.Namespace + localName);

        public static IEnumerable<XElement> Children(this XElement e, params string[] localNames) =>
            e.Elements().Where(c => c.Name.Namespace == e.Name.Namespace && localNames.Contains(c.Name.LocalName));

        public static bool Has(this XElement e, params string[] localNames) => e.Children(localNames).Any();

        /// <summary>The element at a child path such as "earliestExerciseTime/hourMinuteTime".</summary>
        public static XElement? At(this XElement e, string path)
        {
            XElement? current = e;
            foreach (var step in path.Split('/'))
            {
                current = current?.Child(step);
                if (current == null)
                    return null;
            }
            return current;
        }

        public static string? Value(this XElement? e) => e?.Value.Trim();

        public static string? ValueAt(this XElement e, string path) => e.At(path).Value();

        public static string? Href(this XElement? e) => (string?)e?.Attribute("href");

        public static IEnumerable<XElement> DescendantsNamed(this XElement e, string localName) => e.Descendants(e.Name.Namespace + localName);

        public static DateTime? Date(this XElement? e)
        {
            var text = e.Value();
            if (string.IsNullOrEmpty(text))
                return null;
            // xs:date may carry a time zone (2001-06-01Z, 2001-06-01+02:00); dates are compared on their calendar day.
            var day = text!.Length >= 10 ? text.Substring(0, 10) : text;
            return DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;
        }

        public static TimeSpan? Time(this XElement? e)
        {
            var text = e.Value();
            if (string.IsNullOrEmpty(text))
                return null;
            try
            {
                return XmlConvert.ToDateTime(text!, XmlDateTimeSerializationMode.Unspecified).TimeOfDay;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static decimal? Decimal(this XElement? e) =>
            decimal.TryParse(e.Value(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null;

        /// <summary>Elements of <paramref name="items"/> whose key equals the key of an earlier item.</summary>
        public static IEnumerable<XElement> Duplicates<TKey>(this IEnumerable<XElement> items, Func<XElement, TKey> key)
        {
            var seen = new HashSet<TKey>();
            foreach (var item in items)
                if (!seen.Add(key(item)))
                    yield return item;
        }

        /// <summary>An identifier together with its scheme attribute, which is part of its identity in FpML.</summary>
        public static string SchemeKey(this XElement e, string schemeAttribute) =>
            ((string?)e.Attribute(schemeAttribute) ?? "") + "\u0001" + e.Value.Trim();
    }
}
