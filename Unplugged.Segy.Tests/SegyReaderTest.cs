using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        public void ShouldHaveDefaultOptions()
        {
            // Arrange
            ISegyOptions options = Subject.Options;

            // Assert
            Assert.IsInstanceOfType(options, typeof(SegyOptions));
        }

        [TestMethod]
        public void InlineNumberLocation()
        {
            // Arrange
            int inlineNumberLocation = Subject.InlineNumberLocation;
            int crosslineNumberLocation = Subject.CrosslineNumberLocation;

            // Assert
            Assert.AreEqual(189, inlineNumberLocation, "According to SEGY Rev 1, byte 189 - 192 in the trace header should be used for the in-line number");
            Assert.AreEqual(193, crosslineNumberLocation, "According to SEGY Rev 1, byte 193 - 196 in the trace header should be used for the cross-line number");
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
            var header = Subject.ReadTextHeader(_example);

            // Assert
            Console.WriteLine(header);
            StringAssert.StartsWith(header, "C 1");
            StringAssert.Contains(header, "CLIENT");
            StringAssert.EndsWith(header.TrimEnd(), "END EBCDIC");
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadTextHeaderGivenStream()
        {
            // Arrange
            using (Stream stream = File.OpenRead(_example))
            {
                // Act
                string header = Subject.ReadTextHeader(stream);

                // Arrange
                StringAssert.EndsWith(header.TrimEnd(), "END EBCDIC");
            }
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

        [TestMethod]
        public void ShouldDetectAsciiHeaderByFirstCharacter()
        {
            // Arrange
            var expected = "C 1 This is expected";
            var extra = new string('z', 80);
            File.WriteAllText(TestPath(), expected + extra, Encoding.ASCII);

            // Act
            var header = Subject.ReadTextHeader(TestPath());

            // Assert
            StringAssert.StartsWith(header, expected);
            Assert.AreEqual(2, SplitLines(header).Count(), "Should split lines for ascii header");
        }

        [TestMethod]
        public void ShouldObserveOptionsForTextHeader()
        {
            // Arrange
            Subject.Options = new SegyOptions { TextHeaderColumnCount = 3, TextHeaderRowCount = 7, IsEbcdic = false };
            var text = new string('e', 21) + "n";
            File.WriteAllText(TestPath(), text);

            // Act
            var result = Subject.ReadTextHeader(TestPath());

            // Assert
            StringAssert.StartsWith(result, "eee" + Environment.NewLine);
            CollectionAssert.DoesNotContain(result.ToCharArray(), 'n');
        }

        [TestMethod]
        public void ShouldObserveNewLinesOption()
        {
            // Arrange
            Subject.Options = new SegyOptions { TextHeaderInsertNewLines = false };
            var expected = new string('x', 3200);
            File.WriteAllBytes(TestPath(), ConvertToEbcdic(expected));

            // Act
            var actual = Subject.ReadTextHeader(TestPath());

            // Assert
            Assert.AreEqual(expected, actual);
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
            bool isLittleEndian = false;

            // Act
            IFileHeader result = Set16BitValueInBinaryStreamAndRead((sr, br) => sr.ReadBinaryHeader(br), 25, (Int16)expected, isLittleEndian);

            // Assert
            Assert.AreEqual(expected, result.SampleFormat);
            Assert.AreEqual(isLittleEndian, result.IsLittleEndian);
        }

        [TestMethod]
        public void ShouldDetectLittleEndiannessFromSampleFormat()
        {
            // Arrange
            var expected = FormatCode.TwosComplementInteger1;
            bool isLittleEndian = true;

            // Act
            IFileHeader result = Set16BitValueInBinaryStreamAndRead((sr, br) => sr.ReadBinaryHeader(br), 25, (Int16)expected, isLittleEndian);

            // Assert
            Assert.AreEqual(expected, result.SampleFormat);
            Assert.AreEqual(isLittleEndian, result.IsLittleEndian);
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
            using (var stream = File.OpenRead(_example))
            using (var reader = new BinaryReader(stream))
            {
                // Act
                IFileHeader fileHeader = Subject.ReadFileHeader(reader);

                // Assert
                Assert.AreEqual(Subject.ReadTextHeader(_example), fileHeader.Text);
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
        public void ShouldReadTraceNumberFromSpecifiedByteLocation()
        {
            // Arrange
            Int32 expectedValue = Int16.MaxValue + 100;
            Subject.CrosslineNumberLocation = 113;

            // Act
            ITraceHeader result = Set32BitValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), Subject.CrosslineNumberLocation, expectedValue);

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
            ITraceHeader result = Set32BitValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), Subject.InlineNumberLocation, expectedValue);

            // Assert
            Assert.AreEqual(expectedValue, result.InlineNumber);
        }

        [TestMethod]
        public void ShouldReadNumberOfSamplesFromByte115()
        {
            // Arrange
            Int16 expectedValue = 2345;

            // Act
            ITraceHeader result = Set16BitValueInBinaryStreamAndRead((sr, br) => sr.ReadTraceHeader(br), 115, expectedValue, false);

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
            using (var stream = File.OpenRead(_example))
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
            AssertBytesConsumed(r => result = Subject.ReadTrace(r, sampleFormat, sampleCount, false), expected);

            // Assert
            Assert.AreEqual(sampleCount, result.Count);
        }

        [TestMethod]
        public void ShouldReadSinglesForIeeeFormat()
        {
            var expected = new float[] { 10, 20, 30 };
            var bytes = BitConverter.GetBytes(10f).Concat(BitConverter.GetBytes(20f)).Concat(BitConverter.GetBytes(30f)).ToArray();
            VerifyReadsSamplesOfGivenFormat(FormatCode.IeeeFloatingPoint4, expected, bytes, false);
        }

        [TestMethod]
        public void ShouldReadSingleIbmForIbmFormat()
        {
            // TODO: This test would be clearer if I had code to convert TO IBM Float
            var bytes = BitConverter.GetBytes(96795456).Concat(BitConverter.GetBytes(2136281153)).Concat(BitConverter.GetBytes(1025579328)).ToArray();
            var expected = new float[] { 0.9834598f, 1.020873f, 0.09816343f };
            VerifyReadsSamplesOfGivenFormat(FormatCode.IbmFloatingPoint4, expected, bytes, false);
        }

        [TestMethod]
        public void ShouldReadTwosComplementInteger2BigEndian()
        {
            var expected = new float[] { -777, 888 };
            var bytes = BitConverter.GetBytes((Int16)(-777)).Reverse().Concat(BitConverter.GetBytes((Int16)888).Reverse()).ToArray();
            VerifyReadsSamplesOfGivenFormat(FormatCode.TwosComplementInteger2, expected, bytes, false);
        }

        [TestMethod]
        public void ShouldReadTwosComplementInteger2LittleEndian()
        {
            VerifyReadsSamplesOfGivenFormat(FormatCode.TwosComplementInteger2, new float[] { 513 }, new byte[] { 1, 2 }, true);
        }

        [TestMethod]
        public void ShouldReadTwosComplementInteger4BigEndian()
        {
            var expected = new float[] { -777, 888 };
            var bytes = BitConverter.GetBytes((Int32)(-777)).Reverse().Concat(BitConverter.GetBytes((Int32)888).Reverse()).ToArray();
            VerifyReadsSamplesOfGivenFormat(FormatCode.TwosComplementInteger4, expected, bytes, false);
        }

        [TestMethod]
        public void ShouldReadTwosComplementInteger4LittleEndian()
        {
            var expected = new float[] { -777, 888 };
            var bytes = BitConverter.GetBytes((Int32)(-777)).Concat(BitConverter.GetBytes((Int32)888)).ToArray();
            VerifyReadsSamplesOfGivenFormat(FormatCode.TwosComplementInteger4, expected, bytes, true);
        }

        [TestMethod]
        public void ShouldReadTwosComplementInteger1()
        {
            VerifyReadsSamplesOfGivenFormat(FormatCode.TwosComplementInteger1, new float[] { -126, -128, 88 }, new byte[] { 130, 128, 88}, true);
        }

        [TestMethod]
        public void ShouldHandleEndOfStreamBeforeEndOfTrace()
        {
            // Arrange
            var bytes = new byte[] { 1, 0, 2, 0 };
            var expected = new float[] { 1, 2, 0 };

            // Act
            VerifyReadsSamplesOfGivenFormat(FormatCode.TwosComplementInteger2, expected, bytes, true);
        }

        private void VerifyReadsSamplesOfGivenFormat(FormatCode sampleFormat, float[] expected, byte[] bytes, bool isLittleEndian)
        {
            // Arrange
            var sampleCount = expected.Length;
            IList<float> result = null;

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                result = Subject.ReadTrace(reader, sampleFormat, sampleCount, isLittleEndian);

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
                trace = Subject.ReadTrace(reader, sampleFormat, false);

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
            var path = _example;

            // Act
            ISegyFile segy = Subject.Read(path);

            // Assert
            VerifyExampleValuesAndTraceCount(segy);
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadStreamHeadersAndAllTraces()
        {
            // Arrange
            var path = _example;
            ISegyFile segy = null;

            // Act
            using (Stream stream = File.OpenRead(path))
                segy = Subject.Read(stream);

            // Assert
            VerifyExampleValuesAndTraceCount(segy);
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldBeAbleToReadSegyFromNetworkStream()
        {
            // Arrange
            var webclient = new WebClient();
            using (var stream = webclient.OpenRead("https://github.com/jfoshee/UnpluggedSegy/raw/c44f0f65c4c80671964531cf5d45170fcb585d15/Unplugged.Segy.Tests/Examples/lineE.sgy"))
            {
                // Act
                var segy = Subject.Read(stream);

                // Assert
                VerifyExampleValuesAndTraceCount(segy);
            }
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadNoMoreThanRequestedNumberOfTraces()
        {
            // Arrange
            int traceCount = 5;
            using (Stream stream = File.OpenRead(_example))
            {
                // Act
                ISegyFile segy = Subject.Read(stream, traceCount);

                // Assert
                VerifyExampleValues(segy);
                Assert.AreEqual(traceCount, segy.Traces.Count);
            }
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadAllTracesIfRequestedTraceCountIsGreater()
        {
            // Arrange
            using (Stream stream = File.OpenRead(_example))
            {
                // Act
                ISegyFile segy = Subject.Read(stream, 9999);

                // Assert
                VerifyExampleValuesAndTraceCount(segy);
            }
        }

        private const string _example = "lineE.sgy";

        private static void VerifyExampleValues(ISegyFile result)
        {
            StringAssert.StartsWith(result.Header.Text, "C 1");
            StringAssert.Contains(result.Header.Text, "C16 GEOPHONES");
            Assert.AreEqual(FormatCode.IbmFloatingPoint4, result.Header.SampleFormat);
            Assert.AreEqual(1001, result.Traces.First().Header.SampleCount);
            var traceValues = result.Traces.First().Values.ToList();
            Assert.AreEqual(1001, traceValues.Count);
            CollectionAssert.Contains(traceValues, 0f);
            CollectionAssert.Contains(traceValues, 0.9834598f);
            CollectionAssert.DoesNotContain(traceValues, float.PositiveInfinity);
        }

        private static void VerifyExampleValuesAndTraceCount(ISegyFile result)
        {
            VerifyExampleValues(result);
            var fileLength = new FileInfo(_example).Length;
            Assert.AreEqual((fileLength - 3200 - 400) / (240 + 1001 * 4), result.Traces.Count);
        }

        [TestMethod]
        public void AllReadMethodsShouldBeMockable()
        {
            // Act
            var readMethods = typeof(SegyReader).GetMethods().Where(m => m.Name == "Read");

            // Assert
            foreach (var method in readMethods)
                Assert.IsTrue(method.IsVirtual);
        }

        #endregion

        #region Reading Little Endian

        [TestMethod]
        public void ShouldReadLittleEndianTraceHeader()
        {
            // Arrange
            var bytes = new byte[240];
            bytes[114] = 1; // 1
            bytes[115] = 2; // 512
            Subject.InlineNumberLocation = 101;
            bytes[100] = 1; // 1
            bytes[103] = 2; // 33,554,432
            Subject.CrosslineNumberLocation = 201;
            bytes[200] = 2; // 2
            bytes[203] = 2; // 33,554,432
            ITraceHeader traceHeader = null;

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                traceHeader = Subject.ReadTraceHeader(reader, true);

            // Assert
            Assert.AreEqual(513, traceHeader.SampleCount);
            Assert.AreEqual(33554433, traceHeader.InlineNumber);
            Assert.AreEqual(33554434, traceHeader.CrosslineNumber);
            Assert.AreEqual(33554434, traceHeader.TraceNumber);
        }

        // TODO: LittleEndianIntegrationTest

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
            return header.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }

        private TResult Set16BitValueInBinaryStreamAndRead<TResult>(Func<SegyReader, BinaryReader, TResult> act, int byteNumber, Int16 value, bool isLittleEndian)
        {
            // Arrange
            var bytes = new byte[400];
            if (isLittleEndian)
                BitConverter.GetBytes(value).CopyTo(bytes, byteNumber - 1);
            else
                SetBigIndianValue(value, bytes, byteNumber);

            // Act
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
                return act(Subject, reader);
        }

        private TResult Set32BitValueInBinaryStreamAndRead<TResult>(Func<SegyReader, BinaryReader, TResult> act, int byteNumber, Int32 value)
        {
            // Arrange
            var bytes = new byte[400];
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
