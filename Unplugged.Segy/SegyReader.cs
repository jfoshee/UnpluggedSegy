using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unplugged.IbmBits;

namespace Unplugged.Segy
{
    /// <summary>
    /// Responsible for reading SEGY files given a path or a Stream.
    /// </summary>
    public class SegyReader
    {
        public ISegyOptions Options { get; set; }
        public int InlineNumberLocation { get; set; }
        public int CrosslineNumberLocation { get; set; }

        public SegyReader()
        {
            Options = new SegyOptions();
            InlineNumberLocation = 189;
            CrosslineNumberLocation = 193;
        }

        #region From the Top: Methods that start reading from the beginning of the file

        /// <summary>
        /// Given a file path, reads entire SEGY file into memory
        /// </summary>
        public virtual ISegyFile Read(string path, IReadingProgress progress = null)
        {
            using (var stream = File.OpenRead(path))
                return Read(stream, progress);
        }

        /// <summary>
        /// Given stream, reads entire SEGY file into memory.
        /// Assumes the stream is at the start of the file.
        /// </summary>
        public virtual ISegyFile Read(Stream stream, IReadingProgress progress = null)
        {
            return Read(stream, int.MaxValue, progress);
        }

        /// <summary>
        /// Given stream and traceCount, reads the requested number
        /// of traces into memory. The given traceCount may exceed
        /// the number of traces in the file; 
        /// in that case all the traces in the file are read.
        /// Assumes the stream is at the start of the file.
        /// </summary>
        public virtual ISegyFile Read(Stream stream, int traceCount, IReadingProgress progress = null)
        {
            using (var reader = new BinaryReader(stream))
            {
                var fileHeader = ReadFileHeader(reader);
                var traces = new List<ITrace>();
                for (int i = 0; i < traceCount; i++)
                {
                    if (progress != null)
                    {
                        // TODO: Check if stream.Length breaks when streaming from web
                        int percentage = (int)(100 * stream.Position / stream.Length);
                        progress.ReportProgress(percentage);
                        if (progress.CancellationPending)
                            break;
                    }
                    var trace = ReadTrace(reader, fileHeader.SampleFormat, fileHeader.IsLittleEndian);
                    if (trace == null)
                        break;
                    traces.Add(trace);
                }
                return new SegyFile { Header = fileHeader, Traces = traces };
            }
        }

        /// <summary>
        /// Given a BinaryReader, reads the SEGY File Header into memory.
        /// Asummes the BinaryReader is at the start of the file.
        /// </summary>
        public virtual IFileHeader ReadFileHeader(BinaryReader reader)
        {
            var text = ReadTextHeader(reader);
            FileHeader header = ReadBinaryHeader(reader) as FileHeader;
            header.Text = text;
            return header;
        }

        /// <summary>
        /// Given a file path reads the text header from the beginning
        /// of the SEGY file.
        /// </summary>
        public virtual string ReadTextHeader(string path)
        {
            using (var stream = File.OpenRead(path))
                return ReadTextHeader(stream);
        }

        /// <summary>
        /// Given a stream reads the text header.
        /// Assumes the stream is at the start of the file.
        /// </summary>
        public virtual string ReadTextHeader(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
                return ReadTextHeader(reader);
        }

        /// <summary>
        /// Given a BinaryReader reads the text header.
        /// Assumes the BinaryReader is at the start of the file.
        /// </summary>
        public virtual string ReadTextHeader(BinaryReader reader)
        {
            var textHeaderLength = Options.TextHeaderColumnCount * Options.TextHeaderRowCount;
            var bytes = reader.ReadBytes(textHeaderLength);
            string text = (bytes[0] == 'C') || Options.IsEbcdic == false ?
                ASCIIEncoding.Default.GetString(bytes) :
                IbmConverter.ToString(bytes);
            return Options.TextHeaderInsertNewLines ? InsertNewLines(text) : text;
        }

        #endregion

        #region Already in progress: Methods that start reading from the current location in the stream

        /// <summary>
        /// Given a BinaryReader, reads the binary header.
        /// Assumes that the binary header is the next item to be read.
        /// </summary>
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

        /// <summary>
        /// Given a BinaryReader, reads the trace header.
        /// Assumes that the trace header is the next item to be read.
        /// Assumes that the byte order is Big Endian.
        /// </summary>
        public virtual ITraceHeader ReadTraceHeader(BinaryReader reader)
        {
            return ReadTraceHeader(reader, false);
        }

