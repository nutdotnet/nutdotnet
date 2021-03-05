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
            string commonQuery = "LIST RW " + Name;
            if (forceUpdate || rewritables.Count == 0)
            {
                List<string> response = client.SendQuery(commonQuery);
                if (!response[0].Equals("BEGIN " + commonQuery) ||
                    !response[response.Count - 1].Equals("END " + commonQuery))
                    throw new Exception("Malformed header or footer in response from server.");
                rewritables = new Dictionary<string, string>();
                foreach (string str in response)
                {
                    string[] splitStr = str.Split(' ');
                    if (splitStr.Length != 4 || splitStr[0].Equals("RW") || splitStr[1].Equals(Name))
                        throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                    rewritables.Add(splitStr[3], splitStr[4]);
                }
            }
            return rewritables;
        }

        public List<string> GetCommands(bool forceUpdate = false)
        {
            string commonQuery = "LIST CMD " + Name;
            if (forceUpdate || commands.Count == 0)
            {
                List<string> response = client.SendQuery(commonQuery);
                if (!response[0].Equals("BEGIN " + commonQuery) ||
                    !response[response.Count - 1].Equals("END " + commonQuery))
                    throw new Exception("Malformed header or footer in response from server.");
                commands = new List<string>();
                foreach (string str in response)
                {
                    string[] splitStr = str.Split(' ');
                    if (splitStr.Length != 3 || splitStr[0].Equals("CMD") || splitStr[1].Equals(Name))
                        throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                    commands.Add(splitStr[2]);
                }
            }
            return commands;
        }

        public List<string> GetEnumerations(string enumName, bool forceUpdate = false)
        {
            string commonQuery = "LIST ENUM " + Name + " " + enumName;
            if (forceUpdate || !(enumerations[enumName] is null))
            {
                List<string> response = client.SendQuery(commonQuery);
                if (!response[0].Equals("BEGIN " + commonQuery) ||
                    !response[response.Count - 1].Equals("END " + commonQuery))
                    throw new Exception("Malformed header or footer in response from server.");
                foreach (string str in response)
                {
                    string[] splitStr = str.Split(' ');
                    if (splitStr.Length != 4 || splitStr[0].Equals("ENUM") || splitStr[1].Equals(Name) ||
                        splitStr[2].Equals(enumName))
                        throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                    enumerations[enumName].Add(splitStr[3]);
                }
            }
            return enumerations[enumName];
        }

        public List<string[]> GetRanges(string rangeName, bool forceUpdate = false)
        {
            string commonQuery = "LIST RANGE " + Name + " " + rangeName;
            if (forceUpdate)
            {
                List<string> response = client.SendQuery(commonQuery);
                if (!response[0].Equals("BEGIN " + commonQuery) ||
                    !response[response.Count - 1].Equals("END " + commonQuery))
                    throw new Exception("Malformed header or footer in response from server.");
                foreach (string str in response)
                {
                    string[] splitStr = str.Split(' ');
                    if (splitStr.Length != 5 || splitStr[0].Equals("RANGE") || splitStr[1].Equals(Name) ||
                        splitStr[2].Equals(rangeName))
                        throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                    ranges[rangeName].Add(new string[] { splitStr[3], splitStr[4] });
                }
            }
            return ranges[rangeName];
        }

        public List<string> GetClients(bool forceUpdate = false)
        {
            string commonQuery = "LIST CLIENT " + Name;
            if (forceUpdate || clients.Count == 0)
            {
                List<string> response = client.SendQuery(commonQuery);
                if (!response[0].Equals("BEGIN " + commonQuery) ||
                    !response[response.Count - 1].Equals("END " + commonQuery))
                    throw new Exception("Malformed header or footer in response from server.");
                clients = new List<string>();
                foreach (string str in response)
                {
                    string[] splitStr = str.Split(' ');
                    if (splitStr.Length != 3 || splitStr[0].Equals("CLIENT") || splitStr[1].Equals(Name))
                        throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                    clients.Add(splitStr[2]);
                }
            }
            return clients;
        }
    }
}
