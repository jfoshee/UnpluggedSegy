using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Unplugged.Segy
{
    public class ImageWriter
    {
        public bool SetNullValuesToTransparent { get; set; }

        public ImageWriter()
        {
            SetNullValuesToTransparent = true;
        }

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

        public virtual void Write(IEnumerable<ITrace> traces, string path)
        {
            dynamic range = FindRange(traces);
            WriteBitmapForTraces(traces, path, range);
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
            var width = traces.Count();
            var height = traces.First().Values.Count;
            var valueRange = range.Max - range.Min;
            using (var bitmap = new Bitmap(width, height))
            {
                if (valueRange != 0)
                    AssignPixelColors(traces.ToList(), width, height, bitmap, range.Min, valueRange);
                bitmap.Save(path);
            }
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
