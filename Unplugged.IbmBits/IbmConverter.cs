using System;
using System.Text;

namespace Unplugged.IbmBits
{
    public static class IbmConverter
    {
        /// <summary>
        /// Returns a Unicode string converted from a byte array of EBCDIC encoded characters
        /// </summary>
        public static string ToString(byte[] bytes)
        {
            return ToString(bytes, 0);
        }

        /// <summary>
        /// Returns a Unicode string converted from a byte array of EBCDIC encoded characters 
        /// starting at the specified position
        /// </summary>
        /// <param name="startingIndex">
        /// Zero-based starting index of starting position in bytes array
        /// </param>
        public static string ToString(byte[] bytes, int startingIndex)
        {
            return ToString(bytes, startingIndex, bytes.Length - startingIndex);
        }

        /// <summary>
        /// Returns a Unicode string converted from a byte array of EBCDIC encoded characters 
        /// starting at the specified position of the given length
        /// </summary>
        /// <param name="startingIndex">
        /// Zero-based starting index of starting position in bytes array
        /// </param>
        /// <param name="length">
        /// Number of characters to convert
        /// </param>
        public static string ToString(byte[] bytes, int startingIndex, int length)
        {
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, bytes, startingIndex, length);
            return unicode.GetString(unicodeBytes);
        }

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes encoding a big endian 16-bit signed integer
        /// </summary>
        public static Int16 ToInt16(byte[] bytes)
        {
            bytes = new byte[] { bytes[1], bytes[0] };
            return BitConverter.ToInt16(bytes, 0);
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes encoding a big endian 32-bit signed integer
        /// </summary>
        public static int ToInt32(byte[] bytes)
        {
            bytes = new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] };
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Returns a 32-bit IEEE single precision floating point number from four bytes encoding
        /// a single precision number in IBM System/360 Floating Point format
        /// </summary>
        public static float ToSingle(byte[] bytes)
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
            var mantissa = BitConverter.ToInt32(fractionBytes, 0);              // TODO: Test if mantissa should be converted with ToUint32
            // And divide by 2^(8 * 3) to move the decimal all the way to the left
            var dividend = 16777216; // Math.Pow(2, 8 * 3);
            var fraction = mantissa / (float)dividend;

            return (float)(sign * exponent * fraction);
        }
    }
}
