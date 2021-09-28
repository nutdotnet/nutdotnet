﻿using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NUTDotNetServer
{
    public class ServerUPS : AbstractUPS
    {
        #region Properties

        public HashSet<UPSVariable> Variables
        {
            get => variables;
            set => variables = value;
        }

        public List<string> Clients
        {
            get => clients;
            set => clients = value;
        }

        public new Dictionary<string, string> InstantCommands
        {
            get => instantCommands;
            set => value = instantCommands;
        }

        #endregion

        public ServerUPS(string name, string description = null) : base(name, description)
        {
            // InstantCommands = new Dictionary<string, string>();
        }

        public override UPSVariable GetVariable(string varName)
        {
            return GetVariableByName(varName);
        }

        #region String methods

        public string EnumerationToString(string enumName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (UPSVariable enumVar in GetListOfVariables(VarList.Enumerations, enumName))
            {
                foreach (string item in enumVar.Enumerations)
                {
                    sb.AppendFormat("ENUM {0} {1} \"{2}\"{3}", Name, enumName, item, NUTCommon.NewLine);
                }
            }

            return sb.ToString();
        }

        public string RangeToString(string rangeName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (UPSVariable rangeVar in GetListOfVariables(VarList.Ranges, rangeName))
            {
                foreach (Tuple<int, int> range in rangeVar.Ranges)
                {
                    sb.AppendFormat("RANGE {0} {1} \"{2}\" \"{3}\"{4}", Name, rangeName, range.Item1, range.Item2,
                        NUTCommon.NewLine);
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
