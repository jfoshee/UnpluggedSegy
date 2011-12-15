using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Unplugged.Segy
{
    public static class BinaryReaderExtensionMethods
    {
        public static string ReadStringEbcdic(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, bytes);
            return unicode.GetString(unicodeBytes);
        }

        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2).Reverse().ToArray();
            return BitConverter.ToInt16(bytes, 0);
        }

        public static float ReadSingleIbm(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return ConvertFromIbmToIeee(bytes);
        }

        public static float ConvertFromIbmToIeee(byte[] bytes)
        {
            if (0 == BitConverter.ToInt32(bytes, 0))
                return 0;

            // The first bit is the sign.  The next 7 bits are the exponent.
            int exponentBits = bytes[0];
            var sign = +1.0;
            // Remove sign from first bit
            if (exponentBits >= 128)
            {
                sign = -1.0;
                exponentBits -= 128;
            }
            // Remove the bias of 64 from the exponent
            exponentBits -= 64;
            var ibmBase = 16;
            var exponent = Math.Pow(ibmBase, exponentBits);

            // The fractional part is Big Endian unsigned int to the right of the radix point
            // So we reverse the bytes and pack them back into an int
            var fractionBytes = new byte[] { bytes[3], bytes[2], bytes[1], 0 };
            var mantissa = BitConverter.ToInt32(fractionBytes, 0);
            // And divide by 2^(8 * 3) to move the decimal all the way to the left
            var dividend = 16777216; // Math.Pow(2, 8 * 3);
            var fraction = mantissa / (float)dividend;

            return (float)(sign * exponent * fraction);
        }
    }
}