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
    public class BaseVariable
    {
        public static readonly int MAX_VALUE_LENGTH = 256;

        public readonly string Name;
        protected string description;
        public List<string> Enumerations;
        // The second dimension of the array is a length of 2, representing a min and max of the range.
        public List<Tuple<int, int>> Ranges;

        private string varValue;
        protected VarFlags flags = VarFlags.None;

        #region Properties

        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }

        public virtual string Value
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
        public virtual VarFlags Flags
        {
            get
            {
                return flags;
            }
            set
            {
                if (value.HasFlag(VarFlags.String | VarFlags.Number))
                    throw new ArgumentException("A variable cannot be both a string and a number.");
                else
                    flags = value;
            }
        }

        #endregion

        public BaseVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name variable cannot be blank or null.");

            Name = name;
            // Flags = flags;
            Enumerations = new List<string>();
            Ranges = new List<Tuple<int, int>>();
        }

        #region Base methods

        public override bool Equals(object obj)
        {
            return Equals(obj as BaseVariable);
        }

        /// <summary>
        /// Two variables may still be considered identical if at least one of the descriptions is null or empty.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(BaseVariable obj)
        {
            return Name == obj.Name && 
                (Description == obj.Description || String.IsNullOrEmpty(Description) || String.IsNullOrEmpty(obj.Description)) && 
                Value == obj.Value && 
                Flags == obj.Flags;
        }

        /// <summary>
        /// Override hash code. Variable names must be unique, so we're only hashing that.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 53;
                hash *= 23 + Name.GetHashCode();
                return hash;
            }
        }

        #endregion
    }
}
