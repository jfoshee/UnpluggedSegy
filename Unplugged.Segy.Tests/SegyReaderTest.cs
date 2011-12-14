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

                var inline = GetIntFromBytes(header, 17);
                Assert.AreEqual(1, inline);
                var crossline = GetIntFromBytes(header, 13);
                Assert.AreEqual(1, crossline);
            }
        }

        private static int GetIntFromBytes(byte[] header, int byteNumber)
        {
            var bytes = header.Skip(byteNumber - 1).Take(4).Reverse().ToArray();    // Reverse byte order big endian to little endian
            var inline = BitConverter.ToInt32(bytes, 0);
            return inline;
        }

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
    }
}
