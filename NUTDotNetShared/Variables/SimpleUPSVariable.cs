using System;
using System.Collections.Generic;

namespace NUTDotNetShared.Variables
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
        Immutable = 8 //Seems to only block server from changing flags later, not much use to a client.
    }

    /// <summary>
    /// Implements a basic state, or variable from a UPS. A state's value is limited to 256 characters (although the
    /// UPS C code says this could be made dynamic), and may be a number or string (check <see cref="VarFlags"/>.)
    /// Refer to https://github.com/networkupstools/nut/blob/master/include/extstate.h
    /// </summary>
    public class SimpleUPSVariable<T> : IEquatable<SimpleUPSVariable<T>> where T: IConvertible
    {
        public static readonly int MAX_VALUE_LENGTH = 256;

        private VarFlags flags;
        private T varValue;

        public string Name { get; set; }
        public string Description { get; set; }

        public T Value {
            get
            {
                return varValue;
            }

            set
            {
                if (value.ToString().Length > MAX_VALUE_LENGTH)
                    throw new ArgumentOutOfRangeException("Value",
                        "Value exceeds the maximum allowed length: " + MAX_VALUE_LENGTH);

                varValue = value;
            }
        }

        public VarFlags Flags
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

        public SimpleUPSVariable(string name, string description = NUTCommon.NULL_TEXT, VarFlags flags = VarFlags.None)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name variable cannot be blank or null.");

            Name = name;
            Description = description;
            Flags = flags;
        }

        #region Base methods

        public override bool Equals(object obj)
        {
            return Equals(obj as SimpleUPSVariable<T>);
        }

        /// <summary>
        /// Two variables may still be considered identical if at least one of the descriptions is null or empty.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(SimpleUPSVariable<T> obj)
        {
            return Name == obj.Name &&
                (Description == obj.Description || String.IsNullOrEmpty(Description) || String.IsNullOrEmpty(obj.Description)) &&
                Equals(Value, obj.Value) &&
                Flags == obj.Flags;
        }

        /// <summary>
        /// Override hash code. Variable names must be unique, so we're only hashing that.
        /// Quote from .NET Docs: "You should not assume that equal hash codes imply object equality."
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
