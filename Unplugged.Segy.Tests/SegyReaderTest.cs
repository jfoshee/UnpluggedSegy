using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDrivenDesign;
using Unplugged.IbmBits;

namespace Unplugged.Segy.Tests
{
    [TestClass]
    public class SegyReaderTest : TestBase<SegyReader>
    {
        #region Loading Options

        [TestMethod]
        public void InlineNumberLocation()
        {
            // Arrange
            int inlineNumberLocation = Subject.InlineNumberLocation;

            // Assert
            Assert.AreEqual(189, inlineNumberLocation, "According to SEGY Rev 1, byte 189 - 192 in the trace header should be used for the in-line number");
        }

        #endregion

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
            IFileHeader result = SetValueInBinaryStreamAndRead((sr, br) => sr.ReadBinaryHeader(br), 25, (Int16)expected);

            // Assert
            Assert.AreEqual(expected, result.SampleFormat);
        }

        [TestMethod]
        public void ShouldConsume400Bytes()
        {
            AssertBytesConsumed((r) => Subject.ReadBinaryHeader(r), 400);
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
        public void ShouldReadTraceNumberFromByte13()
        {
            // Arrange
            Int32 expectedValue = Int16.MaxValue + 100;

            // Act
            ITraceHeader result = SetValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), 13, expectedValue);

            // Assert
            Assert.AreEqual(expectedValue, result.TraceNumber);
            Assert.AreEqual(expectedValue, result.CrosslineNumber);
        }

        [TestMethod]
        public void ShouldReadInlineNumberFromSpecifiedByteLocation()
        {
            // Arrange
            Int32 expectedValue = Int16.MaxValue + 100;
            Subject.InlineNumberLocation = 123;

            // Act
            ITraceHeader result = SetValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), 123, expectedValue);

            // Assert
            Assert.AreEqual(expectedValue, result.InlineNumber);
        }

        [TestMethod]
        public void ShouldReadNumberOfSamplesFromByte115()
        {
            // Arrange
            Int16 expectedValue = 2345;

            // Act
            ITraceHeader result = SetValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), 115, expectedValue);

            // Assert
            Assert.AreEqual(expectedValue, result.SampleCount);
        }

        [TestMethod]
        public void ShouldConsume240Bytes()
        {
            AssertBytesConsumed(r => Subject.ReadTraceHeader(r), 240);
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
            AssertBytesConsumed(r => result = Subject.ReadTrace(r, sampleFormat, sampleCount), expected);

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

        [TestMethod]
        public void ShouldReadSingleIbmForIbmFormat()
        {
            // Arrange
            var sampleFormat = FormatCode.IbmFloatingPoint4;
            var given = new int[] { 96795456, 2136281153, 1025579328 }; // TODO: This test would be clearer if I had code to convert TO IBM Float
            var expected = new float[] { 0.9834598f, 1.020873f, 0.09816343f };
            var sampleCount = given.Length;
            IList<float> result = null;
            var bytes = BitConverter.GetBytes(given[0]).Concat(BitConverter.GetBytes(given[1])).Concat(BitConverter.GetBytes(given[2])).ToArray();

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                result = Subject.ReadTrace(reader, sampleFormat, sampleCount);

            // Assert
            for (int i = 0; i < sampleCount; i++)
                Assert.AreEqual(expected[i], result[i], 0.00001);
        }

        [TestMethod]
        public void ShouldReadTraceHeaderAndValuesGivenSampleFormat()
        {
            // Arrange
            var sampleFormat = FormatCode.IeeeFloatingPoint4;
            var expected = new float[] { 11, 111 };
            var bytes = new byte[240]; // trace header
            SetBigIndianValue((Int16)expected.Length, bytes, 115);
            bytes = bytes.Concat(BitConverter.GetBytes(expected[0])).Concat(BitConverter.GetBytes(expected[1])).ToArray();
            ITrace trace = null;

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                trace = Subject.ReadTrace(reader, sampleFormat);

            // Assert
            Assert.AreEqual(expected.Length, trace.Header.SampleCount);
            CollectionAssert.AreEqual(expected, trace.Values.ToList());
        }

        #endregion

        #region Reading Entire File

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadFileHeadersAndAllTraces()
        {
            // Arrange
            var path = "lineE.sgy";

            // Act
            ISegyFile result = Subject.Read(path);

            // Assert
            StringAssert.Contains(result.Header.Text, "C16 GEOPHONES");
            Assert.AreEqual(FormatCode.IbmFloatingPoint4, result.Header.SampleFormat);
            Assert.AreEqual(1001, result.Traces.First().Header.SampleCount);
            var traceValues = result.Traces.First().Values.ToList();
            Assert.AreEqual(1001, traceValues.Count);
            CollectionAssert.Contains(traceValues, 0f);
            CollectionAssert.Contains(traceValues, 0.9834598f);
            CollectionAssert.DoesNotContain(traceValues, float.PositiveInfinity);
            var fileLength = new FileInfo(path).Length;
            Assert.AreEqual((fileLength - 3200 - 400) / (240 + 1001 * 4), result.Traces.Count);
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

        private TResult SetValueInBinaryStreamAndRead<TResult>(Func<SegyReader, BinaryReader, TResult> act, int byteNumber, Int16 value)
        {
            // Arrange
            var bytes = new byte[byteNumber + 1];
            SetBigIndianValue(value, bytes, byteNumber);

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                return act(Subject, reader);
        }

        private TResult SetValueInBinaryStreamAndRead<TResult>(Func<SegyReader, BinaryReader, TResult> act, int byteNumber, Int32 value)
        {
            // Arrange
            var bytes = new byte[byteNumber + 3];
            SetBigIndianValue(value, bytes, byteNumber);

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                return act(Subject, reader);
        }

        private static void SetBigIndianValue(Int16 value, byte[] bytes, int byteNumber)
        {
            var samplesBytes = BitConverter.GetBytes(value);
            bytes[byteNumber - 1] = samplesBytes[1];
            bytes[byteNumber] = samplesBytes[0];
        }

        private static void SetBigIndianValue(Int32 value, byte[] bytes, int byteNumber)
        {
            var samplesBytes = BitConverter.GetBytes(value);
            bytes[byteNumber - 1] = samplesBytes[3];
            bytes[byteNumber + 0] = samplesBytes[2];
            bytes[byteNumber + 1] = samplesBytes[1];
            bytes[byteNumber + 2] = samplesBytes[0];
        }

        // TODO: Move AssertBytesConsumed to TDD lib
        public static void AssertBytesConsumed(Action<BinaryReader> act, int expectedNumberOfBytes)
        {
            var bytes = new byte[2 * expectedNumberOfBytes];
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                // Act
                act(reader);

                // Assert
                Assert.AreEqual(expectedNumberOfBytes, stream.Position, "Wrong number of bytes were consumed.");
            }
        }
        #endregion
    }
}
