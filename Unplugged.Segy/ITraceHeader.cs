using System;

namespace Unplugged.Segy
{
    public interface ITraceHeader
    {
        int SampleCount { get; }
        int TraceNumber { get; }
        int InlineNumber { get; }
        int CrosslineNumber { get; }
        //int SampleIntervalInMicroseconds { get; }

        double X { get; }
        double Y { get; }

        /// <summary>
        /// Timestamp. This can be either in local time or UTC time.
        /// </summary>
        DateTime Timestamp { get; }
    }
}
