using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
#if !MONO_TOUCH
using System.Drawing.Imaging;
#endif

namespace Unplugged.Segy
{
    /// <summary>
    /// Responsible for converting SEGY data to a bitmap image
    /// </summary>
    public class ImageWriter
    {
        /// <summary>
        /// When true, sample values that are exactly 0.0f will have their alpha component set to 0.
        /// Defaults to true.
        /// </summary>
        public bool SetNullValuesToTransparent { get; set; }

        public ImageWriter()
        {
            SetNullValuesToTransparent = false;
        }

#if !MONO_TOUCH

        /// <summary>
        /// Writes one or more bitmap for the given SEGY file. 
        /// If the SEGY has multiple inline numbers, it is assumed to be 3D, and one bitmap is written for each inline.
        /// </summary>
        /// <param name="path">
        /// The destination path for the image. 
        /// If multiple images are written, each inline number is added parenthetically to the file name.
        /// </param>
        public virtual void Write(ISegyFile segyFile, string path)
        {
            if (segyFile.Traces == null)
            {
                using (var bitmap = new Bitmap(1, 1))
                    bitmap.Save(path);
                return;
            }
            var inlineNumbers = GetInlineNumbers(segyFile);
            if (inlineNumbers.Count() == 1)
                Write(segyFile.Traces, path);
            else
            {
                dynamic range = FindRange(segyFile.Traces);
                WriteBitmapPerInline(segyFile, path, inlineNumbers, range);
            }
        }

        /// <summary>
        /// Writes a bitmap image with the given traces to the given destination path.
        /// </summary>
        public virtual void Write(IEnumerable<ITrace> traces, string path)
        {
            using (var bitmap = GetBitmap(traces))
                bitmap.Save(path);
        }

        /// <summary>
        /// Returns a single bitmap image composed from all the trace in the SEGY file.
        /// The caller is responsible for disposing of the bitmap. This is highy recommended
        /// so that GDI resources will be cleaned up.
        /// </summary>
        /// <returns>A disposable bitmap</returns>
        public virtual Bitmap GetBitmap(ISegyFile segyFile)
        {
            return GetBitmap(segyFile.Traces);
        }

        /// <summary>
        /// Returns a single bitmap image composed from the given traces.
        /// The caller is responsible for disposing of the bitmap. This is highy recommended
        /// so that GDI resources will be cleaned up.
        /// </summary>
        /// <returns>A disposable bitmap</returns>
        public virtual Bitmap GetBitmap(IEnumerable<ITrace> traces)
        {
            dynamic range = FindRange(traces);
            return GetBitmap(traces, range);
        }
#endif

        /// <summary>
        /// Returns a bitmap as a one-dimensional byte array. Pixels are layed out as R, G, B, A with one byte per channel.
        /// </summary>
        public virtual byte[] GetRaw32BppRgba(IEnumerable<ITrace> traces)
        {
            return GetRaw(traces, 4);
        }

        /// <summary>
        /// Returns a bitmap as a one-dimensional byte array. The pixel format is 1 unsigned byte per pixel. 
        /// Thus, the seismic data is quantized to the range 0...255.
        /// </summary>
        public virtual byte[] GetRaw8Bpp(IEnumerable<ITrace> traces)
        {
            return GetRaw(traces, 1);
        }

        #region Behind the Scenes

        private class ValueRange
        {
            public float Min { get; set; }
            public float Max { get; set; }
            public float Delta { get; set; }
        }

        private class RawBitmap
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public byte[] Bytes { get; set; }
        }

        // O(N)
        private static ValueRange FindRange(IEnumerable<ITrace> traces)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var trace in traces)
                foreach (var value in trace.Values)
                {
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            return new ValueRange { Min = min, Max = max, Delta = max - min };
        }

        private byte[] GetRaw(IEnumerable<ITrace> traces, int components)
        {
            var range = FindRange(traces);
            return GetRawBytesAndSize(traces, range, components).Bytes;
        }

        // O(N)
        private RawBitmap GetRawBytesAndSize(IEnumerable<ITrace> traces, ValueRange range, int components)
        {
            var traceList = traces.ToList();
            int width = traceList.Count;
            if (width == 0)
                return new RawBitmap { Bytes = new byte[] { }, Width = 0, Height = 0 };
            int height = traceList.First().Values.Count;
            var length = components * width * height;
            var bytes = new byte[length];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    var index = components * (j * width + i);
                    SetColor(bytes, index, range.Min, range.Delta, traceList[i].Values[j], components);
                }
            return new RawBitmap { Bytes = bytes, Width = width, Height = height };
        }

#if !MONO_TOUCH

        private void WriteBitmapPerInline(ISegyFile segyFile, string path, IEnumerable<int> inlineNumbers, ValueRange range)
        {
            foreach (var inline in inlineNumbers)
            {
                var traces = segyFile.Traces.Where(t => t.Header.InlineNumber == inline);
                var filename = Path.GetFileNameWithoutExtension(path);
                var extenstion = Path.GetExtension(path);
                var newPath = filename + " (" + inline + ")" + extenstion;
                WriteBitmapForTraces(traces, newPath, range);
            }
        }

        private void WriteBitmapForTraces(IEnumerable<ITrace> traces, string path, ValueRange range)
        {
            using (var bitmap = GetBitmap(traces, range))
                bitmap.Save(path);
        }

        private Bitmap GetBitmap(IEnumerable<ITrace> traces, dynamic range)
        {
            dynamic raw = GetRawBytesAndSize(traces, range, 4);
            return GetBitmap(raw.Bytes, raw.Width, raw.Height);
        }

        private static Bitmap GetBitmap(byte[] bytes, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(Rectangle.FromLTRB(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

#endif

        private void SetColor(byte[] bytes, int offset, float valueMin, float valueRange, float value, int components)
        {
            var alpha = byte.MaxValue;
            if (SetNullValuesToTransparent && value == 0.0f) // Exactly zero is assumed to be a null sample
                alpha = byte.MinValue;
            var byteValue = (byte)(byte.MaxValue * (value - valueMin) / valueRange);
            if (components == 1)
                bytes[offset + 0] = byteValue;
            else if (components == 4)
            {
                bytes[offset + 0] = byteValue;
                bytes[offset + 1] = byteValue;
                bytes[offset + 2] = byteValue;
                bytes[offset + 3] = alpha;
            }
            else
                throw new ArgumentException("Unsupported option", "components");
        }

        private static IEnumerable<int> GetInlineNumbers(ISegyFile segyFile)
        {
            return segyFile.Traces.Select(t =>
            {
                if (t.Header == null)
                    return 0;
                return t.Header.InlineNumber;
            }
            ).Distinct();
        }

        #endregion
    }
}
