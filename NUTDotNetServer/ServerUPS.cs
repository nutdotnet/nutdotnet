using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NUTDotNetServer
{
    public class ServerUPS : AbstractUPS
    {
        #region Properties
        public List<string> Clients
        {
            get => clients;
            set => clients = value;
        }

        public List<string> Commands
        {
            get => commands;
            set => commands = value;
        }

        public Dictionary<string, string> Variables
        {
            get => variables;
            set => variables = value;
        }

        public Dictionary<string, string> Rewritables
        {
            get => rewritables;
            set => rewritables = value;
        }

        public Dictionary<string, List<string>> Enumerations
        {
            get => enumerations;
            set => enumerations = value;
        }
        #endregion

        public ServerUPS(string name, string description = null) : base(name, description)
        {

        }

        #region Specfic string methods

        public string EnumerationToString(string enumName)
        {
            if (Enumerations.Count == 0 || !Enumerations.ContainsKey(enumName))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (string item in Enumerations[enumName])
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

        #endregion
    }
}
