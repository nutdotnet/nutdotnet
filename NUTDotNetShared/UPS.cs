using System.Collections.Generic;

namespace NUTDotNetShared
{
    public class UPS
    {
        public readonly string Name;
        public readonly string Description;
        public Dictionary<string, string> Variables;

        public UPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
        }

        public override string ToString()
        {
            return "UPS " + Name + " \"" + Description + "\"";
        }
    }
}
