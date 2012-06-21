using System;
using System.Collections.Generic;
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
			Assert.That(segy.Traces.Count == 111);
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
		public void ShouldReturnEmptyArrayForNoTraces()
		{
			var bytes = new ImageWriter().GetRaw32bppRgba(new ITrace[]{});
            Assert.That(bytes.Length == 0);
		}
		
		class TestProgressReporter : IReadingProgress
		{
			public void ReportProgress (int progressPercentage)
			{
				ProgressReported.Add(progressPercentage);
			}

			public bool CancellationPending { get; set; }
			public List<int> ProgressReported { get; private set; } 
			
			public TestProgressReporter ()
			{
				ProgressReported = new List<int>();
			}
		}
		
		[Test]
		public void ShouldReportProgress()
		{
			var subject = new SegyReader();
			var testProgressReporter = new TestProgressReporter();
			
			subject.Read(@"./Examples/lineE.sgy", testProgressReporter);
			
			// Assert that message received for each percentage 0 to 100
			// (The example has more than 100 traces, so this is reasonable)
			var p = testProgressReporter.ProgressReported;
			for (int i = 0; i < 101; i++)
				Assert.That(p.Contains(i), i.ToString());
		}
		
		class CancelsAtThirty : IReadingProgress
		{
			public void ReportProgress (int progressPercentage)
			{
				if (progressPercentage == 30)
					CancellationPending = true;
				if (progressPercentage > 30)
					Assert.Fail("Should not proceed past 30%");
			}
			public bool CancellationPending { get; set; }
		}
		
		[Test]
		public void ShouldStopReadingWhenCancellationPending()
		{
			var subject = new SegyReader();
			var segy = subject.Read(@"./Examples/lineE.sgy", new CancelsAtThirty());
			Assert.That(segy.Traces.Count == (int)(.3 * 111), segy.Traces.Count.ToString());
		}
		
		[Test]
		public void Canary()
		{
			Assert.That(true);
		}
	}
}
