FpmlToolKit.Net
================

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
			Console.WriteLine(cen.creditEventNotice.creditEventDate);             
			Console.WriteLine(cen.creditEventNotice.notifiedPartyReference.href);               
			Console.ReadKey();         
		}
	}
}

