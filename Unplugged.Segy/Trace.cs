using System.Collections.Generic;

namespace Unplugged.Segy
{
    class Trace : ITrace
    {
        public ITraceHeader Header { get; set; }
        public IList<float> Values { get; set; }
    }
}
