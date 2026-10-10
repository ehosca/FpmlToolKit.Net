# FpmlToolKit.Net

[![NuGet](https://img.shields.io/nuget/vpre/FpmlToolKit.Fpml513?label=FpmlToolKit.Fpml513)](https://www.nuget.org/packages/FpmlToolKit.Fpml513)
[![CI](https://github.com/ehosca/FpmlToolKit.Net/actions/workflows/ci.yml/badge.svg)](https://github.com/ehosca/FpmlToolKit.Net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Strongly typed C# for every FpML version, 4.0 to 5.14.** Read, build and validate FpML documents in a few lines of
code, with IntelliSense for every element and nothing to generate yourself. If you work with derivatives
confirmations, reports or trade messages in .NET, you can be productive in the next five minutes.

## Quick start

Add the package for your FpML version:

    dotnet add package FpmlToolKit.Fpml513 --prerelease

Then read a trade:

```csharp
using fpml_5_13_confirmation;

var fpml = dataDocument.Load("ird-ex08-fra.xml");
var trade = fpml.trade[0];

if (trade.product is fra deal)
    Console.WriteLine($"{trade.tradeHeader.tradeDate.TypedValue:yyyy-MM-dd}: FRA on {deal.notional.amount:N0} " +
                      $"{deal.notional.currency.TypedValue}, fixed {deal.fixedRate.TypedValue} vs {deal.floatingRateIndex.TypedValue}");
```

    1991-05-14: FRA on 25,000,000 CHF, fixed 0.04 vs CHF-LIBOR-BBA

That's it: real dates, real decimals, and the FpML names you already know. (The file is one of FpML's own examples.)

## Pick your package

There's one package per FpML version, and it contains every view of that version. They target `netstandard2.0`
(so .NET Framework 4.7.2 and later, too) and `net10.0`.

| FpML | Package | Namespaces |
|---|---|---|
| 5.14 | `FpmlToolKit.Fpml514` | `fpml_5_14_confirmation`, `_reporting`, `_recordkeeping`, `_transparency`, `_pretrade`, `_legal` |
| 5.13 | `FpmlToolKit.Fpml513` | `fpml_5_13_confirmation`, `_reporting`, ... (same six views) |
| 5.0 – 5.12 | `FpmlToolKit.Fpml50` ... `FpmlToolKit.Fpml512` | `fpml_5_<minor>_<view>` |
| 4.0 – 4.10 | `FpmlToolKit.Fpml40` ... `FpmlToolKit.Fpml410` | `fpml_4_<minor>` (4.x has no views) |

The 5.x views grew over time: 5.0–5.2 have confirmation and reporting; recordkeeping and transparency arrive in 5.3,
pretrade in 5.5 and legal in 5.7. 5.8 has no transparency view, because FpML's published schema for it doesn't compile.

The FpML specification doubles as your API reference: element `<dataDocument>` is class `dataDocument`, type `Party`
is class `Party`, and child elements and attributes are properties with the same names.

## Recipes

Every example below is also a test in this repository, so you can copy them with confidence.

### Read any document

Don't know what's in the file? `XRoot` loads any FpML document and gives you the right type:

```csharp
var doc = XRoot.Load("incoming.xml");

if (doc.Root is dataDocument data)
    foreach (var party in data.party)
        Console.WriteLine($"{party.id}: {party.partyId[0].TypedValue}");
```

When you do know, every root element has `Load` (from a file) and `Parse` (from a string):

```csharp
var fromFile = dataDocument.Load("trade.xml");
var fromString = dataDocument.Parse(xmlText);
```

### Query with LINQ

Lists are just lists, so LINQ works the way you'd hope. Resolve a party reference:

```csharp
var deal = (fra)fpml.trade[0].product;
var buyer = fpml.party.Single(p => p.id == deal.buyerPartyReference.href);
```

Or total up the CHF notional across a whole folder of FRAs:

```csharp
var chfNotional = Directory.EnumerateFiles("trades", "*.xml")
    .Select(dataDocument.Load)
    .SelectMany(doc => doc.trade)
    .Select(trade => trade.product)
    .OfType<fra>()
    .Where(deal => deal.notional.currency.TypedValue == "CHF")
    .Sum(deal => deal.notional.amount);
```

### Build a document

Object initializers and collection expressions make new documents read like the XML they produce:

```csharp
var document = new dataDocument
{
    fpmlVersion = "5-13",
    party =
    [
        new Party
        {
            id = "party1",
            partyId = [new PartyId { partyIdScheme = new Uri("http://www.fpml.org/coding-scheme/external/iso17442"), TypedValue = "549300EXAMPLEBANK001" }],
        },
    ],
};

string xml = document.ToString();
document.Save("party.xml");
```

### Edit and save

Load, change what you need, save. The rest of the document stays as it was:

```csharp
var fpml = dataDocument.Load("ird-ex08-fra.xml");
var deal = (fra)fpml.trade[0].product;

deal.notional.amount = 30_000_000m;
fpml.Save("ird-ex08-fra-amended.xml");
```

### Validate

Each assembly carries the official FpML schemas inside it, so validation works offline with no setup at all. It
catches what the typed API can't, such as a missing required attribute:

```csharp
var draft = new dataDocument
{
    fpmlVersion = "5-13",
    party = [new Party { id = "p1", partyId = [new PartyId { TypedValue = "ACME" }] }],
};

foreach (var problem in FpmlSchema.Validate(draft))
    Console.WriteLine(problem.Message);   // The required attribute 'partyIdScheme' is missing.
```

It works on plain XML from anywhere, too:

```csharp
var incoming = XDocument.Load("from-counterparty.xml");
bool ok = FpmlSchema.IsValid(incoming);              // errors only; warnings are ignored
var schemas = FpmlSchema.CreateSchemaSet();          // a compiled XmlSchemaSet, e.g. for XmlReader validation
```

### Use several views or versions together

Every view and version lives in its own namespace, so mix them freely. A `using` alias keeps things tidy:

```csharp
using Confirmation = fpml_5_13_confirmation;
using Reporting = fpml_5_13_reporting;

var confirmation = Confirmation.XRoot.Load("confirmation.xml");
var report = new Reporting.dataDocument { fpmlVersion = "5-13" };
```

### Drop down to LINQ to XML

Every typed object wraps an `XElement`, available as `Untyped`. Use it for anything the typed API doesn't cover;
changes made through either side show up in the other:

```csharp
var elementNames = fpml.trade[0].Untyped.Elements().Select(e => e.Name.LocalName);
fpml.party[0].Untyped.SetAttributeValue("id", "renamed");   // fpml.party[0].id is now "renamed"
```

## Good to know

- **Lists and nulls:** repeating elements are lists (`fpml.trade`, `party.partyId`); optional elements that are
  absent are `null`.
- **Values:** elements with text and attributes, like `<partyId partyIdScheme="...">`, are objects. The text is
  `TypedValue`, already converted to `string`, `DateTime`, `decimal` and so on, and the attributes are properties.
- **Validate before you send:** the typed API happily builds incomplete documents, so run `FpmlSchema.Validate` on
  anything you produce.
- **Thread safety:** `FpmlSchema.Validate` and `IsValid` are thread-safe and never modify your document.
  `CreateSchemaSet` returns a new set each time.
- **XML Signature:** the W3C signature types are in each view's own `xmldsig` namespace, such as
  `fpml_5_13_confirmation.xmldsig.Signature`.

## Official schemas, untouched

Every schema is a byte-for-byte copy of FpML's published files from the latest Recommendation build of its version,
and so are the official examples used in the tests. [`official-files.txt`](official-files.txt) records where each file
came from with its SHA-256, and the tests check it on every build. FpML is a registered trademark of ISDA; see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Get involved

Questions, ideas, bug reports and pull requests are all very welcome. Start with
[CONTRIBUTING.md](CONTRIBUTING.md); it gets you building in three commands. See [CHANGELOG.md](CHANGELOG.md) for
what's new, [SECURITY.md](SECURITY.md) to report a vulnerability, and [MAINTAINING.md](MAINTAINING.md) for releases
and adding FpML versions.

## License

FpmlToolKit.Net is [MIT licensed](LICENSE). The FpML and W3C schemas it includes are under their owners' terms; see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
