using System.Collections.Generic;

namespace Unplugged.Segy
{
    public interface ISegyFile
    {
        IFileHeader Header { get; }
        IList<ITrace> Traces { get; }
    }
}
