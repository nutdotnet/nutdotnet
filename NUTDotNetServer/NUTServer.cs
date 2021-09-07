using NUTDotNetShared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUTDotNetServer
{
    public class NUTServer : IDisposable
    {
        #region Public Members

        //Specify the network protocol version
        public const string NETVER = "1.2";

        /// <summary>
        /// Number of seconds to wait before disconnecting a client. A value <= 0 disables the timeout function.
        /// </summary>
        public int ClientTimeout = 60;

        // UPSs that are configured for this server.
        public List<ServerUPS> UPSs;

        #endregion

        #region Properties

        public IPAddress ListenAddress { get; }

        // List of clients allowed to execute commands. Even unauthorized clients are allowed to establish
        // a connection.
        public List<IPAddress> AuthorizedClientAddresses { get; set; }

        // If given autoassign port number (0), this will be invalid until the listener has started.
        public int ListenPort { get; private set; }
        public string Username { get; }
        public bool IsListening { get; private set; }
        public string ServerVersion
        {
            get
            {
                var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                return assemblyName.FullName + " " + assemblyName.Version;
            }
        }

        #endregion

        #region Private Members

        private bool disposed = false;
        private TcpListener tcpListener;
        private bool singleQueryMode = false;

        private ConcurrentDictionary<IPAddress, ClientMetadata> clients;
        private ConcurrentDictionary<IPAddress, DateTime> clientsLastSeen;

        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;
        private Task acceptConnections;
        private Task monitorClients;

        #endregion

        public NUTServer(ushort listenPort = NUTCommon.DEFAULT_PORT, bool singleQuery = false)
        {
            ListenAddress = IPAddress.Any;
            ListenPort = listenPort;
            AuthorizedClientAddresses = new List<IPAddress>();
            clients = new ConcurrentDictionary<IPAddress, ClientMetadata>();
            UPSs = new List<ServerUPS>();
            singleQueryMode = singleQuery;
        }

        #region Public Methods

        public void Start()
        {
            if (IsListening) throw new InvalidOperationException("Server is already started and listening.");

            if (clientsLastSeen is null) clientsLastSeen = new ConcurrentDictionary<IPAddress, DateTime>();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            tcpListener = new TcpListener(IPAddress.Any, ListenPort);
            Debug.WriteLine("Server starting on " + tcpListener.LocalEndpoint.ToString());

            acceptConnections = Task.Run(() => AcceptConnections(), cancellationToken);
            monitorClients = Task.Run(() => MonitorForIdleClients(), cancellationToken);
        }

        public void Stop()
        {
            if (!IsListening) throw new InvalidOperationException("Server is already stopped!");

            IsListening = false;
            tcpListener.Stop();
            cancellationTokenSource.Cancel();
            Debug.WriteLine("Server has stopped.");
        }

        /// <summary>
        /// Disconnects a client from the server.
        /// </summary>
        /// <param name="ipPort">The IP and Port string of the client's endpoint.</param>
        /// <returns>True if client was found and disconnected, false if the client was not found.</returns>
        public void DisconnectClient(IPAddress ip)
        {
            if (clients.TryGetValue(ip, out ClientMetadata client))
            {
                client.Dispose();
                clients.TryRemove(ip, out _);
            }
        }

        public ServerUPS GetUPSByName(string name)
        {
            for (int i = 0; i < UPSs.Count; i++)
            {
                if (UPSs[i].Name.Equals(name))
                    return UPSs[i];
            }

            throw new Exception("ERR UNKNOWN-UPS");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                tcpListener.Server.Close();
                tcpListener.Stop();
                acceptConnections = null;
            }

            disposed = true;
        }

        #endregion

        #region Private Memebers

        /// <summary>
        /// Start the listener and begin looping to accept clients.
        /// </summary>
        private async Task AcceptConnections()
        {
            tcpListener.Start();
            if (ListenPort == 0)
                ListenPort = ((IPEndPoint)tcpListener.Server.LocalEndPoint).Port;
            IsListening = true;

            while (true)
            {
                try
                {
                    // Wait for a connection.
                    TcpClient newClient = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                    newClient.LingerState.Enabled = false;
                    Debug.WriteLine("New client connecting from " + newClient.Client.RemoteEndPoint);
                    ClientMetadata newClientMetadata = new ClientMetadata(newClient);
                    clients.TryAdd(newClientMetadata.Ip, newClientMetadata);
                    clientsLastSeen.TryAdd(newClientMetadata.Ip, DateTime.Now);
                    Debug.WriteLine("Starting data receiving for client " + newClientMetadata.Ip);
                    Task dataReceiverTask = Task.Run(() => DataReceiver(newClientMetadata), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Debug.WriteLine("Cancellating AcceptConnections task.");
        }

        /// <summary>
        /// Check the clients list and time them out if necessary.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private async Task MonitorForIdleClients()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                    if (ClientTimeout > 0 && clientsLastSeen.Count > 0)
                    {
                        DateTime idleTimestamp = DateTime.Now.AddSeconds(-1 * ClientTimeout);

                        foreach (KeyValuePair<IPAddress, DateTime> curr in clientsLastSeen)
                        {
                            if (curr.Value < idleTimestamp)
                            {
                                Debug.WriteLine(curr.Key + " is being kicked due to timeout.");
                                DisconnectClient(curr.Key);
                            }
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (OperationCanceledException)
            {

            }
        }

        /// <summary>
        /// Determine is the specified client is allowed to execute commands on the server. Note: A NUT server will
        /// still allow an unauthorized client to connect. The client will be able to send commands and remain
        /// connected, while only getting an unauthorized error in response. They will also be able to execute the
        /// LOGOUT command.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private bool IsClientAuthorized(ClientMetadata client)
        {
            // Authorization system is disabled when no clients are on the list.
            if (AuthorizedClientAddresses.Count == 0)
                return true;

            IPEndPoint clientEndpoint = (IPEndPoint)client.TcpClient.Client.RemoteEndPoint;
            return AuthorizedClientAddresses.Contains(clientEndpoint.Address);
        }

        async Task DataReceiver(ClientMetadata client)
        {
            bool isAuthorized = IsClientAuthorized(client);
            // Authentication details passed from the client during this session. Will determine is they can execute
            // certain commands.
            string sessionUsername = "";
            string sessionPassword = "";

            StreamReader streamReader = new StreamReader(client.NetworkStream, NUTCommon.PROTO_ENCODING);
            StreamWriter streamWriter = new StreamWriter(client.NetworkStream, NUTCommon.PROTO_ENCODING)
            {
                AutoFlush = true,
                NewLine = NUTCommon.NewLine
            };

            // Enter into a loop of listening a responding to queries.
            string readLine;
            while (client.TcpClient.Connected)
            {
                if (!client.NetworkStream.DataAvailable)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                    continue;
                }

                readLine = streamReader.ReadLine();
                clientsLastSeen.AddOrUpdate(client.Ip, DateTime.Now, (key, value) => DateTime.Now);
                // Remove quotes
                readLine = readLine.Replace("\"", string.Empty);
                // Split the query around whitespace characters.
                string[] splitLine = readLine.Split();
                Debug.WriteLine(client.Ip + " says " + readLine);
                // If the client is not authorized, then any command besides LOGOUT will result in an A.D error.
                if (!splitLine[0].Equals("LOGOUT") & !isAuthorized)
                {
                    streamWriter.WriteLine("ERR ACCESS-DENIED");
                }
                else
                {
                    if (splitLine[0].Equals("VER"))
                        streamWriter.WriteLine(ServerVersion);
                    else if (splitLine[0].Equals("NETVER"))
                        streamWriter.WriteLine(NETVER);
                    else if (splitLine[0].Equals("GET"))
                        streamWriter.Write(ParseGetQuery(splitLine));
                    else if (splitLine[0].Equals("LIST") && splitLine.Length > 1)
                        streamWriter.Write(ParseListQuery(splitLine));
                    else if (splitLine[0].Equals("INSTCMD") && splitLine.Length == 3)
                        streamWriter.Write(DoInstCmd(splitLine[1], splitLine[2]));
                    else if (splitLine[0].Equals("SET") && splitLine.Length == 5)
                        streamWriter.Write(DoSetVar(splitLine[2], splitLine[3], splitLine[4]));
                    else if (splitLine[0].Equals("USERNAME"))
                    {
                        if (splitLine.Length != 2 || string.IsNullOrWhiteSpace(splitLine[1]))
                        {
                            streamWriter.WriteLine("ERR INVALID-ARGUMENT");
                            continue;
                        }
                        else if (!string.IsNullOrEmpty(sessionUsername))
                        {
                            streamWriter.WriteLine("ERR ALREADY-SET-USERNAME");
                            continue;
                        }
                        sessionUsername = splitLine[1];
                        streamWriter.WriteLine("OK");
                    }
                    else if (splitLine[0].Equals("PASSWORD"))
                    {
                        if (splitLine.Length != 2 || string.IsNullOrWhiteSpace(splitLine[1]))
                        {
                            streamWriter.WriteLine("ERR INVALID-ARGUMENT");
                            continue;
                        }
                        else if (!string.IsNullOrEmpty(sessionPassword))
                        {
                            streamWriter.WriteLine("ERR ALREADY-SET-PASSWORD");
                            continue;
                        }
                        sessionPassword = splitLine[1];
                        streamWriter.WriteLine("OK");
                    }
                    else if (splitLine[0].Equals("LOGIN"))
                    {
                        if (splitLine.Length != 2 || string.IsNullOrWhiteSpace(splitLine[1]))
                            streamWriter.WriteLine("ERR INVALID-ARGUMENT");
                        else if (string.IsNullOrEmpty(sessionUsername) || string.IsNullOrEmpty(sessionPassword))
                            streamWriter.WriteLine("ERR ACCESS-DENIED");
                        else
                            streamWriter.Write(ClientLogin(splitLine[1], client.Ip.ToString()));

                    }
                    else if (readLine.Equals("LOGOUT"))
                    {
                        streamWriter.Write(ClientLogout(client.Ip.ToString()));
                        break;
                    }
                    else
                    {
                        streamWriter.WriteLine("ERR UNKNOWN-COMMAND");
                    }
                }
                if (singleQueryMode)
                    break;
            }

            Debug.WriteLine("Client " + client.Ip + " has disconnected.");
            streamReader.Dispose();
            streamWriter.Dispose();
            clients.TryRemove(client.Ip, out _);
            clientsLastSeen.TryRemove(client.Ip, out _);
            client.Dispose();
        }

        private string ClientLogin(string upsName, string clientAddress)
        {
            try
            {
                ServerUPS upsObject = GetUPSByName(upsName);
                if (upsObject.Clients.Contains(clientAddress))
                    throw new Exception("ERR ALREADY-LOGGED-IN");
                upsObject.Clients.Add(clientAddress);
                Debug.WriteLine("Logged client " + clientAddress + " into UPS " + upsObject);
                return "OK" + NUTCommon.NewLine;
            }
            catch (Exception ex)
            {
                return ex.Message + NUTCommon.NewLine;
            }
        }

        private string ClientLogout(string clientAddress)
        {
            // NUT protocol doesn't support a UPS name parameter when logging out, so we need to search for the client
            // in all UPSes.
            foreach (ServerUPS ups in UPSs)
            {
                if (ups.Clients.Remove(clientAddress))
                    Debug.WriteLine("Removed client " + clientAddress + " from " + ups);
            }

            return "OK Goodbye" + NUTCommon.NewLine;
        }

        private string DoSetVar(string upsName, string varName, string value)
        {
            try
            {
                UPSVariable upsVar = GetUPSByName(upsName).GetVariableByName(varName);
                if (upsVar is null)
                    throw new Exception("ERR VAR-NOT-SUPPORTED");
                upsVar.Value = value;
                return "OK" + NUTCommon.NewLine;
            }
            catch (Exception)
            {
                return "ERR VAR-NOT-SUPPORTED" + NUTCommon.NewLine;
            }
        }

        /// <summary>
        /// Simulates an Instant Command that a NUT server would run on a UPS. Since this is just a test server,
        /// the method only searches for the specified command and doesn't actually run anything.
        /// </summary>
        /// <param name="upsName"></param>
        /// <param name="cmdName"></param>
        /// <returns></returns>
        private string DoInstCmd(string upsName, string cmdName)
        {
            try
            {
                ServerUPS upsObject = GetUPSByName(upsName);
                if (!upsObject.InstantCommands.ContainsKey(cmdName))
                    return "ERR CMD-NOT-SUPPORTED" + NUTCommon.NewLine;
                else
                    return "OK" + NUTCommon.NewLine;
            }
            catch (Exception ex)
            {
                return ex.Message + NUTCommon.NewLine;
            }
        }

        /// <summary>
        /// Construct a string of type(s) that apply to the variable.
        /// </summary>
        /// <param name="upsVar"></param>
        /// <returns></returns>
        string GetVarType(UPSVariable upsVar)
        {
            String retString = "";
            if (upsVar.Flags.HasFlag(VarFlags.RW))
                retString += " RW";

            if (upsVar.Enumerations.Count > 0)
                retString += " ENUM";

            if (upsVar.Ranges.Count > 0)
                retString += " RANGE";

            // Note: the value appended to STRING: should be the *max* length, not current length.
            if (upsVar.Flags.HasFlag(VarFlags.String))
            {
                retString += " STRING:" + upsVar.Value.Length;
                return retString;
            }
            
            // netget.c: Any variable that is not string | range | enum is just a simple numeric value.
            retString += " NUMBER";
            return retString;
        }

        /// <summary>
        /// Parses and processes one of the GET queries.
        /// </summary>
        /// <param name="splitQuery"></param>
        /// <returns></returns>
        string ParseGetQuery(string[] splitQuery)
        {
            StringBuilder response = new StringBuilder();
            try
            {
                string subquery = splitQuery.Length >= 2 ? splitQuery[1] : string.Empty;
                ServerUPS ups = GetUPSByName(splitQuery[2]);
                string itemName = splitQuery.Length >= 4 ? splitQuery[3] : string.Empty;

                if (subquery.Equals("NUMLOGINS"))
                {
                    response.AppendFormat("NUMLOGINS {0} {1}{2}", ups.Name, ups.Clients.Count, NUTCommon.NewLine);
                }
                else if (subquery.Equals("UPSDESC"))
                {
                    response.AppendFormat("UPSDESC {0} \"{1}\"{2}", ups.Name, ups.Description, NUTCommon.NewLine);
                }
                else if (subquery.Equals("VAR") && splitQuery.Length == 4)
                {
                    UPSVariable upsVar = ups.GetVariableByName(itemName);
                    response.AppendFormat("VAR {0} {1} \"{2}\"{3}", ups.Name, itemName, upsVar.Value, NUTCommon.NewLine);
                }
                else if (subquery.Equals("TYPE") && splitQuery.Length == 4)
                {
                    UPSVariable upsVar = ups.GetVariableByName(itemName);
                    string type = GetVarType(upsVar);
                    response.AppendFormat("TYPE {0} {1}{2}{3}", ups.Name, itemName, type, NUTCommon.NewLine);
                }
                else if (subquery.Equals("DESC"))
                {
                    UPSVariable upsVar = ups.GetVariableByName(itemName);
                    string description = string.IsNullOrWhiteSpace(upsVar.Description) ?
                        "Description unavailable" : upsVar.Description;
                    response.AppendFormat("DESC {0} {1} \"{2}\"{3}", ups.Name, itemName, description,
                        NUTCommon.NewLine);
                }
                else if (subquery.Equals("CMDDESC"))
                {
                    if (!ups.InstantCommands.TryGetValue(itemName, out string description)
                        || string.IsNullOrEmpty(ups.InstantCommands[itemName]))
                        description = "Unavailable";
                    response.AppendFormat("CMDDESC {0} {1} \"{2}\"{3}", ups.Name, itemName, description,
                        NUTCommon.NewLine);
                }
                else
                    throw new Exception("ERR INVALID-ARGUMENT");

                return response.ToString();
            }
            catch (IndexOutOfRangeException)
            {
                return "ERR INVALID-ARGUMENT" + NUTCommon.NewLine;
            }
            catch (KeyNotFoundException)
            {
                return "ERR VAR-NOT-SUPPORTED" + NUTCommon.NewLine;
            }
            catch (InvalidOperationException)
            {
                return "ERR VAR-NOT-SUPPORTED" + NUTCommon.NewLine;
            }
            catch (Exception ex)
            {
                return ex.Message + NUTCommon.NewLine;
            }
        }

        // Valid quieries for retrieving properties of a UPS.
        private static List<string> ValidUPSQueries = new List<string> { "VAR", "RW", "CMD", "CLIENT" };
        // Valid ways to query a specified property of a UPS.
        private static List<string> ValidUPSPropQueries = new List<string> { "ENUM", "RANGE" };
        private string ParseListQuery(string[] splitQuery)
        {
            StringBuilder response = new StringBuilder();
            try
            {
                string subquery = splitQuery.Length >= 2 ? splitQuery[1] : string.Empty;
                string upsName = splitQuery.Length >= 3 ? splitQuery[2] : string.Empty;
                ServerUPS upsObject;
                string varName = splitQuery.Length >= 4 ? splitQuery[3] : string.Empty;

                if (subquery.Equals("UPS"))
                {
                    response.Append("BEGIN LIST UPS" + NUTCommon.NewLine);
                    foreach (ServerUPS ups in UPSs)
                        response.AppendFormat("UPS {0} \"{1}\"{2}", ups.Name, ups.Description, NUTCommon.NewLine);
                    response.Append("END LIST UPS" + NUTCommon.NewLine);
                }
                else if (!upsName.Equals(string.Empty) && ValidUPSQueries.Contains(subquery))
                {
                    upsObject = GetUPSByName(upsName);
                    response.AppendFormat("BEGIN LIST {0} {1}{2}", subquery, upsName, NUTCommon.NewLine);
                    if (subquery.Equals("VAR"))
                        foreach (UPSVariable var in upsObject.GetListOfVariables(AbstractUPS.VarList.Variables))
                            response.AppendFormat("{0} {1} {2} \"{3}\"{4}", subquery, upsObject.Name, var.Name,
                                var.Value, NUTCommon.NewLine);
                    else if (subquery.Equals("RW"))
                        foreach (UPSVariable var in upsObject.GetListOfVariables(AbstractUPS.VarList.Rewritables))
                            response.AppendFormat("{0} {1} {2} \"{3}\"{4}", subquery, upsObject.Name, var.Name,
                                var.Value, NUTCommon.NewLine);
                    else if (subquery.Equals("CMD"))
                        foreach (string var in upsObject.InstantCommands.Keys)
                            response.AppendFormat("{0} {1} {2}{3}", subquery, upsObject.Name, var,
                                NUTCommon.NewLine);
                    else if (subquery.Equals("CLIENT"))
                        upsObject.Clients.ForEach(str => response.AppendFormat("{0} {1} {2}{3}", subquery,
                            upsObject.Name, str, NUTCommon.NewLine));
                    response.AppendFormat("END LIST {0} {1}{2}", subquery, upsName, NUTCommon.NewLine);
                }
                else if (!varName.Equals(string.Empty) && ValidUPSPropQueries.Contains(subquery))
                {
                    upsObject = GetUPSByName(upsName);
                    response.AppendFormat("BEGIN LIST {0} {1} {2}{3}", subquery, upsName, varName, NUTCommon.NewLine);
                    if (subquery.Equals("ENUM"))
                        response.Append(upsObject.EnumerationToString(varName));
                    else if (subquery.Equals("RANGE"))
                        response.Append(upsObject.RangeToString(varName));
                    response.AppendFormat("END LIST {0} {1} {2}{3}", subquery, upsName, varName, NUTCommon.NewLine);
                }
                // Bad subquery provided.
                else
                {
                    throw new Exception(string.Empty);
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message.Equals(string.Empty) ? "ERR INVALID-ARGUMENT" : ex.Message;
                response.Clear();
                response.Append(error + NUTCommon.NewLine);
            }

            return response.ToString();
        }

        #endregion
    }
}