        /// <summary>
        /// Given a BinaryReader, reads the trace header.
        /// Assumes that the trace header is the next item to be read.
        /// </summary>
        public virtual ITraceHeader ReadTraceHeader(BinaryReader reader, bool isLittleEndian)
        {
            var traceHeader = new TraceHeader();
            var headerBytes = reader.ReadBytes(_traceHeaderSize);
            if (headerBytes.Length < _traceHeaderSize)
                return null;
            if (headerBytes.Length >= CrosslineNumberLocation + 3)
                traceHeader.CrosslineNumber = traceHeader.TraceNumber =
                    ToInt32(headerBytes, CrosslineNumberLocation - 1, isLittleEndian);
            if (headerBytes.Length >= InlineNumberLocation + 3)
                traceHeader.InlineNumber = ToInt32(headerBytes, InlineNumberLocation - 1, isLittleEndian);
            if (headerBytes.Length >= _sampleCountIndex + 2)
                traceHeader.SampleCount = ToInt16(headerBytes, _sampleCountIndex, isLittleEndian);
            return traceHeader;
        }

        /// <summary>
        /// Reads the trace (header and sample values).
        /// Assumes that the trace header is the next item to be read.
        /// </summary>
        public virtual ITrace ReadTrace(BinaryReader reader, FormatCode sampleFormat, bool isLittleEndian)
        {
            var header = ReadTraceHeader(reader, isLittleEndian);
            if (header == null)
                return null;
            var values = ReadTrace(reader, sampleFormat, header.SampleCount, isLittleEndian);
            return new Trace { Header = header, Values = values };
        }

        /// <summary>
        /// Assuming the trace header has been read, reads the array of sample values
        /// </summary>
        public virtual IList<float> ReadTrace(BinaryReader reader, FormatCode sampleFormat, int sampleCount, bool isLittleEndian)
        {
            var trace = new float[sampleCount];
            try
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    switch (sampleFormat)
                    {
                        case FormatCode.IbmFloatingPoint4:
                            trace[i] = reader.ReadSingleIbm();
                            break;
                       case FormatCode.IeeeFloatingPoint4:
                            trace[i] = ReadSingle(reader, isLittleEndian);
                            break;
                        case FormatCode.TwosComplementInteger1:
                            trace[i] = ReadSignedByte(reader);
                            break;
                        case FormatCode.TwosComplementInteger2:
                            trace[i] = ReadInt16(reader, isLittleEndian);
                            break;
                        case FormatCode.TwosComplementInteger4:
                            trace[i] = ReadInt32(reader, isLittleEndian);
                            break;
                        default:
                            throw new NotSupportedException(
                                String.Format("Unsupported sample format: {0}. Send an email to dev@segy.net to request support for this format.", sampleFormat));
                    }
                }
            }
            catch (EndOfStreamException) { /* Encountered end of stream before end of trace. Leave remaining trace samples as zero */ }
            return trace;
        }

        #endregion

        #region Behind the Scenes

        private const int _binaryHeaderSize = 400;
        private const int _traceHeaderSize = 240;
        private const int _sampleFormatIndex = 24;
        private const int _sampleCountIndex = 114;

        private string InsertNewLines(string text)
        {
            var rows = Options.TextHeaderRowCount;
            var cols = Options.TextHeaderColumnCount;
            var result = new StringBuilder(text.Length + rows);
            for (int i = 0; i < 1 + text.Length / cols; i++)
            {
                var line = new string(text.Skip(cols * i).Take(cols).ToArray());
                result.AppendLine(line);
            }
            return result.ToString();
        }

        private static int ToInt16(byte[] bytes, int index, bool isLittleEndian)
        {
            return isLittleEndian ?
                BitConverter.ToInt16(bytes, index) :
                IbmConverter.ToInt16(bytes, index);
        }

        private static int ToInt32(byte[] bytes, int index, bool isLittleEndian)
        {
            return isLittleEndian ?
                BitConverter.ToInt32(bytes, index) :
                IbmConverter.ToInt32(bytes, index);
        }

        private static float ReadSignedByte(BinaryReader reader)
        {
            byte b = reader.ReadByte();
            return b < 128 ? b : b - 256;
        }

        private static float ReadSingle(BinaryReader reader, bool isLittleEndian)
            => isLittleEndian ? reader.ReadSingle() : ReadReversedSingle(reader);

        private static Int32 ReadInt32(BinaryReader reader, bool isLittleEndian)
            => isLittleEndian ? reader.ReadInt32() : reader.ReadInt32BigEndian();

        private static Int16 ReadInt16(BinaryReader reader, bool isLittleEndian)
            => isLittleEndian ? reader.ReadInt16() : reader.ReadInt16BigEndian();

        private static float ReadReversedSingle(BinaryReader reader)
        {
            var b = reader.ReadBytes(4);
            byte tmp;
            tmp = b[0];
            b[0] = b[3];
            b[3] = tmp;
            tmp = b[1];
            b[1] = b[2];
            b[2] = tmp;
            return BitConverter.ToSingle(b, 0);
        }

        #endregion
    }
}
