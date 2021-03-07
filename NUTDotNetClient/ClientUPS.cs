using NUTDotNetShared;
using System;
using System.Collections.Generic;

namespace NUTDotNetClient
{
    /// <summary>
    /// Represents a UPS that is managed by the NUT server. This class provides additional functionality to acquire
    /// data from the server.
    /// </summary>
    public class ClientUPS : AbstractUPS
    {
        private readonly NUTClient client;

        public ClientUPS(NUTClient client, string name, string description) : base(name, description)
        {
            this.client = client;
        }

        /// <summary>
        /// Sends a LIST query, validates the response and breaks it down for further processing.
        /// </summary>
        /// <param name="subquery">The second portion of the LIST query, such as VAR.</param>
        /// <param name="parameter">The parameter required for RANGE and ENUM subqueries.</param>
        /// <returns></returns>
        private List<string[]> GetListResponse(string subquery, string parameter = null)
        {
            string query;
            if (parameter is null)
                query = string.Format("LIST {0} {1}", subquery, Name);
            else
                query = string.Format("LIST {0} {1} {2}", subquery, Name, parameter);

            List<string> response = client.SendQuery(query);
            if (!response[0].Equals("BEGIN " + query) ||
                    !response[response.Count - 1].Equals("END " + query))
                throw new Exception("Malformed header or footer in response from server.");
            List<string[]> returnList = new List<string[]>(response.Count - 2);
            for (int i = 1; i <= returnList.Capacity; i++)
            {
                // Strip out any double quotes.
                response[i] = response[i].Replace("\"", string.Empty);
                string[] splitStr = response[i].Split(' ');
                if (!splitStr[0].Equals(subquery) && !splitStr[1].Equals(Name) && (!(parameter is null) &&
                    splitStr[2].Equals(subquery)))
                    throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                returnList.Add(splitStr);
            }
            return returnList;
        }

        /// <summary>
        /// Sends an INSTCMD query to the NUT server for execution. Throws an error if unsuccessful.
        /// </summary>
        /// <param name="commandId">The index of the command found from calling GetCommands.</param>
        public void DoInstantCommand(int commandId)
        {
            if (commands.Count == 0)
                GetCommands();
            List<string> response = client.SendQuery(string.Format("INSTCMD {0} {1}", Name, commands[commandId]));
        }

        /// <summary>
        /// Gets the variables assigned to this UPS from the server, in a name-value format.
        /// </summary>
        /// <param name="forceUpdate">Download the list of variables from the server even if one is cached here.</param>
        /// <returns></returns>
        public Dictionary<string, string> GetVariables(bool forceUpdate = false)
        {
            if (forceUpdate || variables.Count == 0)
            {
                List<string[]> response = GetListResponse("VAR");
                variables = new Dictionary<string, string>(response.Count);
                response.ForEach(str => variables.Add(str[2], str[3]));
            }
            return variables;
        }

        public Dictionary<string, string> GetRewritables(bool forceUpdate = false)
        {
            if (forceUpdate || rewritables.Count == 0)
            {
                List<string[]> response = GetListResponse("RW");
                rewritables = new Dictionary<string, string>();
                response.ForEach(str => rewritables.Add(str[2], str[3]));
            }
            return rewritables;
        }

        public List<string> GetCommands(bool forceUpdate = false)
        {
            if (forceUpdate || commands.Count == 0)
            {
                List<string[]> response = GetListResponse("CMD");
                commands = new List<string>();
                response.ForEach(str => commands.Add(str[2]));
            }
            return commands;
        }

        public List<string> GetEnumerations(string enumName, bool forceUpdate = false)
        {
            if (forceUpdate || !enumerations.ContainsKey(enumName))
            {
                List<string[]> response = GetListResponse("ENUM", enumName);
                enumerations[enumName] = new List<string>(response.Count);
                response.ForEach(str => enumerations[enumName].Add(str[3]));
            }
            return enumerations[enumName];
        }

        public List<string[]> GetRanges(string rangeName, bool forceUpdate = false)
        {
            if (forceUpdate || !ranges.ContainsKey(rangeName))
            {
                List<string[]> response = GetListResponse("RANGE", rangeName);
                ranges[rangeName] = new List<string[]>(response.Count);
                response.ForEach(str => ranges[rangeName].Add(new string[] { str[3], str[4] }));
            }
            return ranges[rangeName];
        }

        public List<string> GetClients(bool forceUpdate = false)
        {
            if (forceUpdate || clients.Count == 0)
            {
                List<string[]> response = GetListResponse("CLIENT");
                clients = new List<string>();
                response.ForEach(str => clients.Add(str[2]));
            }
            return clients;
        }
    }
}
