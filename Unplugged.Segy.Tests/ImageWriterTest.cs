using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestDrivenDesign;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class ImageWriterTest : TestBase<ImageWriter>
    {
        [TestMethod]
        public void ImageWriterOptions()
        {
            // Arrange
            bool setNullValuesToTransparent = Subject.SetNullValuesToTransparent;

            // Assert
            Assert.AreEqual(false, setNullValuesToTransparent);
        }

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
        public void ShouldGetBitmapWithoutWritingIt()
        {
            // Arrange
            var expectedWidth = 5;
            var expectedHeight = 21;
            var segy = new MockSegyFile(expectedWidth, expectedHeight);

            // Act
            Bitmap image = Subject.GetBitmap((ISegyFile)segy);

            // Assert
            Assert.AreEqual(expectedWidth, image.Width);
            Assert.AreEqual(expectedHeight, image.Height);
            image.Dispose();
        }

        [TestMethod]
        public void ShouldHaveCorrectResolution()
        {
            // Arrange
            var expectedWidth = 7;
            var expectedHeight = 11;
            var segy = new MockSegyFile(expectedWidth, expectedHeight);
            var path = TestPath() + ".png";

            // Act
            Subject.Write(segy, path);

            // Assert
            using (var image = Bitmap.FromFile(path))
            {
                Assert.AreEqual(expectedWidth, image.Width);
                Assert.AreEqual(expectedHeight, image.Height);
            }
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

        [TestMethod]
        public void NullSamplesShouldBeTransparent()
        {
            // Arrange: A sample value of exactly 0
            var traceValues = new float[] { 0.00001f, 0, -0.00001f };
            var trace = new MockTrace { Values = traceValues };
            var segy = new MockSegyFile { Traces = new ITrace[] { trace } };
            var path = TestPath() + ".png";
            Subject.SetNullValuesToTransparent = true;

            // Act
            Subject.Write(segy, path);

            // Assert
            Bitmap image = new Bitmap(path);
            Assert.AreEqual(255, image.GetPixel(0, 0).A);
            Assert.AreEqual(0, image.GetPixel(0, 1).A);
            Assert.AreEqual(255, image.GetPixel(0, 2).A);
        }

        [TestMethod]
        public void WhenDisabledNullSamplesShouldNotBeTransparent()
        {
            // Arrange
            var trace = new MockTrace { Values = new float[] { 0, 1, -1 } };
            var segy = new MockSegyFile { Traces = new ITrace[] { trace } };
            var path = TestPath() + ".png";

            // Act
            Subject.SetNullValuesToTransparent = false;
            Subject.Write(segy, path);

            // Assert
            Bitmap image = new Bitmap(path);
            Assert.AreEqual(255, image.GetPixel(0, 0).A);
        }

        [TestMethod]
        public void ShouldCreateImageForEachInline()
        {
            // Arrange
            var values = new float[] { 1, 2, 3 };
            var trace12a = new MockTrace { Header = new MockTraceHeader { InlineNumber = 12, CrosslineNumber = 1 }, Values = values };
            var trace12b = new MockTrace { Header = new MockTraceHeader { InlineNumber = 12, CrosslineNumber = 2 }, Values = values };
            var trace14 = new MockTrace { Header = new MockTraceHeader { InlineNumber = 14 }, Values = values };
            var segy = new MockSegyFile { Traces = new ITrace[] { trace12a, trace12b, trace14 } };
            var path = TestPath() + ".png";

            // Act
            Subject.Write(segy, path);

            // Assert
            var image12 = new Bitmap(TestPath() + " (12).png");
            Assert.AreEqual(3, image12.Height);
            Assert.AreEqual(2, image12.Width);
            var image14 = new Bitmap(TestPath() + " (14).png");
            Assert.AreEqual(3, image14.Height);
            Assert.AreEqual(1, image14.Width);
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
            BinaryFileAssert.Exists(path);
            TestContext.AddResultFile(path);
        }

        [TestMethod]
        public void ShouldCreateBlackImageWhenRangeIsZero()
        {
            // Arrange
            var path = TestPath() + ".png";
            Subject.SetNullValuesToTransparent = false;

            // Act
            Subject.Write(new MockSegyFile(10, 10), path);

            // Assert
            TestContext.AddResultFile(path);
            Bitmap image = new Bitmap(path);
            Assert.AreEqual(0, image.GetPixel(0, 0).R);
            Assert.AreEqual(255, image.GetPixel(0, 0).A);
        }

        [TestMethod]
        public void ShouldCreateImageWithGivenTraces()
        {
            // Arrange
            IEnumerable<ITrace> traces = MockTraces2x3();
            var path = TestPath() + ".jpg";

            // Act
            Subject.Write(traces, path);

            // Assert
            BinaryFileAssert.Exists(path);
            TestContext.AddResultFile(path);
            var image = new Bitmap(path);
            VerifyBitmapFromMockTraces2x3(image);
        }

        [TestMethod]
        public void ShouldGetBitmapWithGivenTraces()
        {
            // Arrange
            IEnumerable<ITrace> traces = MockTraces2x3();

            // Act
            Bitmap image = Subject.GetBitmap(traces);

            // Assert
            VerifyBitmapFromMockTraces2x3(image);
        }

        [TestMethod]
        public void ShouldGetFourComponentByteArray()
        {
            // Arrange
            IEnumerable<ITrace> traces = MockTraces2x3();

            // Act
            byte[] bytes = Subject.GetRaw32bppRgba(traces);

            // Assert
            Assert.AreEqual(2 * 3 * 4, bytes.Length);
            Assert.AreEqual(127, bytes[0], "r");
            Assert.AreEqual(255, bytes[3], "a");
            using (var bmp = Subject.GetBitmap(traces))
            {
                var bmpData = bmp.LockBits(Rectangle.FromLTRB(0, 0, 2, 3), ImageLockMode.ReadOnly, bmp.PixelFormat);
                int length = bmpData.Stride * bmp.Height;
                byte[] expected = new byte[length];
                Marshal.Copy(bmpData.Scan0, expected, 0, length);
                CollectionAssert.AreEqual(expected, bytes);
            }
        }

        [TestMethod]
        public void ShouldGetSingleComponentByteArray()
        {
            // Arrange
            IEnumerable<ITrace> traces = MockTraces2x3();

            // Act
            byte[] bytes = Subject.GetRaw8bpp(traces);

            // Assert
            var expected = new byte[] 
            { 
                127, 63,    // 20, 15
                0, 255,     // 10, 30
                191, 191    // 25, 25
            };
            CollectionAssert.AreEqual(expected, bytes);
        }

        private static IEnumerable<ITrace> MockTraces2x3()
        {
            var trace1 = new MockTrace { Values = new float[] { 20, 10, 25 } };
            var trace2 = new MockTrace { Values = new float[] { 15, 30, 25 } };
            return new ITrace[] { trace1, trace2 };
        }

        private static void VerifyBitmapFromMockTraces2x3(Bitmap image)
        {
            Assert.AreEqual(2, image.Width);
            Assert.AreEqual(3, image.Height);
            Assert.AreEqual(Color.FromArgb(127, 127, 127), image.GetPixel(0, 0));
            image.Dispose();
        }

        [TestMethod]
        public void GivenTracesForCrosslineShouldCreateOneImage()
        {
            // Arrange
            var trace1 = new MockTrace { Values = new float[] { 99 }, Header = new MockTraceHeader { CrosslineNumber = 1, InlineNumber = 10 } };
            var trace2 = new MockTrace { Values = new float[] { 77 }, Header = new MockTraceHeader { CrosslineNumber = 1, InlineNumber = 20 } };
            IEnumerable<ITrace> traces = new ITrace[] { trace1, trace2 };
            var path = TestPath() + ".jpg";

            // Act
            Subject.Write(traces, path);

            // Assert
            BinaryFileAssert.Exists(path);
            TestContext.AddResultFile(path);
            var image = new Bitmap(path);
            Assert.AreEqual(2, image.Width);
            Assert.AreEqual(1, image.Height);
        }

        //[TestMethod]
        //public void ImageFromExample3D()
        //{
        //    // Arrange
        //    var reader = new SegyReader { InlineNumberLocation = 17 };
        //    var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        //    var segy = reader.Read(userprofile + @"\Desktop\RMOTC Data\RMOTC Seismic data set\3D_Seismic\filt_mig.sgy");
        //    var path = TestPath() + ".png";

        //    // Act
        //    Subject.Write(segy, path);

        //    // Assert
        //    TestContext.AddResultFile(path);
        //}

        // TODO: Should verify trace range normalized over whole volume
        // TODO: Handle variable length traces
    }

    class MockTraceHeader : ITraceHeader
    {
        public int SampleCount { get; set; }
        public int TraceNumber { get; set; }
        public int InlineNumber { get; set; }
        public int CrosslineNumber { get; set; }
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

        public MockSegyFile()
        {
        }

        public MockSegyFile(int traces, int samplesPerTrace)
        {
            var trace = new MockTrace { Values = new float[samplesPerTrace] };
            Traces = new ITrace[traces];
            for (int i = 0; i < traces; i++)
                Traces[i] = trace;
        }
    }

}
