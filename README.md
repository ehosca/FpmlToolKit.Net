#FpmlToolKit.Net

FpmlToolKit.Net exists to speed up the initial ramp up time for projects that deal with FpML documents. 
Its based on [Linq2XSD](http://linqtoxsd.codeplex.com).

You can head over to [downloads](https://github.com/ehosca/FpmlToolKit.Net/downloads) and grab the pre-built 
binaries and start using them right away, or you can fork this project for fine tuning the output to your
specific needs.


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

