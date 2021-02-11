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
        public const ushort DEFAULT_PORT = 3493;
        public IPAddress ListenAddress { get; }
        // List of clients allowed to execute commands. Even unauthorized clients are allowed to establish
        // a connection.
        public List<IPAddress> AuthorizedClients { get; set; }
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
        private static readonly Encoding PROTO_ENCODING = Encoding.ASCII;
        // Use Unix newline like NUT normally does.
        private static readonly string NewLine = "\n";
        private string Password;
        private TcpListener tcpListener;
        List<TcpClient> connectedClients;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;
        #endregion

        public NUTServer(ushort listenPort = DEFAULT_PORT)
        {
            ListenPort = listenPort;
            ListenAddress = IPAddress.Any;
            AuthorizedClients = new List<IPAddress>();
            connectedClients = new List<TcpClient>();
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

        void HandleNewClient(TcpClient newClient)
        {
            // See if this client will be allowed to execute commands.
            IPEndPoint clientEndpoint = (IPEndPoint)newClient.Client.RemoteEndPoint;
            bool isAuthorized = AuthorizedClients.Contains(clientEndpoint.Address);

            NetworkStream clientNetStream = newClient.GetStream();
            StreamReader streamReader = new StreamReader(clientNetStream, PROTO_ENCODING);
            StreamWriter streamWriter = new StreamWriter(clientNetStream, PROTO_ENCODING)
            {
                AutoFlush = true,
                NewLine = NUTServer.NewLine
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
                    if (readLine.Equals("VER"))
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
    }
}
