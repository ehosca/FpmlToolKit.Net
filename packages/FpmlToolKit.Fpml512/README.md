# FpmlToolKit.Fpml512

Strongly typed LINQ to XSD classes for FpML 5.12, one assembly per view: confirmation, legal, pretrade, recordkeeping, reporting, transparency.

Generated from the FpML 5.12 schemas with [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore),
as part of [FpmlToolKit.Net](https://github.com/ehosca/FpmlToolKit.Net). Targets .NET Standard 2.0 and .NET 10.

| View | Assembly | Namespace |
|---|---|---|
| confirmation | `fpml-5-12-confirmation.dll` | `fpml_5_12_confirmation` |
| legal | `fpml-5-12-legal.dll` | `fpml_5_12_legal` |
| pretrade | `fpml-5-12-pretrade.dll` | `fpml_5_12_pretrade` |
| recordkeeping | `fpml-5-12-recordkeeping.dll` | `fpml_5_12_recordkeeping` |
| reporting | `fpml-5-12-reporting.dll` | `fpml_5_12_reporting` |
| transparency | `fpml-5-12-transparency.dll` | `fpml_5_12_transparency` |

Each assembly has its own copy of the W3C XML Signature types, in `<namespace>.xmldsig`.

## Usage

```csharp
using fpml_5_12_confirmation;

// Load an FpML 5.12 confirmation view document (use the matching namespace for other views); Root is the typed root element.
var doc = XRoot.Load("document.xml");
Console.WriteLine(doc.Root.GetType().Name);
```

Root element types also have static `Load` and `Parse` methods, and every type can be built in code and
serialised with `ToString()`. The package's `xsd/` folder contains the schemas each assembly was generated from.

## License

FpmlToolKit.Net is MIT licensed. The FpML schemas are licensed by ISDA under the
[FpML Public License](https://www.fpml.org/the_standard/fpml-public-license/); see `LICENSE.txt` and
`THIRD-PARTY-NOTICES.md` in the package.
