using System;
using System.IO;

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
            var bytes = reader.ReadBytes(2);
            return IbmConverter.ToInt16(bytes);
        }

        public static Int32 ReadInt32BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return IbmConverter.ToInt32(bytes);
        }

        public static float ReadSingleIbm(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return IbmConverter.ToSingle(bytes);
        }
    }
}