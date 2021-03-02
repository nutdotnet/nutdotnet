using System.Collections.Generic;
using System.Text;

namespace NUTDotNetShared
{
    public class UPS
    {
        public readonly string Name;
        public readonly string Description;
        protected Dictionary<string, string> variables;
        protected Dictionary<string, string> rewritables;
        protected List<string> commands;
        protected Dictionary<string, List<string>> enumerations;
        protected readonly Dictionary<string, List<string[]>> ranges;
        protected List<string> clients;

        public UPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
            variables = new Dictionary<string, string>();
            rewritables = new Dictionary<string, string>();
            commands = new List<string>();
            enumerations = new Dictionary<string, List<string>>();
            ranges = new Dictionary<string, List<string[]>>();
            clients = new List<string>();
        }

        public Dictionary<string, string> Variables { get => variables; }
        public Dictionary<string, string> Rewritables { get => rewritables; }
        public List<string> Commands { get => commands; }
        public Dictionary<string, List<string>> Enumerations { get => enumerations; }
        public List<string> Clients { get => clients; }

        public void AddRange(string name, string[] values)
        {
            if (ranges.ContainsKey(name))
                ranges[name].Add(values);
            else
                ranges[name] = new List<string[]>() { values };
        }

        public override string ToString()
        {
            return "UPS " + Name + " \"" + Description + "\"";
        }

        /// <summary>
        /// Basic function that can put a common dictionary to string as the NUT protocol would expect it.
        /// </summary>
        /// <param name="nutName">The type of data a NUT protocol device would expect, such as VAR or RW</param>
        /// <param name="dictionary">The dictionary to be parsed.</param>
        /// <returns></returns>
        public string DictionaryToString(string nutName, Dictionary<string, string> dictionary)
        {
            if (dictionary.Count == 0)
                return NUTCommon.NewLine;

            StringBuilder sb = new StringBuilder(dictionary.Count);
            foreach (KeyValuePair<string, string> variable in dictionary)
            {
                sb.AppendFormat("{0} {1} {2} \"{3}\"{4}", nutName, Name, variable.Key, variable.Value, NUTCommon.NewLine);
            }
            return sb.ToString();
        }

        public string ListToString(string nutName, List<string> list)
        {
            if (list.Count == 0)
                return NUTCommon.NewLine;

            StringBuilder sb = new StringBuilder(list.Count);
            foreach (string item in list)
            {
                sb.AppendFormat("{0} {1} {2}{3}", nutName, Name, item, NUTCommon.NewLine);
            }
            return sb.ToString();
        }

        public string EnumerationToString(string enumName)
        {
            if (enumerations.Count == 0 || !enumerations.ContainsKey(enumName))
                return NUTCommon.NewLine;

            StringBuilder sb = new StringBuilder();
            foreach (string item in enumerations[enumName])
            {
                sb.AppendFormat("ENUM {0} {1} \"{2}\"{3}", Name, enumName, item, NUTCommon.NewLine);
            }
            return sb.ToString();
        }

        public string RangeToString(string rangeName)
        {
            if (ranges.Count == 0 || !ranges.ContainsKey(rangeName))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (string[] range in ranges[rangeName])
            {
                sb.AppendFormat("RANGE {0} {1} \"{2}\" \"{3}\"{4}", Name, rangeName, range[0], range[1],
                    NUTCommon.NewLine);
            }
            return sb.ToString();
        }
    }
}
