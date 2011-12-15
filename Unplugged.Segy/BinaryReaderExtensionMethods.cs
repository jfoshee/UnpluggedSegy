using System;
using System.Collections;
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

        public static float ReadSingleIbm(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            // TODO: Should reverse bytes?
            return ConvertFromIbmToIeee(bytes);
        }

        //public static float ConvertFromIbmToIeee(byte[] bytes)
        //{
        //    if (0 == BitConverter.ToInt32(bytes, 0))
        //        return 0;

        //    var bits = new BitArray(bytes);
        //    var S = bits[0] ? -1 : 1;

        //    // Exponent = bits[1:7]
        //    int exponent = bytes[0];
        //    exponent = exponent >> 1;
        //    exponent = exponent << 1;
        //    // Remove +64 bias
        //    exponent -= 64;
        //    var ibmBase = 16;
        //    var E = Math.Pow(ibmBase, exponent);

        //    // Fraction = bits[8:31]
        //    var fraction0 = bytes[1] / Math.Pow(2, 8);
        //    var fraction1 = bytes[2] / Math.Pow(2, 16);
        //    var fraction2 = bytes[3] / Math.Pow(2, 24);
        //    var F = fraction0 + fraction1 + fraction2;

        //    return (float)(S * E * F);
        //}

        public static float ConvertFromIbmToIeee(byte[] bytes)
        {
            if (0 == BitConverter.ToInt32(bytes, 0))
                return 0;

            var dividend = Math.Pow(16, 6);
            int istic = bytes[0];
            int a = bytes[1];
            int b = bytes[2];
            int c = bytes[3];
            var sign = 1.0;
            if (istic >= 128)
            {
                sign = -1.0;
                istic = istic - 128;
            }
            //else:
            //    sign = 1.0
            var mant = (float)(a << 16) + (float)(b << 8) + (float)(c);
            return (float)(sign * (Math.Pow(16, (istic - 64)) * (mant / dividend)));
        }

        public static string ReadStringEbcdic(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, bytes);
            return unicode.GetString(unicodeBytes);
        }

        //[StructLayout(LayoutKind.Explicit)]
        //struct IbmFormat
        //{
        //    [FieldOffset(0)] public byte Sign;           // 1 - 1
        //    [FieldOffset(0)] public byte Exponent;       // 2 - 8
        //    [FieldOffset(1)] public int Fraction;        // 
        //}

        //unsafe static void r4ibmieee(float* num)
        //{

        //  union {
        //    float val;
        //    struct ibmfloat {
        //      unsigned int sign:1;
        //      unsigned int exp:7;
        //      unsigned int mant:24;
        //    } fields;
        //    long int ival;
        //  } ibm;
        //  union {
        //    float val;
        //    struct ieeefloat {
        //      unsigned int sign:1;
        //      unsigned int exp:8;
        //      unsigned int mant:23;
        //    } fields;
        //  } ieee;
        //  int exp;


        //  ibm.val = *num;
        //  if (ibm.ival == 0) return;  /* zero is zero in both computers. */
        //  ieee.val = (float) ibm.fields.mant;

        //  /* ieee.fields.exp - 24 + 4*(ibm.fields.exp - 64)   */

        //  exp = ieee.fields.exp + 4 * ibm.fields.exp - 280;
        ///*
        //   Use IEEE un-normalized numbers if exponent is too small.
        //   Add hidden bit, shift right.        
        //*/
        //  if (exp < 0) {
        //    ieee.fields.mant = (ieee.fields.mant + 0x800000l)>>(-exp);
        //    ieee.fields.exp = 0;
        //  }
        //  else if (exp > 254) {
        //    ieee.val = HUGE;
        //  }
        //  else ieee.fields.exp = exp;

        //  ieee.fields.sign = ibm.fields.sign;
        //  *num = ieee.val;
        //  return;
        //}
    }
}