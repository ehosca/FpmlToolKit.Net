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

Load a document and validate it against the FpML schemas, which are embedded in each view assembly:

```csharp
using fpml_5_13_confirmation;

var doc = XRoot.Load("trade.xml");                 // typed root element in doc.Root
foreach (var problem in FpmlSchema.Validate(doc.Root))
    Console.WriteLine($"{problem.Severity}: {problem.Message}");

bool ok = FpmlSchema.IsValid(doc.Root);            // errors only; warnings are ignored
var schemas = FpmlSchema.CreateSchemaSet();         // compiled XmlSchemaSet, e.g. for XmlReader validation
```

`FpmlSchema` resolves includes and imports from the embedded files only; it never reads schemas from disk or the
network. `SchemaFileNames` and `OpenSchema` give access to the raw XSDs.

Each view assembly has its own copy of the W3C XML Signature types, in a namespace under the view's own namespace:
`fpml_5_13_confirmation.xmldsig.Signature`, `fpml_4_9.xmldsig.Signature` and so on. (Before 2.0.0-beta.2 they were all
`org.w3.xmldsig.*`, which made the names ambiguous when more than one view assembly was referenced.)

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
in place of the original CodePlex LinqToXsd. Schema projects cover FpML 4.0-4.9 and 5.0-5.13. For 5.4-5.13
they include every view in FpML's downloads (legal from 5.7, pretrade from 5.5) except 5.8 transparency, whose
published schema does not compile (`fpml-business-events-5-8.xsd` uses a `Withdrawal` type the view never declares;
fixed in 5.9). For 5.0-5.3 they include the views this project has carried since 2012.

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

To add a schema version, download its schemas from [fpml.org](https://www.fpml.org/the_standard/current/), unzip them
and run `dotnet run scripts/add-schema-version.cs -- <folder>`. It creates one `fpml-<version>-<view>` project per
`fpml-main-*.xsd` it finds under the folder (skipping schemas that do not compile), adds it to the solution, and copies
five schema-valid official examples per view into `tests/FpmlToolKit.Tests/Examples/`. The tests validate each example,
load it through the generated `XRoot` API and check that it round-trips unchanged. It also regenerates
`packages/FpmlToolKit.Fpml<version>/`, the per-version package projects (see `build/VersionPackage.targets`);
`dotnet pack FpmlToolKit.slnx` produces one `.nupkg` per FpML version.


    using System;
    using fpml_5_0_reporting;
     
    namespace FpMLWithLinqToXsd
    {
    	internal class Program
    	{
    		private static void Main(string[] args)
    		{             
    			//load the entire document in one step.             
    			creditEventNotification cen = creditEventNotification.Load(@"C:\fpml\reporting\credit-event-notice\msg-ex21-credit-event-notice.xml");               
    			// creditEventNotification ready for action !
    			Console.WriteLine(cen.creditEventNotice.creditEventDate);             
    			Console.WriteLine(cen.creditEventNotice.notifiedPartyReference.href);               
    			Console.ReadKey();         
    		}
    	}
    }



## License

The MIT license below covers FpmlToolKit.Net's own code. The FpML and W3C schemas included in the repository and
packages are under their owners' terms; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Copyright (C) 2012 Erhan Hosca

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

