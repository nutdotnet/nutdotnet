using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NUTDotNetClient
{
    /// <summary>
    /// Represents a UPS that is managed by the NUT server. This class provides additional functionality to acquire
    /// data from the server.
    /// </summary>
    public class ClientUPS : AbstractUPS
    {
        #region Properties

        /// <summary>
        /// Is the client registered as dependant on this UPS? In case of a power-down event, the NUT server will wait
        /// until this client has logged out before shutting down.
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>
        /// Initializes & returns the list of variables from the NUT server, or returns the local copy.
        /// </summary>
        public HashSet<ClientVariable> Variables
        {
            get
            {
                if (variables == null)
                    variables = GetVariables();

                return variables;
            }
        }

        /// <summary>
        /// A list of "Instant Commands" that can be run on this UPS.
        /// </summary>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> of commands by Name, Description</returns>
        public override Dictionary<string, string> InstantCommands
        {
            get
            {
                if (instantCommands == null)
                    instantCommands = GetCommands();

                return instantCommands;
            }
        }

        #endregion

        #region Private fields

        private readonly NUTClient client;
        private new HashSet<ClientVariable> variables;

        #endregion

        internal ClientUPS(NUTClient client, string name, string description) : base(name, description)
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
            string response = client.nutSocket.SimpleQuery("LOGIN " + Name)[0]; // client.SendQuery("LOGIN " + Name)[0];
            IsLoggedIn = response.Equals("OK");
        }

        /// <summary>
        /// Gets the number of clients logged in to this UPS. Uses the GET NUMLOGINS protocol query.
        /// Protocol responds: NUMLOGINS <upsname> <value>
        /// </summary>
        /// <returns></returns>
        public int GetNumLogins()
        {
            string[] response = client.nutSocket.SimpleQuery("GET NUMLOGINS " + Name);
            return int.Parse(response[2]);
        }

        

        /// <summary>
        /// Sends an INSTCMD query to the NUT server for execution. Throws an error if unsuccessful.
        /// </summary>
        /// <param name="command">The name of the command to be run.</param>
        public void DoInstantCommand(string command)
        {
            client.nutSocket.SimpleQuery(string.Format("INSTCMD {0} {1}", Name, command));
        }

        /// <summary>
        /// Sends a query to the NUT server to set a rewritable variable. Any errors will be thrown, otherwise nothing
        /// is returned on success.
        /// </summary>
        /// <param name="rewritableKey">The key (variable name) to be set, found in the Rewritables collection.</param>
        /// <param name="value">The value that this variable should be set to.</param>
        public void SetVariable(string rewritableKey, string value)
        {
            client.nutSocket.SimpleQuery(string.Format("SET VAR {0} {1} \"{2}\"", Name, rewritableKey, value));
            GetRewritables(true);
        }

        /// <summary>
        /// Retrieve a single variable either from the local variables cache, or the NUT server and update the
        /// information stored locally. Any encountered errors will be thrown (NUTError).
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="forceUpdate">Get the variable from the NUT server, even if it's stored locally.</param>
        /// <returns></returns>
        //public UPSVariable GetVariable(string varName, bool forceUpdate)
        //{
        //    UPSVariable returnVar;
        //    bool variableExists = false;

        //    try
        //    {
        //        returnVar = GetVariable(varName);
        //        variables.Remove(returnVar);
        //        variableExists = true;
        //    }
        //    catch (InvalidOperationException)
        //    {
        //        // Should figure out the variable type first so there's no confusion.
        //        returnVar = new UPSVariable(varName, VarFlags.String);
        //    }

        //    // If the variable was already in the cache and user does not want a forceUpdate, then return what we have.
        //    if (forceUpdate || !variableExists)
        //    {
        //        string[] response = client.SendQuery(string.Format("GET VAR {0} {1}", Name, varName))[0]
        //            .Split(new char[] { ' ' }, 4);
        //        if (response.Length != 4 || !response[0].Equals("VAR") || !response[1].Equals(Name) ||
        //            !response[2].Equals(varName))
        //            throw new Exception("Response from NUT server was unexpected or malformed: " + response.ToString());

        //        returnVar.Value = response[3].Replace("\"", string.Empty);
        //        returnVar.Description = GetVariableDescription(returnVar.Name);
        //    }

        //    variables.Add(returnVar);
        //    return returnVar;
        //}

        public new ClientVariable GetVariable(string varName)
        {
            // return GetVariable(varName, false);
            return (ClientVariable) base.GetVariable(varName);
        }

        /// <summary>
        /// Gets the variables assigned to this UPS from the server. Note: All variables will have the "None" flag
        /// since this information isn't returned from the server by default. Use GET TYPE (ups) (var name) to find
        /// the correct flags.
        /// </summary>
        /// <param name="forceUpdate">Download the list of variables from the server,even if one is cached here.
        /// </param>
        /// <returns></returns>
        //public List<UPSVariable> GetVariables(bool forceUpdate = false)
        //{
        //    List<UPSVariable> varCopy = new List<UPSVariable>(GetListOfVariables(VarList.Variables));
        //    if (forceUpdate || varCopy.Count == 0)
        //    {
        //        // Remove any duplicate variables from the set first.
        //        variables.ExceptWith(varCopy);
        //        List<string[]> response = GetListResponse("VAR");
        //        foreach (string[] str in response)
        //        {
        //            UPSVariable var = new UPSVariable(str[2], VarFlags.None);
        //            var.Description = GetVariableDescription(var.Name);
        //            var.Value = str[3];
        //            varCopy.Add(var);
        //        }
        //        // Now add the updated list of variables back in.
        //        variables.UnionWith(varCopy);
        //    }
        //    return varCopy;
        //}

        public List<UPSVariable> GetRewritables(bool forceUpdate = false)
        {
            //List<UPSVariable> rewritables = new List<UPSVariable>(GetListOfVariables(VarList.Rewritables));
            //if (forceUpdate || rewritables.Count == 0)
            //{
            //    variables.ExceptWith(rewritables);
            //    List<string[]> response = GetListResponse("RW");
            //    foreach (string[] str in response)
            //    {
            //        UPSVariable var = new UPSVariable(str[2], VarFlags.RW);
            //        var.Value = str[3];
            //        rewritables.Add(var);
            //    }
            //    variables.UnionWith(rewritables);
            //}
            //return rewritables;

            throw new NotImplementedException();
        }

        public List<string> GetEnumerations(string enumName, bool forceUpdate = false)
        {
            //UPSVariable var = null;
            //try
            //{
            //    var = base.GetVariable(enumName);
            //}
            //catch (InvalidOperationException)
            //{

            //}

            //if (forceUpdate || var is null)
            //{
            //    var = new UPSVariable(enumName, VarFlags.None);
            //    List<string[]> response = GetListResponse("ENUM", enumName);
            //    var.Enumerations = new List<string>(response.Count);
            //    response.ForEach(str => var.Enumerations.Add(str[3]));
            //}
            //return var.Enumerations;

            throw new NotImplementedException();
        }

        public List<Tuple<int, int>> GetRanges(string rangeName, bool forceUpdate = false)
        {
            //UPSVariable var = null;
            //try
            //{
            //    var = base.GetVariable(rangeName);
            //}
            //catch (InvalidOperationException)
            //{

            //}

            //if (forceUpdate || var is null)
            //{
            //    var = new UPSVariable(rangeName, VarFlags.None);
            //    List<string[]> response = GetListResponse("RANGE", rangeName);
            //    var.Ranges = new List<Tuple<int, int>>(response.Count);
            //    response.ForEach(str => var.Ranges.Add(new Tuple<int, int>(int.Parse(str[3]), int.Parse(str[4]))));
            //}
            //return var.Ranges;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a list of clients logged in to the UPS. This is not cached locally.
        /// </summary>
        public List<string> GetClients()
        {
            List<string[]> response = client.nutSocket.ListQuery("CLIENT", Name);
            clients = new List<string>();
            response.ForEach(str => clients.Add(str[2]));

            return clients;
        }

        #region Private methods

        /// <summary>
        /// Gets the list of variables from the NUT server.
        /// </summary>
        private HashSet<ClientVariable> GetVariables()
        {
            HashSet<ClientVariable> returnList = new HashSet<ClientVariable>();

            // Response format: BEGIN LIST VAR <upsname>
            //                  VAR <upsname> <varname> "<value>"
            List<string[]> listResponse = client.nutSocket.ListQuery("VAR", Name);

            foreach (string[] str in listResponse)
            {
                ClientVariable var = new ClientVariable(client.nutSocket, this, str[2]);
                // var.Description = GetVariableDescription(var.Name);
                var.Value = str[3];
                returnList.Add(var);
            }

            return returnList;
        }

        /// <summary>
        /// Retrieves a list of "Instant Commands" available to this UPS from the NUT server.
        /// </summary>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> of commands by Name, Description</returns>
        private Dictionary<string, string> GetCommands()
        {
            Dictionary<string, string> retList;

            // Expect a reponse line to look like: CMD <ups name> <cmd name>
            List<string[]> listResponse = client.nutSocket.ListQuery("CMD", Name);
            retList = new Dictionary<string, string>(listResponse.Count);
            listResponse.ForEach(line => retList.Add(line[2], GetCommandDescription(line[2])));

            return retList;
        }

        /// <summary>
        /// Retrieves the description of a command from the NUT server.
        /// </summary>
        /// <param name="cmdName"></param>
        /// <returns></returns>
        private string GetCommandDescription(string cmdName)
        {
            string[] response = client.nutSocket.SimpleQuery(string.Format("GET CMDDESC {0} {1}", Name, cmdName));
            if (response.Length != 4 || !response[0].Equals("CMDDESC") || !response[1].Equals(Name) ||
                    !response[2].Equals(cmdName))
                throw new Exception("Response from NUT server was unexpected or malformed: " + response.ToString());

            return response[3];
        }

        #endregion
    }
}
