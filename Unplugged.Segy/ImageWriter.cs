using System.IO;
using System.Drawing;

namespace Unplugged.Segy
{
    public class ImageWriter
    {
        public void Write(ISegyFile segyFile, string path)
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Save(path);
        }
    }
}
