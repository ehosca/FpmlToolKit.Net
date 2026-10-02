#FpmlToolKit.Net

FpmlToolKit.Net exists to speed up the initial ramp up time for projects that deal with FpML documents. 
Its based on [Linq2XSD](http://linqtoxsd.codeplex.com).

You can head over to [downloads](https://github.com/ehosca/FpmlToolKit.Net/downloads) and grab the pre-built 
binaries and start using them right away, or you can fork this project for fine tuning the output to your
specific needs.

## Building (2.0, in progress)

The 2.0 line targets `netstandard2.0` and `net10.0` and uses [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore)
in place of the original CodePlex LinqToXsd. Schema projects cover FpML 4.0-4.9 and 5.0-5.13. For 5.4-5.13
they include every view in FpML's downloads (legal from 5.7, pretrade from 5.5) except 5.8 transparency, whose
published schema does not compile (`fpml-business-events-5-8.xsd` uses a `Withdrawal` type the view never declares;
fixed in 5.9). For 5.0-5.3 they include the views this project has carried since 2012.

    dotnet tool restore
    dotnet build FpmlToolKit.slnx
    dotnet test FpmlToolKit.slnx

C# classes are generated from the XSDs at build time into `obj/` (see `Directory.Build.targets`) and are not
committed, since each schema set produces about 10 MB of code. A project regenerates only when the content hash of
its schemas, namespace config, `dotnet-tools.json` or `build/LinqToXsd.targets` changes; CI caches the generated code
between runs, so a push regenerates only the projects whose schemas changed. `build/LinqToXsd.targets` also works around a
LinqToXsdCore bug that duplicates `creditCurve.name` and `creditCurve.currency` in FpML 4.1-4.9.

To add a schema version, download its schemas from [fpml.org](https://www.fpml.org/the_standard/current/), unzip them
and run `dotnet run scripts/add-schema-version.cs -- <folder>`. It creates one `fpml-<version>-<view>` project per
`fpml-main-*.xsd` it finds under the folder (skipping schemas that do not compile), adds it to the solution, and copies
five schema-valid official examples per view into `tests/FpmlToolKit.Tests/Examples/`. The tests validate each example,
load it through the generated `XRoot` API and check that it round-trips unchanged.


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

