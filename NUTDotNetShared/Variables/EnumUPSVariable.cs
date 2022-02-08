using System.Collections.Generic;

namespace NUTDotNetShared.Variables
{
    /// <summary>
    /// An ENUM variable type from a UPS. An ENUM is a <see cref="SimpleUPSVariable{T}"/> that has one or more defined states or values.
    /// </summary>
    class EnumUPSVariable : SimpleUPSVariable<string>
    {
        public List<string> Enums;

        public EnumUPSVariable(string name, string description = NUTCommon.NULL_TEXT, VarFlags flags = VarFlags.None)
            : base(name, description, flags)
        {
            Enums = new List<string>();
        }
    }
}
