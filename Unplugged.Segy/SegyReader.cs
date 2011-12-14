using System.IO;
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
                var header = reader.ReadBytes(40 * 80);
                var unicode = Encoding.Unicode;
                var ebcdic = Encoding.GetEncoding("IBM037");
                var headerText = Encoding.Convert(ebcdic, unicode, header);
                return unicode.GetString(headerText);
            }

        }
    }
}
