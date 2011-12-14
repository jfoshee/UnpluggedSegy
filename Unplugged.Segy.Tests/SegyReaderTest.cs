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
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var expected = "Once upon a time...";
            var unicodeBytes = unicode.GetBytes(expected);
            var ebcdicBytes = Encoding.Convert(unicode, ebcdic, unicodeBytes);
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
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var expected = "TheEnd";
            var unicodeBytes = unicode.GetBytes(new string('c', 3200 - 6) + expected + "notExpected");
            var ebcdicBytes = Encoding.Convert(unicode, ebcdic, unicodeBytes);
            var path = TestPath();
            File.WriteAllBytes(path, ebcdicBytes);

            // Act
            string header = Subject.ReadTextHeader(path);

            // Assert
            StringAssert.EndsWith(header, expected);
        }

        [TestMethod, DeploymentItem(@"Unplugged.Segy.Tests\Examples\lineE.sgy")]
        public void ShouldReadExampleTextHeader()
        {
            // Act
            var header = Subject.ReadTextHeader("lineE.sgy");

            // Assert
            StringAssert.StartsWith(header, "C 1");
            StringAssert.Contains(header, "CLIENT");
            StringAssert.EndsWith(header.TrimEnd(), "END EBCDIC");
        }

        // Should insert newlines at 80 chars

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
    }
}
