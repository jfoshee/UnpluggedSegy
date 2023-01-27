using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unplugged.Segy
{
    public enum TimeBasis : Int16
    {
        LOCAL = 1,
        GMT = 2,
        OTHER =3,
        UTC = 4,
        GPS = 5
    }
}
