# FpmlToolKit.Fpml54

Strongly typed LINQ to XSD classes for FpML 5.4, one assembly per view: confirmation, recordkeeping, reporting, transparency.

Generated from the FpML 5.4 schemas with [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore),
as part of [FpmlToolKit.Net](https://github.com/ehosca/FpmlToolKit.Net). Targets .NET Standard 2.0 and .NET 10.

| View | Assembly | Namespace |
|---|---|---|
| confirmation | `fpml-5-4-confirmation.dll` | `fpml_5_4_confirmation` |
| recordkeeping | `fpml-5-4-recordkeeping.dll` | `fpml_5_4_recordkeeping` |
| reporting | `fpml-5-4-reporting.dll` | `fpml_5_4_reporting` |
| transparency | `fpml-5-4-transparency.dll` | `fpml_5_4_transparency` |

Each assembly has its own copy of the W3C XML Signature types, in `<namespace>.xmldsig`.

## Usage

```csharp
using fpml_5_4_confirmation;

// Load an FpML 5.4 confirmation view document (use the matching namespace for other views); Root is the typed root element.
var doc = XRoot.Load("document.xml");
Console.WriteLine(doc.Root.GetType().Name);
```

Root element types also have static `Load` and `Parse` methods, and every type can be built in code and
serialised with `ToString()`. The package's `xsd/` folder contains the schemas each assembly was generated from.

## License

FpmlToolKit.Net is MIT licensed. The FpML schemas are licensed by ISDA under the
[FpML Public License](https://www.fpml.org/the_standard/fpml-public-license/); see `LICENSE.txt` and
`THIRD-PARTY-NOTICES.md` in the package.
