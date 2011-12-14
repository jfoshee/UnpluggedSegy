using System;
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
            AssertBytesConsumed((sr, br) => sr.ReadBinaryHeader(br), 400);
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
            AssertBytesConsumed((sr, br) => sr.ReadTraceHeader(br), 240);
        }

        [TestMethod]
        public void TryTraceHeader()
        {
            var path = @"C:\Users\jfoshee\Desktop\RMOTC Data\RMOTC Seismic data set\3D_Seismic\filt_mig.sgy";
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadBytes(3200);
                reader.ReadBytes(400);
                var header = reader.ReadBytes(240);

                var numSamples = GetInt16FromBytes(header, 115);
                Assert.AreEqual(1501, numSamples);
                var sampleInterval = GetInt16FromBytes(header, 117);
                Assert.AreEqual(2, sampleInterval / 1000);
                var inline = GetInt32FromBytes(header, 17);
                Assert.AreEqual(1, inline);
                var crossline = GetInt32FromBytes(header, 13);
                Assert.AreEqual(1, crossline);
            }
        }

        private static int GetInt32FromBytes(byte[] header, int byteNumber)
        {
            var bytes = header.Skip(byteNumber - 1).Take(4).Reverse().ToArray();    // Reverse byte order big endian to little endian
            var inline = BitConverter.ToInt32(bytes, 0);
            return inline;
        }

        private static int GetInt16FromBytes(byte[] header, int byteNumber)
        {
            var bytes = header.Skip(byteNumber - 1).Take(2).Reverse().ToArray();    // Reverse byte order big endian to little endian
            var inline = BitConverter.ToInt16(bytes, 0);
            return inline;
        }

        #endregion

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
            var lines = header.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }

        private void AssertBytesConsumed(Action<SegyReader, BinaryReader> act, int expectedNumberOfBytes)
        {
            // Arrange
            byte expected = 12;
            var bytes = new byte[expectedNumberOfBytes + 1];
            bytes[expectedNumberOfBytes] = expected;
            File.WriteAllBytes(TestPath(), bytes);

            // Act
            using (var stream = File.OpenRead(TestPath()))
            using (var reader = new BinaryReader(stream))
            {
                act(Subject, reader);

                // Assert
                var actual = reader.ReadByte();
                if (expected != actual)
                    Assert.Fail("Not enough bytes were consumed. Expected: {0}", expectedNumberOfBytes);
            }
        }

        private TResult SetValueInBinaryStreamAndRead<TResult>(Func<SegyReader, BinaryReader, TResult> act, int byteNumber, int value)
        {
            // Arrange
            var samplesBytes = BitConverter.GetBytes(value);
            var bytes = new byte[byteNumber + 2];
            bytes[byteNumber - 1] = samplesBytes[1];
            bytes[byteNumber] = samplesBytes[0];
            File.WriteAllBytes(TestPath(), bytes);

            // Act
            TResult result;
            using (var stream = File.OpenRead(TestPath()))
            using (var reader = new BinaryReader(stream))
            {
                result = act(Subject, reader);
            }

            return result;
        }
    }
}
