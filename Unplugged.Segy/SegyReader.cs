using System;
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
                return ReadTextHeader(reader);
        }

        private static string ReadTextHeader(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(40 * 80);
            var text = ConvertFromEbcdic(bytes);
            return InsertNewlines(text);
        }

        public IFileHeader ReadBinaryHeader(BinaryReader reader)
        {
            reader.ReadBytes(24);
            var sampleFormat = (FormatCode)ReadBigEndianInt16(reader);
            reader.ReadBytes(400 - 24 - 2);
            return new FileHeader { SampleFormat = sampleFormat };
        }

        public IFileHeader ReadFileHeader(BinaryReader reader)
        {
            var text = ReadTextHeader(reader);
            FileHeader header = ReadBinaryHeader(reader) as FileHeader;
            header.Text = text;
            return header;
        }

        public ITraceHeader ReadTraceHeader(BinaryReader reader)
        {
            reader.ReadBytes(114);
            var sampleCount = ReadBigEndianInt16(reader);
            reader.ReadBytes(240 - 114 - 2);
            return new TraceHeader { SampleCount = sampleCount };
        }

        private static string ConvertFromEbcdic(byte[] header)
        {
            var unicode = Encoding.Unicode;
            var ebcdic = Encoding.GetEncoding("IBM037");
            var unicodeBytes = Encoding.Convert(ebcdic, unicode, header);
            var unicodeText = unicode.GetString(unicodeBytes);
            return unicodeText;
        }

        private static string InsertNewlines(string text)
        {
            var result = new StringBuilder(text.Length + 40);
            for (int i = 0; i < 1 + text.Length / 80; i++)
            {
                var line = new string(text.Skip(80 * i).Take(80).ToArray());
                result.AppendLine(line);
            }
            return result.ToString();
        }

        private static short ReadBigEndianInt16(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2).Reverse().ToArray();
            return BitConverter.ToInt16(bytes, 0);
        }
    }
}
