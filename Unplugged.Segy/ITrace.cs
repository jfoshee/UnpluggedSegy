using System.Collections.Generic;

namespace Unplugged.Segy
{
    public interface ITrace
    {
        ITraceHeader Header { get; }
        IList<float> Values { get; }
    }
}
