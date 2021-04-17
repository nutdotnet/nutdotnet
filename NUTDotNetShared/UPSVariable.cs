using System;
using System.Collections.Generic;

namespace NUTDotNetShared
{
    /// <summary>
    /// Specifies properties of a UPSVariable (state). Multiple flags can be given. String and number are mutually
    /// exclusive (not String implies Number).
    /// </summary>
    [Flags]
    public enum VarFlags
    {
        None = 0,
        RW = 1,
        String = 2,
        Number = 4,
        Immutable = 8
    }

    /// <summary>
    /// Implements a state, or variable from a UPS. A state's value is limited to 256 characters (although the
    /// UPS C code says this could be made dynamic). A state can have multiple Flags, Enumerations, Ranges,
    /// and Commands.
    /// Refer to https://github.com/networkupstools/nut/blob/master/include/extstate.h
    /// </summary>
    public class UPSVariable
    {
        private string varValue;
        private VarFlags flags;

        public static readonly int MAX_VALUE_LENGTH = 256;
        public readonly string Name;
        public string Value
        {
            get
            {
                return varValue;
            }
            set
            {
                if (value.Length > MAX_VALUE_LENGTH)
                    throw new ArgumentOutOfRangeException("Value", "Value cannot be longer than MAX_VALUE_LENGTH.");
                else
                    varValue = value;
            }
        }
        public VarFlags Flags
        {
            get
            {
                return flags;
            }
            private set
            {
                if (value.HasFlag(VarFlags.String | VarFlags.Number))
                    throw new ArgumentException("A variable cannot be both a string and a number.");
                else
                    flags = value;
            }
        }
        public List<string> Enumerations;
        // The second dimension of the array is a length of 2, representing a min and max of the range.
        public List<Tuple<int, int>> Ranges;

        public UPSVariable(string name, VarFlags flags)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name variable cannot be blank or null.");

            Name = name;
            Flags = flags;
            Enumerations = new List<string>();
            Ranges = new List<Tuple<int, int>>();
        }
    }
}
