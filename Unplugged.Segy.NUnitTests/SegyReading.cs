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
			Assert.That(segy.Traces.Count, Is.EqualTo(111));
		}

        [Test]
        public void ShouldReadBigEndianIEEEFloatingPoint()
        {
            var subject = new SegyReader();
            var segy = subject.Read(@"./Examples/bigEndianIEEEFloat.sgy");
            Console.WriteLine(segy.Header.Text);
            Assert.That(segy.Traces.Count, Is.EqualTo(120));
            Assert.That(segy.Traces[0].Values[0], Is.EqualTo(0));
            Assert.That(segy.Traces[60].Values[159], Is.EqualTo(0.896f).Within(0.001f));
            Assert.That(segy.Traces[60].Values[160], Is.EqualTo(1.000f).Within(0.001f));
            Assert.That(segy.Traces[60].Values[161], Is.EqualTo(0.896f).Within(0.001f));
        }
		
		[Test]
		public void ShouldGetImageBytes()
		{
			var reader = new SegyReader();
			var segy = reader.Read(@"./Examples/lineE.sgy");
			var imageWriter = new ImageWriter();
			var bytes = imageWriter.GetRaw32BppRgba(segy.Traces);
            var expected = 4 * segy.Traces.Count * segy.Traces[0].Values.Count;
			Assert.That(bytes.Length, Is.EqualTo(expected));
		}
		
		[Test]
		public void ShouldReturnEmptyArrayForNoTraces()
		{
			var bytes = new ImageWriter().GetRaw32BppRgba(new ITrace[]{});
            Assert.That(bytes.Length, Is.EqualTo(0));
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
            var expected = (int)(.3 * 111);
			Assert.That(segy.Traces.Count, Is.EqualTo(expected));
		}
	}
}
