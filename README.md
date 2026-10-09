# FpmlToolKit.Net

FpmlToolKit.Net exists to speed up the initial ramp up time for projects that deal with FpML documents.
It generates strongly typed classes from the FpML schemas with [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore).

## Packages

There is one NuGet package per FpML version, `FpmlToolKit.Fpml<major><minor>` (for example `FpmlToolKit.Fpml513`), each
containing one assembly per view (`fpml-5-13-confirmation.dll`, `fpml-5-13-reporting.dll`, ...). Packages target `netstandard2.0` and `net10.0` and are attached to each
[GitHub release](https://github.com/ehosca/FpmlToolKit.Net/releases). To use one, download the `.nupkg` into a folder,
add that folder as a package source, and reference the package:

    dotnet nuget add source ~/fpml-packages --name fpmltoolkit
    dotnet add package FpmlToolKit.Fpml513 --prerelease

Packages are not on NuGet.org yet. See [CHANGELOG.md](CHANGELOG.md) for what changed in each release.

## Usage

Each view has its own namespace, named after its assembly: `fpml_5_13_confirmation`, `fpml_5_13_reporting`,
`fpml_4_9` and so on. Element and type names are the FpML names unchanged, so the FpML specification documents the
API: element `<dataDocument>` is class `dataDocument`, complex type `Party` is class `Party`, and child elements and
attributes are properties with the same names. The examples use the 5.13 confirmation view; the other views and
versions work the same way.

### Reading a document

When you don't know which root element a file has, load it through `XRoot` and look at the type of `Root`:

```csharp
using fpml_5_13_confirmation;

var doc = XRoot.Load("trade.xml");
if (doc.Root is dataDocument data)
{
    foreach (var trade in data.trade)
        Console.WriteLine($"Trade dated {trade.tradeHeader.tradeDate.TypedValue:yyyy-MM-dd}");

    foreach (var party in data.party)
        Console.WriteLine($"{party.id}: {party.partyId[0].TypedValue}");
}
```

When you do know it, each root element class has static `Load` (from a file) and `Parse` (from a string) methods:

```csharp
var data = dataDocument.Load("trade.xml");
var fromString = dataDocument.Parse(xmlText);
```

- Repeating elements are lists (`data.trade`, `party.partyId`); optional elements that are absent are `null`.
- Elements with text content and attributes, such as `<partyId partyIdScheme="...">`, are objects: the text is
  `TypedValue`, converted to its schema type (`string`, `DateTime`, `decimal`, ...), and the attributes are properties.
- Every typed object wraps an `XElement`, available as `Untyped`, for anything the typed API doesn't cover:
  `data.Untyped.Descendants()`, LINQ to XML queries, and so on. Changes through either view are visible in the other.

### Building a document

Create objects with initializers, then serialise with `ToString()` or `Save`:

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

The typed API does not check that required elements and attributes are present or that values meet the schema's
constraints, so validate documents you build before sending them.

### Validating

Each view assembly embeds the FpML schemas it was generated from, and `FpmlSchema` validates against them:

```csharp
foreach (var problem in FpmlSchema.Validate(document))      // typed element, or an XDocument
    Console.WriteLine($"{problem.Severity}: {problem.Message}");

bool ok = FpmlSchema.IsValid(document);                       // errors only; warnings are ignored
var schemas = FpmlSchema.CreateSchemaSet();                   // compiled XmlSchemaSet, e.g. for XmlReader validation
```

- `FpmlSchema` resolves includes and imports from the embedded files only; it never reads schemas from disk or the
  network. `SchemaFileNames` and `OpenSchema` give access to the raw XSDs.
- `Validate` and `IsValid` are thread-safe and don't modify the document. `CreateSchemaSet` returns a new set each
  time; `XmlSchemaSet` itself is not thread-safe.

### Using several views or versions

Every view of a version ships in that version's package, and packages for different versions can be referenced side by
side: each assembly has its own namespace, so no type name is defined twice. When a file uses the same class names
from two views, qualify them or use a `using` alias:

```csharp
using Confirmation = fpml_5_13_confirmation;
using Reporting = fpml_5_13_reporting;

var confirmation = Confirmation.XRoot.Load("confirmation.xml");
var report = Reporting.XRoot.Load("report.xml");
```

The W3C XML Signature types are generated into each view too, under a namespace inside the view's:
`fpml_5_13_confirmation.xmldsig.Signature`, `fpml_4_9.xmldsig.Signature` and so on.

## Releasing

Releases are made by pushing a version tag on a commit that is already on `main`:

    git tag v2.0.0-beta.2
    git push origin v2.0.0-beta.2

The Release workflow (`.github/workflows/release.yml`) builds and tests everything with that version, packs one
`.nupkg` per FpML version and attaches them to a GitHub release for the tag; versions with a suffix such as `-beta.2`
are marked as prereleases. Running the workflow manually from the Actions tab does a dry run: the packages are kept as a
workflow artifact, with no GitHub release and no NuGet.org push.

Publishing the same packages to NuGet.org is off until it is switched on:

1. On nuget.org, create an API key with the **Push new packages and package versions** scope, limited to the glob
   pattern `FpmlToolKit.*`, and add it as the repository secret `NUGET_API_KEY` (Settings > Secrets and variables >
   Actions).
2. Optionally, add a required reviewer to the `nuget` environment (Settings > Environments), so each push waits for
   approval after the GitHub release is created.
3. Set the repository variable `NUGET_PUBLISH` to `true`.

The next version tag then also runs the `nuget` job. It uses `--skip-duplicate`, so re-running it after a partial push
is safe; NuGet.org versions cannot be deleted, only unlisted.

## Building

The 2.0 line targets `netstandard2.0` and `net10.0` and uses [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore)
in place of the original CodePlex LinqToXsd. Schema projects cover FpML 4.0-4.10 and 5.0-5.14, each from the latest
published Recommendation build of that version (for example 5.13 build 8, `fpml-5-13-8-rec-2`). They include every
view in FpML's downloads except 5.8 transparency, whose published schema does not compile
(`fpml-business-events-5-8.xsd` uses a `Withdrawal` type the view never declares; fixed in 5.9).

The schemas (`fpml-*/xsd/*.xsd`) and the example documents (`tests/FpmlToolKit.Tests/Examples/`) are byte-for-byte
copies of FpML's "Schema and Examples" downloads and are never edited. `official-files.txt` lists every one with its
SHA-256 and the download and entry it came from, and the downloads with their own SHA-256; `.gitattributes` stops git
from converting their line endings. The tests fail if a file no longer matches the list, and
`dotnet run scripts/official-files.cs -- verify` downloads the listed zips from fpml.org and checks every file against
them.

    dotnet tool restore
    dotnet build FpmlToolKit.slnx
    dotnet test FpmlToolKit.slnx

On Windows the tests also run on .NET Framework 4.8 (`net48`), against the `netstandard2.0` build of each schema
assembly; CI does the same in a separate Windows job.

C# classes are generated from the XSDs at build time into `obj/` (see `Directory.Build.targets`) and are not
committed, since each schema set produces about 10 MB of code. A project regenerates only when the content hash of
its schemas, namespace config, `dotnet-tools.json` or `build/LinqToXsd.targets` changes; CI caches the generated code
between runs, so a push regenerates only the projects whose schemas changed. The LinqToXsdCore tool version
(`dotnet-tools.json`) and the `XObjectsCore` runtime version (`XObjectsCoreVersion` in `Directory.Build.props`) must
match.

To add a schema version, download the "Schema and Examples" zip of each view of its latest Recommendation build from
[fpml.org](https://www.fpml.org/the_standard/current/) (the download links show once you are logged in; the files
themselves need no login). Keep the zips under their path on fpml.org (for example
`zips/fpml-5-13-8-rec-2/xml/confirmation-5-13_xml.zip`), unzip copies of them elsewhere, and run
`dotnet run scripts/add-schema-version.cs -- <folder>`. It creates one `fpml-<version>-<view>` project (`fpml-<version>`
for 4.x) per `fpml-main-*.xsd` it finds under the folder (skipping schemas that do not compile), adds it to the
solution, and copies five schema-valid official examples per view into `tests/FpmlToolKit.Tests/Examples/`. The tests
validate each example, load it through the generated `XRoot` API and check that it round-trips unchanged. It also
regenerates `packages/FpmlToolKit.Fpml<version>/`, the per-version package projects (see
`build/VersionPackage.targets`); `dotnet pack FpmlToolKit.slnx` produces one `.nupkg` per FpML version. To move an
existing version to a newer FpML build, run it with `--replace`. Then run
`dotnet run scripts/official-files.cs -- write <zips>` to record the new files in `official-files.txt`.

## License

The MIT license below covers FpmlToolKit.Net's own code. The FpML and W3C schemas included in the repository and
packages are under their owners' terms; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Copyright (C) 2012 Erhan Hosca

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

