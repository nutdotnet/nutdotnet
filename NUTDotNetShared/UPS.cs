using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUTDotNetShared
{
    public class UPS
    {
        public readonly string Name;
        public readonly string Description;
        public Dictionary<string, string> Variables;
        public Dictionary<string, string> Rewritables;
        public List<string> Commands;
        public Dictionary<string, string[]> Enumerations;
        public readonly Dictionary<string, List<string[]>> Ranges;

        public UPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
            Variables = new Dictionary<string, string>();
            Rewritables = new Dictionary<string, string>();
            Commands = new List<string>();
            Enumerations = new Dictionary<string, string[]>();
            Ranges = new Dictionary<string, List<string[]>>();
        }

        public void AddRange(string name, string[] values)
        {
            if (Ranges.ContainsKey(name))
                Ranges[name].Add(values);
            else
                Ranges[name] = new List<string[]>() { values };
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
            if (Enumerations.Count == 0 || !Enumerations.ContainsKey(enumName))
                return NUTCommon.NewLine;

            StringBuilder sb = new StringBuilder();
            foreach (string item in Enumerations[enumName])
            {
                sb.AppendFormat("ENUM {0} {1} \"{2}\"{3}", Name, enumName, item, NUTCommon.NewLine);
            }
            return sb.ToString();
        }

        public string RangeToString(string rangeName)
        {
            if (Ranges.Count == 0 || !Ranges.ContainsKey(rangeName))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (string[] range in Ranges[rangeName])
            {
                sb.AppendFormat("RANGE {0} {1} \"{2}\" \"{3}\"{4}", Name, rangeName, range[0], range[1],
                    NUTCommon.NewLine);
            }
            return sb.ToString();
        }
    }
}
