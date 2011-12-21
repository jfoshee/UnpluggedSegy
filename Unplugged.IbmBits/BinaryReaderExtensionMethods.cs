using System;
using System.IO;
using System.Linq;

namespace Unplugged.IbmBits
{
    public static class BinaryReaderExtensionMethods
    {
        public static string ReadStringEbcdic(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            return IbmConverter.ToString(bytes);
        }

        public static Int16 ReadInt16BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2).Reverse().ToArray();
            return BitConverter.ToInt16(bytes, 0);
        }

        public static Int32 ReadInt32BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4).Reverse().ToArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        public static float ReadSingleIbm(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return IbmConverter.ToSingle(bytes);
        }
    }
}