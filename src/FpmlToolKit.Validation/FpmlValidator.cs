using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace FpmlToolKit.Validation
{
    /// <summary>
    /// Validates FpML documents against their XSD and the FpML business validation rules.
    /// </summary>
    /// <remarks>
    /// Rules are matched on schema types (FpML defines each rule's context as a complex type), so the document is first
    /// validated against the schema set to learn every element's type. Pass the schema set of the document's
    /// version and view, for example <c>fpml_5_13_confirmation.FpmlSchema.CreateSchemaSet()</c>. Compiling a schema set
    /// is expensive: create one per version, reuse it, and don't share one between threads.
    /// The document passed in is never modified.
    /// </remarks>
    public static class FpmlValidator
    {
        /// <summary>Validates <paramref name="document"/> against <paramref name="schemas"/> and every rule in <see cref="FpmlRules.All"/>.</summary>
        public static ValidationReport Validate(XDocument document, XmlSchemaSet schemas) => Validate(document, schemas, FpmlRules.All);

        /// <summary>Validates <paramref name="document"/> against <paramref name="schemas"/> and the given rules.</summary>
        public static ValidationReport Validate(XDocument document, XmlSchemaSet schemas, IEnumerable<FpmlRule> rules)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (schemas == null) throw new ArgumentNullException(nameof(schemas));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (document.Root == null) throw new ArgumentException("The document has no root element.", nameof(document));

            // Schema validation adds type information and default attributes to the tree it validates, so it runs on a
            // copy. The copy has the same elements in the same order; map them back to report the caller's elements.
            var copy = new XDocument(document);
            var originals = new Dictionary<XElement, XElement>();
            using (var o = document.Root.DescendantsAndSelf().GetEnumerator())
            using (var c = copy.Root!.DescendantsAndSelf().GetEnumerator())
            {
                while (o.MoveNext() && c.MoveNext())
                    originals[c.Current] = o.Current;
            }
            XElement Original(XElement e) => originals.TryGetValue(e, out var original) ? original : e;

            var schemaErrors = new List<SchemaError>();
            copy.Validate(schemas, (sender, e) =>
            {
                var element = sender switch
                {
                    XElement x => Original(x),
                    XAttribute a when a.Parent != null => Original(a.Parent),
                    _ => null,
                };
                schemaErrors.Add(new SchemaError(e.Severity, e.Message, element));
            }, addSchemaInfo: true);

            var violations = new List<RuleViolation>();
            var ruleList = rules.ToList();
            var rulesApplied = RulesApplyTo(copy.Root);
            if (rulesApplied)
            {
                var context = new RuleContext(copy);
                foreach (var element in copy.Root.DescendantsAndSelf())
                {
                    foreach (var rule in ruleList)
                    {
                        if (!context.IsInstanceOfAny(element, rule.ContextTypes))
                            continue;
                        foreach (var failure in rule.Check(element, context))
                            violations.Add(new RuleViolation(rule, Original(failure ?? element)));
                    }
                }
            }

            return new ValidationReport(schemaErrors, violations, rulesApplied);
        }

        private static readonly string[] RuleViews = { "confirmation", "reporting", "recordkeeping", "transparency" };

        /// <summary>
        /// FpML publishes the shared and reference rules for 5.x in the confirmation view, and applies them to reporting,
        /// recordkeeping and transparency too (since 5.5). Pretrade, legal and FpML 4.x are not covered.
        /// </summary>
        internal static bool RulesApplyTo(XElement root)
        {
            const string prefix = "http://www.fpml.org/FpML-5/";
            var ns = root.Name.NamespaceName;
            return ns.StartsWith(prefix, StringComparison.Ordinal) && RuleViews.Contains(ns.Substring(prefix.Length));
        }
    }

    /// <summary>The result of <see cref="FpmlValidator.Validate(XDocument, XmlSchemaSet)"/>.</summary>
    public sealed class ValidationReport
    {
        internal ValidationReport(IReadOnlyList<SchemaError> schemaErrors, IReadOnlyList<RuleViolation> ruleViolations, bool rulesApplied)
        {
            SchemaErrors = schemaErrors;
            RuleViolations = ruleViolations;
            RulesApplied = rulesApplied;
        }

        /// <summary>XSD validation errors and warnings.</summary>
        public IReadOnlyList<SchemaError> SchemaErrors { get; }

        /// <summary>Business rule violations.</summary>
        public IReadOnlyList<RuleViolation> RuleViolations { get; }

        /// <summary>
        /// False when no business rules were run: the rules cover the FpML 5.x confirmation, reporting, recordkeeping and
        /// transparency views only.
        /// </summary>
        public bool RulesApplied { get; }

        /// <summary>True when there are no schema errors (warnings don't count) and no rule violations.</summary>
        public bool IsValid => RuleViolations.Count == 0 && SchemaErrors.All(e => e.Severity != XmlSeverityType.Error);
    }

    /// <summary>An XSD validation error or warning.</summary>
    public sealed class SchemaError
    {
        internal SchemaError(XmlSeverityType severity, string message, XElement? element)
        {
            Severity = severity;
            Message = message;
            Element = element;
        }

        /// <summary>Error or warning.</summary>
        public XmlSeverityType Severity { get; }

        /// <summary>The validator's message.</summary>
        public string Message { get; }

        /// <summary>The element the error is about, in the validated document, if known.</summary>
        public XElement? Element { get; }

        /// <summary>Location path of <see cref="Element"/>, for example <c>/dataDocument/trade[1]/swap</c>.</summary>
        public string? Path => Element == null ? null : ElementPath.Of(Element);

        /// <summary>Line number of <see cref="Element"/>, when the document was loaded with <see cref="LoadOptions.SetLineInfo"/>.</summary>
        public int? LineNumber => ElementPath.LineOf(Element);
    }

    /// <summary>A business rule violation.</summary>
    public sealed class RuleViolation
    {
        internal RuleViolation(FpmlRule rule, XElement element)
        {
            Rule = rule;
            Element = element;
        }

        /// <summary>The rule that was violated.</summary>
        public FpmlRule Rule { get; }

        /// <summary>The rule's FpML identifier, for example <c>shared-1</c>.</summary>
        public string RuleId => Rule.Id;

        /// <summary>The element that violates the rule, in the validated document.</summary>
        public XElement Element { get; }

        /// <summary>Location path of <see cref="Element"/>, for example <c>/dataDocument/trade[1]/swap</c>.</summary>
        public string Path => ElementPath.Of(Element);

        /// <summary>Line number of <see cref="Element"/>, when the document was loaded with <see cref="LoadOptions.SetLineInfo"/>.</summary>
        public int? LineNumber => ElementPath.LineOf(Element);

        /// <inheritdoc />
        public override string ToString() => $"{RuleId} at {Path}: {Rule.Description}";
    }

    internal static class ElementPath
    {
        public static string Of(XElement element)
        {
            var steps = new Stack<string>();
            for (var e = element; e != null; e = e.Parent)
            {
                var name = e.Name;
                var siblings = e.Parent?.Elements(name).ToList();
                steps.Push(siblings != null && siblings.Count > 1 ? $"{name.LocalName}[{siblings.IndexOf(e) + 1}]" : name.LocalName);
            }
            return "/" + string.Join("/", steps);
        }

        public static int? LineOf(XElement? element) =>
            element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : (int?)null;
    }
}
