using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NUTDotNetServer
{
    public class Server
    {
        //Specify the network protocol version
        public const string NETVER = "1.2";
        public const ushort DEFAULT_PORT = 3493;
        private static readonly Encoding PROTO_ENCODING = Encoding.ASCII;

        public IPAddress ListenAddress { get; }
        // List of clients allowed to execute commands. Even unauthorized clients are allowed to establish
        // a connection.
        public List<IPAddress> AuthorizedClients { get; set; }
        public ushort ListenPort { get; }
        public string Username { get; }
        private string Password;
        private TcpListener tcpListener;
        public bool IsListening { get { return tcpListener.Server.IsBound; } }
        public string ServerVersion
        {
            get
            {
                var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                return assemblyName.FullName + " " + assemblyName.Version;
            }
        }

        public Server()
        {
            ListenPort = DEFAULT_PORT;
            ListenAddress = IPAddress.Any;
            AuthorizedClients = new List<IPAddress>();

            tcpListener = new TcpListener(IPAddress.Any, ListenPort);
        }

        /// <summary>
        /// Start the listener and begin looping to accept clients.
        /// </summary>
        public void BeginListening()
        {
            if (tcpListener is null)
                throw new InvalidOperationException("TcpListener is null, cannot begin listening.");

            tcpListener.Start();
            while (true)
            {
                // Wait for a connection.
                TcpClient newClient = tcpListener.AcceptTcpClient();
                HandleNewClient(newClient);

                newClient.Close();
                break;
            }
            tcpListener.Stop();
        }

        void HandleNewClient(TcpClient newClient)
        {
            // See if this client will be allowed to execute commands.
            bool isAuthorized = true;
            if (AuthorizedClients.Count > 0)
            {
                IPEndPoint clientEndpoint = (IPEndPoint)newClient.Client.RemoteEndPoint;
                isAuthorized = AuthorizedClients.Contains(clientEndpoint.Address);
            }

            NetworkStream clientNetStream = newClient.GetStream();
            StreamReader streamReader = new StreamReader(clientNetStream, PROTO_ENCODING);
            StreamWriter streamWriter = new StreamWriter(clientNetStream, PROTO_ENCODING);

            // Enter into a loop of listening a responding to queries.
            string readLine;
            while (newClient.Connected && !((readLine = streamReader.ReadLine()) is null))
            {
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

                streamWriter.Flush();
            }
        }
    }
}
