using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public IPAddress ListenAddress { get; }
        // List of clients allowed to execute commands. Even unauthorized clients are allowed to establish
        // a connection.
        public List<IPAddress> AuthorizedClients { get; set; }
        // UPSs that are configured for this server.
        public List<UPS> UPSs;
        // If given autoassign port number (0), this will be invalid until the listener has started.
        public int ListenPort
        {
            get
            {
                return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            }
        }
        public string Username { get; }
        public bool IsListening
        {
            get
            {
                if (tcpListener is null)
                    return false;
                else
                    return tcpListener.Server.IsBound;
            }
        }
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
        private string Password;
        private TcpListener tcpListener;
        List<TcpClient> connectedClients;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;
        private bool singleQueryMode = false;
        #endregion

        public NUTServer(ushort listenPort = NUTCommon.DEFAULT_PORT, bool singleQuery = false)
        {
            ListenAddress = IPAddress.Any;
            AuthorizedClients = new List<IPAddress>();
            connectedClients = new List<TcpClient>();
            UPSs = new List<UPS>();
            tcpListener = new TcpListener(IPAddress.Any, listenPort);
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            singleQueryMode = singleQuery;

            tcpListener.Start();
            Debug.WriteLine("NUT server has started. PID: {0}, Port: {1}",
                Thread.CurrentThread.ManagedThreadId, ListenPort);

            Task.Run(() => BeginListening(), cancellationToken);
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
            }

            disposed = true;
        }

        /// <summary>
        /// Start the listener and begin looping to accept clients.
        /// </summary>
        private async Task BeginListening()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a connection.
                TcpClient newClient = await tcpListener.AcceptTcpClientAsync();
                Debug.WriteLine("New client connecting from " + newClient.Client.RemoteEndPoint);
                connectedClients.Add(newClient);
                HandleNewClient(newClient);

                newClient.Close();
                connectedClients.Remove(newClient);
            }

            Debug.WriteLine("Cancellation requested, shutting down server.");
            tcpListener.Stop();
        }

        /// <summary>
        /// Determine is the specified client is allowed to execute commands on the server. Note: A NUT server will
        /// still allow an unauthorized client to connect. The client will be able to send commands and remain
        /// connected, while only getting an unauthorized error in response. They will also be able to execute the
        /// LOGOUT command.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private bool IsClientAuthorized(TcpClient client)
        {
            // Authorization system is disabled when no clients are on the list.
            if (AuthorizedClients.Count == 0)
                return true;

            IPEndPoint clientEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
            return AuthorizedClients.Contains(clientEndpoint.Address);
        }

        void HandleNewClient(TcpClient newClient)
        {
            bool isAuthorized = IsClientAuthorized(newClient);

            NetworkStream clientNetStream = newClient.GetStream();
            StreamReader streamReader = new StreamReader(clientNetStream, NUTCommon.PROTO_ENCODING);
            StreamWriter streamWriter = new StreamWriter(clientNetStream, NUTCommon.PROTO_ENCODING)
            {
                AutoFlush = true,
                NewLine = NUTCommon.NewLine
            };

            // Enter into a loop of listening a responding to queries.
            string readLine;
            while (newClient.Connected)
            {
                if (!clientNetStream.DataAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }

                readLine = streamReader.ReadLine();
                Debug.WriteLine(newClient.Client.RemoteEndPoint.ToString() + " says " + readLine);
                // If the client is not authorized, then any command besides LOGOUT will result in an A.D error.
                if (!readLine.Equals("LOGOUT") & !isAuthorized)
                {
                    streamWriter.WriteLine("ERR ACCESS-DENIED");
                }
                else
                {
                    if (readLine.StartsWith("LIST ") && readLine.Length > 5)
                        streamWriter.Write(ParseListQuery(readLine.Substring(5)));
                    else if (readLine.Equals("VER"))
                    {
                        streamWriter.WriteLine(ServerVersion);
                    }
                    else if (readLine.Equals("NETVER"))
                    {
                        streamWriter.WriteLine(NETVER);
                    }
                    else if (readLine.Equals("LOGOUT"))
                    {
                        streamWriter.WriteLine("OK Goodbye");
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
            streamReader.Dispose();
            streamWriter.Dispose();
            Debug.WriteLine("Client " + newClient.Client.RemoteEndPoint.ToString() + " has disconnected.");
        }

        // Valid quieries for retrieving properties of a UPS.
        private static List<string> ValidUPSQueries = new List<string> { "VAR", "RW", "CMD", "CLIENT" };
        // Valid ways to query a specified property of a UPS.
        private static List<string> ValidUPSPropQueries = new List<string> { "ENUM", "RANGE" };
        private string ParseListQuery(string query)
        {
            StringBuilder response = new StringBuilder();
            try
            {
                string[] dividedQuery = query.Split(null, 3);
                string subquery = dividedQuery[0];
                string upsName = dividedQuery.Length >= 2 ? dividedQuery[1] : string.Empty;
                UPS upsObject;
                string varName = dividedQuery.Length >= 3 ? dividedQuery[2] : string.Empty;

                if (subquery.Equals("UPS"))
                {
                    response.Append("BEGIN LIST UPS" + NUTCommon.NewLine);
                    foreach (UPS ups in UPSs)
                        response.Append(ups + NUTCommon.NewLine);
                    response.Append("END LIST UPS" + NUTCommon.NewLine);
                }
                else if (!upsName.Equals(string.Empty) && ValidUPSQueries.Contains(subquery))
                {
                    upsObject = GetUPSByName(upsName);
                    response.AppendFormat("BEGIN LIST {0} {1}{2}", subquery, upsName, NUTCommon.NewLine);
                    if (subquery.Equals("VAR"))
                        response.Append(upsObject.DictionaryToString("VAR", upsObject.Variables));
                    else if (subquery.Equals("RW"))
                        response.Append(upsObject.DictionaryToString("RW", upsObject.Rewritables));
                    else if (subquery.Equals("CMD"))
                        response.Append(upsObject.ListToString("CMD", upsObject.Commands));
                    else if (subquery.Equals("CLIENT"))
                        response.Append(upsObject.ListToString("CLIENT", upsObject.Clients));
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

        public UPS GetUPSByName(string name)
        {
            for (int i = 0; i < UPSs.Count; i++)
            {
                if (UPSs[i].Name.Equals(name))
                    return UPSs[i];
            }

            throw new Exception("ERR UNKNOWN-UPS");
        }
    }
}
