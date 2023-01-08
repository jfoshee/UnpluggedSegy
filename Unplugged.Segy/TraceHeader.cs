
namespace Unplugged.Segy
{
    class TraceHeader : ITraceHeader
    {
        public int SampleCount { get; set; }
        public int TraceNumber { get; set; }
        public int InlineNumber { get; set; }
        public int CrosslineNumber { get; set; }
        public double X { get; set;  }
        public double Y { get; set;  }
    }
}
