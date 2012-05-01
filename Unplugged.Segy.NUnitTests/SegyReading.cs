using System;
using NUnit.Framework;

namespace Unplugged.Segy.MonoTouch.Tests
{
	[TestFixture]
    public class SegyReading
	{
		[Test]
		public void ShouldReadSegyFile()
		{
			var subject = new SegyReader();
			var segy = subject.Read(@"/Users/Jacob/Documents/GitHub/UnpluggedSegy/Unplugged.Segy.Tests/Examples/lineE.sgy");
			Console.WriteLine(segy.Header.Text);
		}
		
		[Test]
		public void Canary()
		{
			Assert.That(true);
		}
	}
}
