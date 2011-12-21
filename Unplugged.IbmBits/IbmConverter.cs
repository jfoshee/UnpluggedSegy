using System.Text;

namespace Unplugged.IbmBits
{
    public class IbmConverter
    {
        public static string ToString(byte[] bytes)
        {
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, bytes);
            return unicode.GetString(unicodeBytes);
        }
    }
}
