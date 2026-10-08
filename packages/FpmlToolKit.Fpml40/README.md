# FpmlToolKit.Fpml40

Strongly typed LINQ to XSD classes for FpML 4.0.

Generated from the FpML 4.0 schemas with [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore),
as part of [FpmlToolKit.Net](https://github.com/ehosca/FpmlToolKit.Net). Targets .NET Standard 2.0 and .NET 10.

| View | Assembly | Namespace |
|---|---|---|
| (all) | `fpml-4-0.dll` | `fpml_4_0` |

Each assembly has its own copy of the W3C XML Signature types, in `<namespace>.xmldsig`.

## Usage

```csharp
using fpml_4_0;

// Load an FpML 4.0 document; Root is the typed root element.
var doc = XRoot.Load("document.xml");
Console.WriteLine(doc.Root.GetType().Name);

// Validate against the schemas embedded in the assembly (no files or network access needed).
foreach (var problem in FpmlSchema.Validate(doc.Root))
    Console.WriteLine($"{problem.Severity}: {problem.Message}");
```

Root element types also have static `Load` and `Parse` methods, and every type can be built in code and
serialised with `ToString()`. Each assembly embeds the schemas it was generated from: `FpmlSchema` validates
documents and typed elements (`Validate`, `IsValid`), returns a compiled `XmlSchemaSet` (`CreateSchemaSet`) and
opens the raw files (`SchemaFileNames`, `OpenSchema`).

## License

FpmlToolKit.Net is MIT licensed. The FpML schemas are licensed by ISDA under the
[FpML Public License](https://www.fpml.org/the_standard/fpml-public-license/); see `LICENSE.txt` and
`THIRD-PARTY-NOTICES.md` in the package.
