﻿using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

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
            SetNullValuesToTransparent = true;
        }

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

        #region Behind the Scenes

        private void WriteBitmapPerInline(ISegyFile segyFile, string path, IEnumerable<int> inlineNumbers, dynamic range)
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

        private void WriteBitmapForTraces(IEnumerable<ITrace> traces, string path, dynamic range)
        {
            using (var bitmap = GetBitmap(traces, range))
                bitmap.Save(path);
        }

        private Bitmap GetBitmap(IEnumerable<ITrace> traces, dynamic range)
        {
            var width = traces.Count();
            var height = traces.First().Values.Count;
            var valueRange = range.Max - range.Min;
            var bitmap = new Bitmap(width, height);
            if (valueRange != 0)
                AssignPixelColors(traces.ToList(), width, height, bitmap, range.Min, valueRange);
            return bitmap;
        }

        private void AssignPixelColors(IList<ITrace> traces, int width, int height, Bitmap bitmap, float valueMin, float valueRange)
        {
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    SetPixel(traces, bitmap, valueMin, valueRange, i, j);
        }

        private void SetPixel(IList<ITrace> traces, Bitmap bitmap, float valueMin, float valueRange, int i, int j)
        {
            var value = traces[i].Values[j];
            var alpha = byte.MaxValue;
            if (SetNullValuesToTransparent && value == 0.0f) // Exactly zero is assumed to be a null sample
                alpha = byte.MinValue;
            int byteValue = (int)(byte.MaxValue * (value - valueMin) / valueRange);
            var color = Color.FromArgb(alpha, byteValue, byteValue, byteValue);
            bitmap.SetPixel(i, j, color);
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

        private static object FindRange(IEnumerable<ITrace> traces)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var trace in traces)
                foreach (var value in trace.Values)
                {
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            return new { Min = min, Max = max };
        }

        #endregion
    }
}
