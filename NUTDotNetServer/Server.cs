using System;
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

            tcpListener = new TcpListener(IPAddress.Any, ListenPort);
        }

        /// <summary>
        /// Start the listener and begin looping to accept and handle clients.
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
                /* NetworkStream clientNetStream = newClient.GetStream();
                StreamReader streamReader = new StreamReader(clientNetStream, PROTO_ENCODING);
                StreamWriter streamWriter = new StreamWriter(clientNetStream, PROTO_ENCODING);

                while (newClient.Connected)
                {
                    string readQuery = streamReader.ReadLine();
                }*/
                // Wait for a bit.
                System.Threading.Thread.Sleep(500);
                newClient.Close();
                break;
            }
            tcpListener.Stop();
        }
    }
}
