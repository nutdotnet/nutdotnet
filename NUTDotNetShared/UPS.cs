using System.Collections.Generic;
using System.Text;

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
            Variables = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            return "UPS " + Name + " \"" + Description + "\"";
        }

        public string VariablesToString()
        {
            if (Variables.Count == 0)
                return NUTCommon.NewLine;

            StringBuilder sb = new StringBuilder(Variables.Count);
            foreach (KeyValuePair<string, string> variable in Variables)
            {
                sb.AppendFormat("VAR {0} {1} \"{2}\"{3}", Name, variable.Key, variable.Value, NUTCommon.NewLine);
            }
            return sb.ToString();
        }
    }
}
