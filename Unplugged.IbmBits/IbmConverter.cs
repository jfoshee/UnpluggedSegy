using System.Text;

namespace Unplugged.IbmBits
{
    public class IbmConverter
    {
        public static string ToString(byte[] bytes)
        {
            return ToString(bytes, 0);
        }

        public static string ToString(byte[] bytes, int startingIndex)
        {
            return ToString(bytes, startingIndex, bytes.Length - startingIndex);
        }

        public static string ToString(byte[] bytes, int startingIndex, int length)
        {
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, bytes, startingIndex, length);
            return unicode.GetString(unicodeBytes);
        }
    }
}
