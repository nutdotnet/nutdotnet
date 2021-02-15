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
        public ushort ListenPort { get; }
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
        #endregion

        public NUTServer(ushort listenPort = NUTCommon.DEFAULT_PORT)
        {
            ListenPort = listenPort;
            ListenAddress = IPAddress.Any;
            AuthorizedClients = new List<IPAddress>();
            connectedClients = new List<TcpClient>();
            UPSs = new List<UPS>();
            tcpListener = new TcpListener(IPAddress.Any, ListenPort);
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

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
            if (IsListening)
                throw new InvalidOperationException("Server is already listening.");

            tcpListener.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a connection.
                TcpClient newClient = await tcpListener.AcceptTcpClientAsync();
                connectedClients.Add(newClient);
                HandleNewClient(newClient);

                newClient.Close();
                connectedClients.Remove(newClient);
            }

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
                readLine = streamReader.ReadLine();
                if (readLine is null)
                    continue;

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
                    }
                    else
                    {
                        streamWriter.WriteLine("ERR UNKNOWN-COMMAND");
                    }
                }
            }

            Debug.WriteLine("Client has gone away.");
        }

        private string ParseListQuery(string query)
        {
            StringBuilder response = new StringBuilder();
            try
            {
                string[] dividedQuery = query.Split(null, 3);

                if (dividedQuery[0].Equals("UPS"))
                {
                    response.Append("BEGIN LIST UPS" + NUTCommon.NewLine);
                    foreach (UPS ups in UPSs)
                        response.Append(ups + NUTCommon.NewLine);
                    response.Append("END LIST UPS" + NUTCommon.NewLine);
                }
            }
            catch (Exception ex)
            {

            }

            return response.ToString();
        }
    }
}
