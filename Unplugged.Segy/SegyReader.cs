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
        public int CrosslineNumberLocation { get; set; }

        public SegyReader()
        {
            InlineNumberLocation = 189;
            CrosslineNumberLocation = 193;
        }

        #region From the Top: Methods that start reading from the beginning of the file

        public virtual ISegyFile Read(string path)
        {
            using (var stream = File.OpenRead(path))
                return Read(stream);
        }

        public virtual ISegyFile Read(Stream stream)
        {
            return Read(stream, int.MaxValue);
        }

        public virtual ISegyFile Read(Stream stream, int traceCount)
        {
            using (var reader = new BinaryReader(stream))
            {
                var fileHeader = ReadFileHeader(reader);
                var traces = new List<ITrace>();
                for (int i = 0; i < traceCount; i++)
                {
                    var trace = ReadTrace(reader, fileHeader.SampleFormat);
                    if (trace == null)
                        break;
                    traces.Add(trace);
                }
                return new SegyFile { Header = fileHeader, Traces = traces };
            }
        }

        public virtual IFileHeader ReadFileHeader(BinaryReader reader)
        {
            var text = ReadTextHeader(reader);
            FileHeader header = ReadBinaryHeader(reader) as FileHeader;
            header.Text = text;
            return header;
        }

        public virtual string ReadTextHeader(string path)
        {
            using (var stream = File.OpenRead(path))
                return ReadTextHeader(stream);
        }

        public virtual string ReadTextHeader(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
                return ReadTextHeader(reader);
        }

        public virtual string ReadTextHeader(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(_textHeaderSize);
            string text = (bytes[0] == 'C') ?
                ASCIIEncoding.Default.GetString(bytes) :
                IbmConverter.ToString(bytes);
            return InsertNewLines(text);
        }

        #endregion

        #region Already in progress: Methods that start reading from the current location in the stream

        public virtual IFileHeader ReadBinaryHeader(BinaryReader reader)
        {
            var binaryHeader = reader.ReadBytes(_binaryHeaderSize);
            var byte0 = binaryHeader[_sampleFormatIndex];
            var byte1 = binaryHeader[_sampleFormatIndex + 1];
            bool isLittleEndian = (byte1 == 0);
            var sampleFormat = isLittleEndian ?
                    (FormatCode)byte0 :
                    (FormatCode)byte1;
            return new FileHeader { SampleFormat = sampleFormat, IsLittleEndian = isLittleEndian };
        }

        public virtual ITraceHeader ReadTraceHeader(BinaryReader reader)
        {
            var traceHeader = new TraceHeader();
            var headerBytes = reader.ReadBytes(_traceHeaderSize);
            if (headerBytes.Length < _traceHeaderSize)
                return null;
            if (headerBytes.Length >= CrosslineNumberLocation + 3)
                traceHeader.CrosslineNumber = traceHeader.TraceNumber = IbmBits.IbmConverter.ToInt32(headerBytes, CrosslineNumberLocation - 1);
            if (headerBytes.Length >= InlineNumberLocation + 3)
                traceHeader.InlineNumber = IbmConverter.ToInt32(headerBytes, InlineNumberLocation - 1);
            if (headerBytes.Length >= _sampleCountIndex + 2)
                traceHeader.SampleCount = IbmConverter.ToInt16(headerBytes, _sampleCountIndex);
            return traceHeader;
        }

        public virtual ITrace ReadTrace(BinaryReader reader, FormatCode sampleFormat)
        {
            var header = ReadTraceHeader(reader);
            if (header == null)
                return null;
            var values = ReadTrace(reader, sampleFormat, header.SampleCount);
            return new Trace { Header = header, Values = values };
        }

        public virtual IList<float> ReadTrace(BinaryReader reader, FormatCode sampleFormat, int sampleCount)
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
                    case FormatCode.TwosComplementInteger2:
                        trace[i] = reader.ReadInt16BigEndian();
                        break;
                    default:
                        throw new NotSupportedException("Unsupported sample format: " + sampleFormat.ToString());
                }
            }
            return trace;
        }

        #endregion

        #region Behind the Scenes

        private const int _textColumns = 80;
        private const int _textRows = 40;
        private const int _textHeaderSize = _textRows * _textColumns;
        private const int _binaryHeaderSize = 400;
        private const int _traceHeaderSize = 240;
        private const int _sampleFormatIndex = 24;
        private const int _sampleCountIndex = 114;

        private static string InsertNewLines(string text)
        {
            var result = new StringBuilder(text.Length + _textRows);
            for (int i = 0; i < 1 + text.Length / _textColumns; i++)
            {
                var line = new string(text.Skip(_textColumns * i).Take(_textColumns).ToArray());
                result.AppendLine(line);
            }
            return result.ToString();
        }

        #endregion
    }
}
