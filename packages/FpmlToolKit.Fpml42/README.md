# FpmlToolKit.Fpml42

Strongly typed C# for FpML 4.2: read, build and validate FpML documents in a few lines, with IntelliSense
for every element. Part of [FpmlToolKit.Net](https://github.com/ehosca/FpmlToolKit.Net), which covers every FpML
version from 4.0 to 5.14. Targets .NET Standard 2.0 and .NET 10.

## Get started

```csharp
using fpml_4_2;

var doc = XRoot.Load("document.xml");           // any FpML 4.2 document
Console.WriteLine(doc.Root.GetType().Name);     // the typed root element

bool valid = FpmlSchema.IsValid(doc.Root);      // the official schemas are built in: no files, no setup
```

Element and type names are FpML's own, so the FpML specification is your API reference. Root elements have
`Load` and `Parse`, every type can be built with object initializers and saved with `Save` or `ToString()`, and
`FpmlSchema.Validate` tells you exactly what's wrong when a document isn't valid. The
[FpmlToolKit.Net README](https://github.com/ehosca/FpmlToolKit.Net#readme) has a quick start and recipes for
reading, querying, building, editing and validating.

## What's inside

| View | Assembly | Namespace |
|---|---|---|
| (all) | `fpml-4-2.dll` | `fpml_4_2` |

Each assembly also has its own copy of the W3C XML Signature types, in `<namespace>.xmldsig`. The schemas are
FpML's official files, unchanged, from the latest Recommendation build of FpML 4.2.

## License

FpmlToolKit.Net is MIT licensed. The FpML schemas are licensed by ISDA under the
[FpML Public License](https://www.fpml.org/the_standard/fpml-public-license/); see `LICENSE.txt` and
`THIRD-PARTY-NOTICES.md` in the package.
