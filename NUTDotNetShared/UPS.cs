using System.Collections.Generic;

namespace NUTDotNetShared
{
    class UPS
    {
        public readonly string Name;
        public readonly string Description;
        public Dictionary<string, string> Variables;

        public UPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
        }
    }
}
