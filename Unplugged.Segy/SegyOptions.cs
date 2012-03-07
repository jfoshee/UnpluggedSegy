
namespace Unplugged.Segy
{
    public class SegyOptions : ISegyOptions
    {
        public bool? IsEbcdic { get; set; }
        //public bool? IsLittleEndian { get; set; }

        public int TextHeaderColumnCount { get; set; }
        public int TextHeaderRowCount { get; set; }
        public bool TextHeaderInsertNewLines { get; set; }

        //public int BinaryHeaderLength { get; set; }
        //public int BinaryHeaderLocationForSampleFormat { get; set; }

        //public int TraceHeaderLength { get; set; }
        //public int TraceHeaderLocationForInlineNumber { get; set; }
        //public int TraceHeaderLocationForCrosslineNumber { get; set; }
        //public int TraceHeaderLocationForSampleCount { get; set; }

        public SegyOptions()
        {
            IsEbcdic = true;
            //IsLittleEndian = false;
            TextHeaderColumnCount = 80;
            TextHeaderRowCount = 40;
            TextHeaderInsertNewLines = true;
            //BinaryHeaderLength = 400;
            //BinaryHeaderLocationForSampleFormat = 25;
            //TraceHeaderLength = 240;
            //TraceHeaderLocationForSampleCount = 115;
            //TraceHeaderLocationForInlineNumber = 189;
            //TraceHeaderLocationForCrosslineNumber = 193;
        }
    }
}
