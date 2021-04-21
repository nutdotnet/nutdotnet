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
        /// <summary>
        /// Is the client registered as dependant on this UPS? In case of a power-down event, the NUT server will wait
        /// until this client has logged out before shutting down.
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        private readonly NUTClient client;

        public ClientUPS(NUTClient client, string name, string description) : base(name, description)
        {
            this.client = client;
            IsLoggedIn = false;
        }

        /// <summary>
        /// Tells the NUT server that we're depending on it for power, so it will wait for us to disconnect before
        /// shutting down. Any encountered errors will be thrown, otherwise the command runs successfully.
        /// </summary>
        public void Login()
        {
            string response = client.SendQuery("LOGIN " + Name)[0];
            IsLoggedIn = response.Equals("OK");
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
        /// <param name="command">The name of the command to be run.</param>
        public void DoInstantCommand(string command)
        {
            client.SendQuery(string.Format("INSTCMD {0} {1}", Name, command));
        }

        /// <summary>
        /// Sends a query to the NUT server to set a rewritable variable. Any errors will be thrown, otherwise nothing
        /// is returned on success.
        /// </summary>
        /// <param name="rewritableKey">The key (variable name) to be set, found in the Rewritables collection.</param>
        /// <param name="value">The value that this variable should be set to.</param>
        public void SetVariable(string rewritableKey, string value)
        {
            client.SendQuery(string.Format("SET VAR {0} {1} \"{2}\"", Name, rewritableKey, value));
            GetRewritables(true);
        }

        /// <summary>
        /// Gets the variables assigned to this UPS from the server. Note: All variables will have the "None" flag
        /// since this information isn't returned from the server by default. Use GET TYPE (ups) (var name) to find
        /// the correct flags.
        /// </summary>
        /// <param name="forceUpdate">Download the list of variables from the server,even if one is cached here.
        /// </param>
        /// <returns></returns>
        public List<UPSVariable> GetVariables(bool forceUpdate = false)
        {
            List<UPSVariable> variables = new List<UPSVariable>(GetListOfVariables(VarList.Variables));
            if (forceUpdate || variables.Count == 0)
            {
                // Remove any duplicate variables from the set first.
                Variables.ExceptWith(variables);
                List<string[]> response = GetListResponse("VAR");
                foreach (string[] str in response)
                {
                    UPSVariable var = new UPSVariable(str[2], VarFlags.None);
                    var.Value = str[3];
                    variables.Add(var);
                }
                // Now add the updated list of variables back in.
                Variables.UnionWith(variables);
            }
            return variables;
        }

        public List<UPSVariable> GetRewritables(bool forceUpdate = false)
        {
            List<UPSVariable> rewritables = new List<UPSVariable>(GetListOfVariables(VarList.Rewritables));
            if (forceUpdate || rewritables.Count == 0)
            {
                Variables.ExceptWith(rewritables);
                List<string[]> response = GetListResponse("RW");
                foreach (string[] str in response)
                {
                    UPSVariable var = new UPSVariable(str[2], VarFlags.RW);
                    var.Value = str[3];
                    rewritables.Add(var);
                }
                Variables.UnionWith(rewritables);
            }
            return rewritables;
        }

        public Dictionary<string, string> GetCommands(bool forceUpdate = false)
        {
            if (forceUpdate || InstantCommands.Count == 0)
            {
                List<string[]> response = GetListResponse("CMD");
                InstantCommands = new Dictionary<string, string>();
                response.ForEach(str => InstantCommands.Add(str[2], string.Empty));
            }
            return InstantCommands;
        }

        public List<string> GetEnumerations(string enumName, bool forceUpdate = false)
        {
            UPSVariable var = null;
            try
            {
                var = GetVariableByName(enumName);
            }
            catch (InvalidOperationException)
            {

            }

            if (forceUpdate || var is null)
            {
                var = new UPSVariable(enumName, VarFlags.None);
                List<string[]> response = GetListResponse("ENUM", enumName);
                var.Enumerations = new List<string>(response.Count);
                response.ForEach(str => var.Enumerations.Add(str[3]));
            }
            return var.Enumerations;
        }

        public List<Tuple<int, int>> GetRanges(string rangeName, bool forceUpdate = false)
        {
            UPSVariable var = null;
            try
            {
                var = GetVariableByName(rangeName);
            }
            catch (InvalidOperationException)
            {

            }

            if (forceUpdate || var is null)
            {
                var = new UPSVariable(rangeName, VarFlags.None);
                List<string[]> response = GetListResponse("RANGE", rangeName);
                var.Ranges = new List<Tuple<int, int>>(response.Count);
                response.ForEach(str => var.Ranges.Add(new Tuple<int, int>(int.Parse(str[3]), int.Parse(str[4]))));
            }
            return var.Ranges;
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
