
namespace Unplugged.Segy
{
    class FileHeader : IFileHeader
    {
        public string Text { get; set; }
        public FormatCode SampleFormat { get; set; }
        public bool IsLittleEndian { get; set; }
        public MeasurementSystem MeasurementSystem { get; set;  }
    }
}
