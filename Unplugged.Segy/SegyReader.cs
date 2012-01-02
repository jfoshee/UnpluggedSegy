using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unplugged.IbmBits;

namespace Unplugged.Segy
{
    public class SegyReader
    {
        public int InlineNumberLocation { get; set; }

        public SegyReader()
        {
            InlineNumberLocation = 189;
        }

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
            var traceHeader = new TraceHeader();
            var headerBytes = reader.ReadBytes(240);
            if (headerBytes.Length >= 12 + 3)
            {
                var traceNumberBytes = headerBytes.Skip(12).ToArray();
                traceHeader.CrosslineNumber = traceHeader.TraceNumber = IbmBits.IbmConverter.ToInt32(traceNumberBytes);
            }
            if (headerBytes.Length >= InlineNumberLocation + 3)
            {
                var inlineNumberBytes = headerBytes.Skip(InlineNumberLocation - 1).ToArray();
                traceHeader.InlineNumber = IbmConverter.ToInt32(inlineNumberBytes);
            }
            if (headerBytes.Length >= 115 + 1)
            {
                var sampleCountBytes = headerBytes.Skip(114).ToArray();
                traceHeader.SampleCount = IbmConverter.ToInt16(sampleCountBytes);
            }
            return traceHeader;
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
