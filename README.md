#FpmlToolKit.Net

FpmlToolKit.Net exists to speed up the initial ramp up time for projects that deal with FpML documents. 
Its based on [Linq2XSD](http://linqtoxsd.codeplex.com).

You can head over to [downloads](https://github.com/ehosca/FpmlToolKit.Net/downloads) and grab the pre-built 
binaries and start using them right away, or you can fork this project for fine tuning the output to your
specific needs.

## Building (2.0, in progress)

The 2.0 line targets `netstandard2.0` and `net10.0` and uses [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore)
in place of the original CodePlex LinqToXsd. All schema projects (FpML 4.0-4.9 and 5.0-5.3) are converted.

    dotnet tool restore
    dotnet build FpmlToolKit.slnx
    dotnet test FpmlToolKit.slnx

C# classes are generated from the XSDs at build time into `obj/` (see `Directory.Build.targets`) and are not
committed, since each schema set produces about 10 MB of code. `build/LinqToXsd.targets` also works around a
LinqToXsdCore bug that duplicates `creditCurve.name` and `creditCurve.currency` in FpML 4.1-4.9.

To add a schema version, download its schemas from [fpml.org](https://www.fpml.org/the_standard/current/), unzip them
and run `dotnet run scripts/add-schema-version.cs -- <folder>`. It creates one `fpml-<version>-<view>` project per
`fpml-main-*.xsd` it finds under the folder and adds it to the solution.


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



Copyright (C) 2012 Erhan Hosca

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

