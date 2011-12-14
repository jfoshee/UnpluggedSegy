
namespace Unplugged.Segy
{
    class FileHeader : IFileHeader
    {
        public string Text { get; set; }
        public FormatCode SampleFormat { get; set; }
    }
}
