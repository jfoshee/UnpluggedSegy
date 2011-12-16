using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestDrivenDesign;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class ImageWriterTest : TestBase<ImageWriter>
    {
        [TestMethod]
        public void ShouldCreateImageFile()
        {
            // Arrange
            var path = TestPath() + ".png";
            ISegyFile segyFile = new Mock<ISegyFile>().Object;

            // Act
            Subject.Write(segyFile, path);

            // Assert
            BinaryFileAssert.Exists(path);
            var image = Bitmap.FromFile(path);
            Assert.AreEqual(1, image.Width);
            Assert.AreEqual(1, image.Height);
        }

        [TestMethod]
        public void ShouldHaveCorrectResolution()
        {
            // Arrange
            var path = TestPath() + ".png";
            var expectedWidth = 5;
            var expectedHeight = 21;
            var trace = new MockTrace { Values = new float[expectedHeight] };
            var segy = new MockSegyFile { Traces = new ITrace[expectedWidth] };
            for (int i = 0; i < expectedWidth; i++)
                segy.Traces[i] = trace;

            // Act
            Subject.Write(segy, path);

            // Assert
            var image = Bitmap.FromFile(path);
            Assert.AreEqual(expectedWidth, image.Width);
            Assert.AreEqual(expectedHeight, image.Height);
        }

        [TestMethod]
        public void PixelValuesShouldBeBlackToWhite()
        {
            // Arrange
            var traceValues = new float[] { 150, 175, 125, 200, 111, 100, 123 };    // Min: 100, Max: 200
            var trace = new MockTrace { Values = traceValues };
            var segy = new MockSegyFile { Traces = new ITrace[] { trace } };
            var path = TestPath() + ".png";

            // Act
            Subject.Write(segy, path);

            // Assert
            Bitmap image = new Bitmap(path);
            Assert.AreEqual(Color.FromArgb(0, 0, 0), image.GetPixel(0, 5));
            Assert.AreEqual(Color.FromArgb(127, 127, 127), image.GetPixel(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 255, 255), image.GetPixel(0, 3));
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ImageFromExample()
        {
            // Arrange
            var segy = new SegyReader().Read("lineE.sgy");
            var path = TestPath() + ".png";

            // Act
            Subject.Write(segy, path);

            // Assert
            TestContext.AddResultFile(path);
        }

        [TestMethod]
        public void NullSamplesShouldBeTransparent()
        {
            // Arrange: A sample value of exactly 0
            var traceValues = new float[] { 0.00001f, 0, -0.00001f };
            var trace = new MockTrace { Values = traceValues };
            var segy = new MockSegyFile { Traces = new ITrace[] { trace } };
            var path = TestPath() + ".png";

            // Act
            Subject.Write(segy, path);

            // Assert
            Bitmap image = new Bitmap(path);
            Assert.AreEqual(255, image.GetPixel(0, 0).A);
            Assert.AreEqual(0, image.GetPixel(0, 1).A);
            Assert.AreEqual(255, image.GetPixel(0, 2).A);
        }

        // TODO: Handle variable length traces
    }

    class MockTrace : ITrace
    {
        public ITraceHeader Header { get; set; }
        public IList<float> Values { get; set; }
    }

    class MockSegyFile : ISegyFile
    {
        public IFileHeader Header { get; set; }
        public IList<ITrace> Traces { get; set; }
    }

}
