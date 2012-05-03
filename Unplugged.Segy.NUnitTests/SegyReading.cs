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
			var segy = subject.Read(@"./Examples/lineE.sgy");
			Console.WriteLine(segy.Header.Text);
		}
		
		[Test]
		public void ShouldGetImageBytes()
		{
			var reader = new SegyReader();
			var segy = reader.Read(@"./Examples/lineE.sgy");
			var imageWriter = new ImageWriter();
			var bytes = imageWriter.GetRaw32bppRgba(segy.Traces);
			Assert.That(bytes.Length == 4 * segy.Traces.Count * segy.Traces[0].Values.Count);
		}
		
		[Test]
		public void Canary()
		{
			Assert.That(true);
		}
	}
}
