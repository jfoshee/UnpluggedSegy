using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            var measurement_system = (MeasurementSystem)ToInt16(binaryHeader, 3254 - 3200, isLittleEndian);
            return new FileHeader {
                SampleFormat = sampleFormat,
                IsLittleEndian = isLittleEndian,
                MeasurementSystem = measurement_system
            };
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

        static DateTime DateTimeOfYearAndDay(int year, int dayOfYear, int hour, int minute, int second, DateTimeKind kind, int extraSeconds)
        {
            var t_jan_1 = new DateTime(year, 1, 1, hour, minute, second, kind);
            var r = t_jan_1.AddSeconds((dayOfYear - 1) * (24 * 3600));
            return r;
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
                    Int32AtOffset(CrosslineNumberLocation - 1);
            if (headerBytes.Length >= InlineNumberLocation + 3)
                traceHeader.InlineNumber = Int32AtOffset(InlineNumberLocation - 1);
            if (headerBytes.Length >= _sampleCountIndex + 2)
                traceHeader.SampleCount = Int16AtOffset(_sampleCountIndex);

            // X,Y
            // TODO: This is valid only when the coordinate units in bytes 88,89 have been set to 1 (length in meters or feet).
            int ifactor = Int16AtOffset(70);
            int ix = Int32AtOffset(72);
            int iy = Int32AtOffset(76);
            if (ifactor>=0)
            {
                traceHeader.X = ix * ifactor;
                traceHeader.Y = iy * ifactor;
            }
            else
            {
                traceHeader.X = ((double)ix) / (-ifactor);
                traceHeader.Y = ((double)iy) / (-ifactor);
            }

            // Timestamp
            var year = Int16AtOffset(156);
            var day_of_year = Int16AtOffset(158); // starting from 1.
            var hour = Int16AtOffset(160);
            var minute = Int16AtOffset(162);
            var second = Int16AtOffset(164);
            var time_basis = (TimeBasis)Int16AtOffset(166);
            switch (time_basis)
            {
                case TimeBasis.LOCAL:
                    traceHeader.Timestamp = DateTimeOfYearAndDay(year, day_of_year, hour, minute, second, DateTimeKind.Local, 0);
                    break;
                case TimeBasis.GMT:
                    traceHeader.Timestamp = DateTimeOfYearAndDay(year, day_of_year, hour, minute, second, DateTimeKind.Utc, 0);
                    break;
                case TimeBasis.OTHER:
                    break;
                case TimeBasis.UTC:
                    traceHeader.Timestamp = DateTimeOfYearAndDay(year, day_of_year, hour, minute, second, DateTimeKind.Utc, 0);
                    break;
                case TimeBasis.GPS:
                    traceHeader.Timestamp = DateTimeOfYearAndDay(year, day_of_year, hour, minute, second, DateTimeKind.Utc, -18); // as of 2022, the GPS time is 18 seconds ahead of UTC.
                    break;
            }

            // Speed
            // Course

            return traceHeader;

            Int16 Int16AtOffset(int index)
                => SegyReader.ToInt16(headerBytes, index, isLittleEndian);

            Int32 Int32AtOffset(int index)
                => SegyReader.ToInt32(headerBytes, index, isLittleEndian);
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
                // Reading into single big buffer first and converting later is about 2x faster than reading every value individually using BinaryReader.
                byte[] bytes;
                switch (sampleFormat)
                {
                    case FormatCode.IbmFloatingPoint4:
                        for (int i = 0; i < sampleCount; i++)
                        {
                            trace[i] = reader.ReadSingleIbm();
                        }
                        break;
                    case FormatCode.IeeeFloatingPoint4:
                        bytes = ReadIntoBuffer(reader, sampleCount * 4);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            trace[i] = ToSingle(bytes, i*4, isLittleEndian);
                        }
                        break;
                    case FormatCode.TwosComplementInteger1:
                        bytes = ReadIntoBuffer(reader, sampleCount);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            var b = bytes[i];
                            trace[i] = b < 128 ? b : b - 256;
                        }
                        break;
                    case FormatCode.TwosComplementInteger2:
                        bytes = ReadIntoBuffer(reader, sampleCount * 2);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            trace[i] = ToInt16(bytes, i*2, isLittleEndian);
                        }
                        break;
                    case FormatCode.TwosComplementInteger4:
                        bytes = ReadIntoBuffer(reader, sampleCount * 4);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            trace[i] = ToInt32(bytes, i*4, isLittleEndian);
                        }
                        break;
                    default:
                        throw new NotSupportedException(
                            String.Format("Unsupported sample format: {0}. Send an email to dev@segy.net to request support for this format.", sampleFormat));
                }
            }
            catch (EndOfStreamException) { /* Encountered end of stream before end of trace. Leave remaining trace samples as zero */ }
            return trace;
        }

        #endregion

        #region Behind the Scenes
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatConvert
        {
            [FieldOffset(0)] public Int32 I;
            [FieldOffset(0)] public Single F;
        }
        private const int _binaryHeaderSize = 400;
        private const int _traceHeaderSize = 240;
        private const int _sampleFormatIndex = 24;
        private const int _sampleCountIndex = 114;
        private byte[] _ReadBuffer = null;

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

        private static Int16 ToInt16(byte[] bytes, int index, bool isLittleEndian)
        {
            return isLittleEndian ?
                BitConverter.ToInt16(bytes, index) :
                IbmConverter.ToInt16(bytes, index);
        }

        private static Int32 ToInt32(byte[] bytes, int index, bool isLittleEndian)
        {
            return isLittleEndian ?
                BitConverter.ToInt32(bytes, index) :
                IbmConverter.ToInt32(bytes, index);
        }

        private static float ToSingle(byte[] bytes, int index, bool isLittleEndian)
            => isLittleEndian ? BitConverter.ToSingle(bytes, index) : ToSingleReversed(bytes, index);

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

        private static float ToSingleReversed(byte[] bytes, int index)
        {
            return new FloatConvert { I = IbmConverter.ToInt32(bytes, index) }.F;
        }

        byte[] ReadIntoBuffer(BinaryReader reader, int length)
        {
            if (_ReadBuffer == null || _ReadBuffer.Length < length)
            {
                _ReadBuffer = new byte[length];
            }
            int this_round;
            for (int so_far=0; so_far<length; so_far += this_round)
            {
                this_round = reader.BaseStream.Read(_ReadBuffer, 0, length);
                if (this_round <= 0)
                {
                    throw new EndOfStreamException("End of input file");
                }
            }
            return _ReadBuffer;
        }
        #endregion
    }
}
