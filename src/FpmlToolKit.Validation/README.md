# FpmlToolKit.Validation

Validates FpML 5.x documents against the XSD **and** FpML's business validation rules. These are the constraints the
schema can't express, for example `shared-1`: "business centers exist if and only if there's a business day
convention", or `ref-29`: "a partyReference points at a party".

```csharp
using FpmlToolKit.Validation;
using System.Xml.Linq;

var schemas = fpml_5_13_confirmation.FpmlSchema.CreateSchemaSet();   // from FpmlToolKit.Fpml513; create once, reuse
var document = XDocument.Load("trade.xml", LoadOptions.SetLineInfo);

var report = FpmlValidator.Validate(document, schemas);
foreach (var v in report.RuleViolations)
    Console.WriteLine($"line {v.LineNumber}: {v.RuleId} {v.Path}: {v.Rule.Description}");
```

`FpmlRules.All` lists the implemented rules. Each one has its FpML identifier, a description and a link to its
specification page. The rules are the FpML 5.13 shared (`shared-*`) and ID/IDREF (`ref-*`) rules. They run on FpML
5.x documents of any version in the confirmation, reporting, recordkeeping and transparency views. The tests run
every official 5.x example and every 5.13 confirmation validation test case, and they check the exact set of rules
each file raises. Every rule except `ref-10`, which FpML publishes without a test case, is raised by at least one of
them. Where FpML's own files don't match their names, the reviewed exceptions are listed in the repository's
`KnownRuleViolations.cs`.

The command-line tool `FpmlToolKit.Cli` (`fpml validate`) wraps this package and detects the version for you.

The rule descriptions paraphrase the FpML validation rules, published by ISDA under the FpML Public License. See
THIRD-PARTY-NOTICES.md.
