using System.IO;
using System.Linq;
using System.Text;

namespace Unplugged.Segy
{
    public class SegyReader
    {
        public string ReadTextHeader(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                var bytes = reader.ReadBytes(40 * 80);
                var text = ConvertFromEbcdic(bytes);
                var result = new StringBuilder(text.Length + 40);
                for (int i = 0; i < 1 + text.Length / 80; i++)
                {
                    var line = new string(text.Skip(80 * i).Take(80).ToArray());
                    result.AppendLine(line);
                }
                return result.ToString();
            }
        }

        private static string ConvertFromEbcdic(byte[] header)
        {
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, header);
            var unicodeText = unicode.GetString(unicodeBytes);
            return unicodeText;
        }
    }
}
