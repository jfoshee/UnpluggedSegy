
namespace Unplugged.Segy
{
    public interface ITraceHeader
    {
        int SampleCount { get; }
        int TraceNumber { get; }
        int InlineNumber { get; }
        int CrosslineNumber { get; }
        //int SampleIntervalInMicroseconds { get; }
        //int X { get; }
        //int Y { get; }
    }
}
