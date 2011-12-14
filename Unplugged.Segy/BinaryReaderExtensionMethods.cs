using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Unplugged.Segy
{
    public static class BinaryReaderExtensionMethods
    {
        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2).Reverse().ToArray();
            return BitConverter.ToInt16(bytes, 0);
        }

        public static string ReadStringEbcdic(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, bytes);
            return unicode.GetString(unicodeBytes);
        }
    }
}
