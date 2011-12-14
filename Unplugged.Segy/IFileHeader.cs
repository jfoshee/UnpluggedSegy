
namespace Unplugged.Segy
{
    public interface IFileHeader
    {
        string Text { get; }
        FormatCode SampleFormat { get; }
    }
}
