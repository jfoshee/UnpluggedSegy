using System;
using System.IO;

namespace Unplugged.IbmBits
{
    public static class BinaryReaderExtensionMethods
    {
        /// <summary>
        /// Reads the requested number of 8-bit EBCDIC encoded characters from the stream and converts them to a 
        /// Unicode string.
        /// </summary>
        /// <param name="length">The number of bytes to read</param>
        /// <returns>Unicode encoded string</returns>
        public static string ReadStringEbcdic(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            return IbmConverter.ToString(bytes);
        }

        /// <summary>
        /// Reads a 16-bit integer from the stream that has been encoded as big endian.
        /// </summary>
        public static Int16 ReadInt16BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            return IbmConverter.ToInt16(bytes);
        }

        /// <summary>
        /// Reads a 32-bit integer from the stream that has been encoded as big endian.
        /// </summary>
        public static Int32 ReadInt32BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return IbmConverter.ToInt32(bytes);
        }

        /// <summary>
        /// Reads a single precision 32-bit floating point number from the stream 
        /// that has been encoded in IBM System/360 Floating Point format
        /// </summary>
        /// <returns>IEEE formatted single precision floating point</returns>
        public static float ReadSingleIbm(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return IbmConverter.ToSingle(bytes);
        }
    }
}