using System.Collections.Generic;
using System.Drawing;

namespace Unplugged.Segy
{
    public class ImageWriter
    {
        public virtual void Write(ISegyFile segyFile, string path)
        {
            if (segyFile.Traces == null)
            {
                new Bitmap(1, 1).Save(path);
                return;
            }
            var width = segyFile.Traces.Count;
            var height = segyFile.Traces[0].Values.Count;
            var bitmap = new Bitmap(width, height);
            dynamic range = FindRange(segyFile.Traces);
            var valueRange = range.Max - range.Min;
            if (valueRange != 0)
                AssignPixelColors(segyFile.Traces, width, height, bitmap, range.Min, valueRange);
            bitmap.Save(path);
        }

        #region Behind the Scenes

        private static void AssignPixelColors(IList<ITrace> traces, int width, int height, Bitmap bitmap, float valueMin, float valueRange)
        {
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    var value = traces[i].Values[j];
                    var alpha = byte.MaxValue;
                    if (value == 0.0f) // Exactly zero is assumed to be a null sample
                        alpha = byte.MinValue;
                    int byteValue = (int)(byte.MaxValue * (value - valueMin) / valueRange);
                    var color = Color.FromArgb(alpha, byteValue, byteValue, byteValue);
                    bitmap.SetPixel(i, j, color);
                }
        }

        private object FindRange(IList<ITrace> traces)
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
