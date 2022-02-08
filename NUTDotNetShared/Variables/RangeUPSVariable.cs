using System;
using System.Collections.Generic;

namespace NUTDotNetShared.Variables
{
    /// <summary>
    /// A RANGE variable type from a UPS.
    /// A RANGE is a <see cref="SimpleUPSVariable{T}"/> that has one or more defined minimums and maximums.
    /// </summary>
    class RangeUPSVariable : SimpleUPSVariable<int>
    {
        public List<Tuple<int, int>> Ranges;

        public RangeUPSVariable(string name, string description = NUTCommon.NULL_TEXT, VarFlags flags = VarFlags.None)
            : base(name, description, flags)
        {
            Ranges = new List<Tuple<int, int>>();
        }
    }
}
