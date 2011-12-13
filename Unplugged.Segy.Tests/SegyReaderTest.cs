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
        public void TryTextHeader()
        {
            var path = @"C:\Users\jfoshee\Desktop\RMOTC Data\RMOTC Seismic data set\3D_Seismic\filt_mig.sgy";
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                var header = reader.ReadBytes(3200);
                Encoding ascii = Encoding.ASCII;
                Encoding ebcdic = Encoding.GetEncoding("IBM037");
                var headerAscii = Encoding.Convert(ebcdic, ascii, header);
                var s = ascii.GetString(headerAscii);
                Console.WriteLine(s);
            }
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
    }
}
