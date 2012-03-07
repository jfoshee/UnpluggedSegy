
namespace Unplugged.Segy
{
    public interface ISegyOptions
    {
        bool? IsEbcdic { get; }
        bool? IsLittleEndian { get; }

        int? TextHeaderColumnCount { get; }
        int? TextHeaderRowCount { get; }
        bool? TextHeaderInsertNewLines { get; }

        int? BinaryHeaderLength { get; }
        int? BinaryHeaderLocationForSampleFormat { get; }

        int? TraceHeaderLength { get; }
        int? TraceHeaderLocationForInlineNumber { get; }
        int? TraceHeaderLocationForCrosslineNumber { get; }
        int? TraceHeaderLocationForSampleCount { get; }
    }
}
