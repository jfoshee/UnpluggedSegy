using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDrivenDesign;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class SegyReaderTest : TestBase<SegyReader>
    {
        /// The text header is typically 3200 EBCDIC encoded character bytes.  That is, 80 columns by 40 lines (formerly punch cards).
        #region Textual Header

        [TestMethod]
        public void ShouldReadHeaderFromEbcdicEncoding()
        {
            // Arrange some Ebcdic characters at the beginning of the file
            var expected = "Once upon a time...";
            var ebcdicBytes = ConvertToEbcdic(expected);
            var path = TestPath();
            File.WriteAllBytes(path, ebcdicBytes);

            // Act
            string header = Subject.ReadTextHeader(path);

            // Assert
            StringAssert.StartsWith(header, expected);
        }

        [TestMethod]
        public void ShouldRead3200BytesForTextHeader()
        {
            // Arrange some Ebcdic characters at the beginning of the file
            var expected = "TheEnd";
            var ebcdicBytes = ConvertToEbcdic(new string('c', 3200 - 6) + expected + "notExpected");
            File.WriteAllBytes(TestPath(), ebcdicBytes);

            // Act
            var header = Subject.ReadTextHeader(TestPath());

            // Assert
            StringAssert.EndsWith(header.TrimEnd(), expected);
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadExampleTextHeader()
        {
            // Act
            var header = Subject.ReadTextHeader("lineE.sgy");

            // Assert
            Console.WriteLine(header);
            StringAssert.StartsWith(header, "C 1");
            StringAssert.Contains(header, "CLIENT");
            StringAssert.EndsWith(header.TrimEnd(), "END EBCDIC");
        }

        [TestMethod]
        public void ShouldInsertNewlinesEvery80Characters()
        {
            // Arrange
            var line1 = new string('a', 80);
            var line2 = new string('b', 80);
            var line3 = new string('c', 80);
            var bytes = ConvertToEbcdic(line1 + line2 + line3);
            File.WriteAllBytes(TestPath(), bytes);

            // Act
            var header = Subject.ReadTextHeader(TestPath());

            // Assert
            var lines = SplitLines(header);
            Assert.AreEqual(line1, lines[0]);
            Assert.AreEqual(line2, lines[1]);
            Assert.AreEqual(line3, lines[2]);
        }

        #endregion

        /// 400-byte Binary File Header follows 3200 byte textual header
        /// Bytes 3225-3226: Data sample format code
        #region Binary File Header

        [TestMethod]
        public void ShouldReadSampleFormatFromByte25()
        {
            // Arrange
            var expected = FormatCode.IeeeFloatingPoint4;

            // Act
            IFileHeader result = SetValueInBinaryStreamAndRead((sr, br) => sr.ReadBinaryHeader(br), 25, (int)expected);

            // Assert
            Assert.AreEqual(expected, result.SampleFormat);
        }

        [TestMethod]
        public void ShouldConsume400Bytes()
        {
            BinaryReaderExtensionMethodsTest.AssertBytesConsumed((r) => Subject.ReadBinaryHeader(r), 400);
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadTextAndBinaryHeaderGivenBinaryReader()
        {
            // Arrange
            using (var stream = File.OpenRead("lineE.sgy"))
            using (var reader = new BinaryReader(stream))
            {
                // Act
                IFileHeader fileHeader = Subject.ReadFileHeader(reader);

                // Assert
                Assert.AreEqual(Subject.ReadTextHeader("lineE.sgy"), fileHeader.Text);
                Assert.AreEqual(FormatCode.IbmFloatingPoint4, fileHeader.SampleFormat);
            }
        }

        #endregion

        /// The Trace Header is typically 240 bytes with Big Endian values
        /// Info from the standard:
        /// 115-116 Number of samples in this trace
        /// 117-118 Sample Interval in Micro-Seconds
        /// 181-194 X CDP
        /// 185-188 Y CDP
        /// 189-192 Inline number (for 3D)
        /// 193-196 Cross-line number (for 3D)
        /// 
        /// Header Information from Teapot load sheet
        /// Inline (line)     bytes:   17- 20 and 181-184
        /// Crossline (trace) bytes:   13- 16 and 185-188
        /// CDP X_COORD       bytes:   81- 84 and 189-193
        /// CDP Y_COORD       bytes:   85- 88 and 193-196
        #region Trace Header

        [TestMethod]
        public void SystemShouldBeLittleEndian()
        {
            Assert.IsTrue(BitConverter.IsLittleEndian, "These tests assume the system is little endian");
        }

        [TestMethod]
        public void ShouldReadNumberOfSamplesFromByte115()
        {
            // Arrange
            var expectedValue = 2345;

            // Act
            ITraceHeader result = SetValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), 115, expectedValue);

            // Assert
            Assert.AreEqual(expectedValue, result.SampleCount);
        }

        [TestMethod]
        public void ShouldConsume240Bytes()
        {
            BinaryReaderExtensionMethodsTest.AssertBytesConsumed(r => Subject.ReadTraceHeader(r), 240);
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadTraceHeaderFromExample()
        {
            // Arrange
            using (var stream = File.OpenRead("lineE.sgy"))
            using (var reader = new BinaryReader(stream))
            {
                var header = Subject.ReadFileHeader(reader);

                // Act
                var traceHeader = Subject.ReadTraceHeader(reader);

                // Assert
                Assert.AreEqual(1001, traceHeader.SampleCount);
                for (int i = 0; i < traceHeader.SampleCount; i++)
                {
                    var value = reader.ReadSingleIbm();
                    Console.WriteLine(value);
                    Assert.IsTrue(value < 10 && value > -10);
                }
                Assert.AreEqual(1001, Subject.ReadTraceHeader(reader).SampleCount);
            }
        }

        #endregion

        #region Trace Data

        [TestMethod]
        public void ShouldConsumeAllBytesInTrace()
        {
            // Arrange
            FormatCode sampleFormat = FormatCode.IeeeFloatingPoint4;
            int sampleCount = 200;
            var expected = 800;
            IList<float> result = null;

            // Act
            BinaryReaderExtensionMethodsTest.AssertBytesConsumed(r => result = Subject.ReadTrace(r, sampleFormat, sampleCount), expected);

            // Assert
            Assert.AreEqual(sampleCount, result.Count);
        }

        [TestMethod]
        public void ShouldReadSinglesForIeeeFormat()
        {
            // Arrange
            var sampleFormat = FormatCode.IeeeFloatingPoint4;
            var expected = new float[] { 10, 20, 30 };
            var sampleCount = expected.Length;
            IList<float> result = null;
            var bytes = BitConverter.GetBytes(10f).Concat(BitConverter.GetBytes(20f)).Concat(BitConverter.GetBytes(30f)).ToArray();

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                result = Subject.ReadTrace(reader, sampleFormat, sampleCount);

            // Assert
            CollectionAssert.AreEqual(expected, result.ToList());
        }

        #endregion

        #region Behind the scenes

        private static byte[] ConvertToEbcdic(string expected)
        {
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = unicode.GetBytes(expected);
            var ebcdicBytes = Encoding.Convert(unicode, ebcdic, unicodeBytes);
            return ebcdicBytes;
        }

        private static string[] SplitLines(string header)
        {
            return header.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        }

        private TResult SetValueInBinaryStreamAndRead<TResult>(Func<SegyReader, BinaryReader, TResult> act, int byteNumber, int value)
        {
            // Arrange
            var samplesBytes = BitConverter.GetBytes(value);
            var bytes = new byte[byteNumber + 2];
            bytes[byteNumber - 1] = samplesBytes[1];
            bytes[byteNumber] = samplesBytes[0];

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                return act(Subject, reader);
        }

        #endregion
    }
}
