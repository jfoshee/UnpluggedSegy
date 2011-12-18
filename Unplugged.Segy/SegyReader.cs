using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Unplugged.Segy
{
    public class SegyReader
    {
        public virtual ISegyFile Read(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                var fileHeader = ReadFileHeader(reader);
                var traces = new List<ITrace>();
                while (stream.Position != stream.Length)
                {
                    var trace = ReadTrace(reader, fileHeader.SampleFormat);
                    traces.Add(trace);
                }
                return new SegyFile { Header = fileHeader, Traces = traces };
            }
        }

        public virtual string ReadTextHeader(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
                return ReadTextHeader(reader);
        }

        public static string ReadTextHeader(BinaryReader reader)
        {
            var text = reader.ReadStringEbcdic(40 * 80);
            return InsertNewLines(text);
        }

        public IFileHeader ReadBinaryHeader(BinaryReader reader)
        {
            reader.ReadBytes(24);
            var sampleFormat = (FormatCode)reader.ReadInt16BigEndian();
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
            reader.ReadBytes(13 - 1);
            var traceNumber = reader.ReadInt32BigEndian();
            reader.ReadBytes(17 - 13 - 4);
            int inlineNumber = 0;
            if (reader.BaseStream.Position != reader.BaseStream.Length)
                inlineNumber = reader.ReadInt32BigEndian();
            reader.ReadBytes(115 - 17 - 4);
            int sampleCount = 0;
            if (reader.BaseStream.Position != reader.BaseStream.Length)
                sampleCount = reader.ReadInt16BigEndian();
            reader.ReadBytes(241 - 115 - 2);
            return new TraceHeader { SampleCount = sampleCount, TraceNumber = traceNumber, CrosslineNumber = traceNumber, InlineNumber = inlineNumber };
        }

        public ITrace ReadTrace(BinaryReader reader, FormatCode sampleFormat)
        {
            var header = ReadTraceHeader(reader);
            var values = ReadTrace(reader, sampleFormat, header.SampleCount);
            return new Trace { Header = header, Values = values };
        }

        public IList<float> ReadTrace(BinaryReader reader, FormatCode sampleFormat, int sampleCount)
        {
            var trace = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                switch (sampleFormat)
                {
                    case FormatCode.IbmFloatingPoint4:
                        trace[i] = reader.ReadSingleIbm();
                        break;
                    case FormatCode.IeeeFloatingPoint4:
                        trace[i] = reader.ReadSingle();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            return trace;
        }

        #region Behind the Scenes

        private static string InsertNewLines(string text)
        {
            var result = new StringBuilder(text.Length + 40);
            for (int i = 0; i < 1 + text.Length / 80; i++)
            {
                var line = new string(text.Skip(80 * i).Take(80).ToArray());
                result.AppendLine(line);
            }
            return result.ToString();
        }

        #endregion
    }
}
