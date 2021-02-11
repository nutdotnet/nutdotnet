using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace NUTDotNetClient
{
    public class NUTClient
    {
        public string Host { get; }
        public ushort Port { get; }
        public string Username { get; }
        // Use the Unix newline representation for NUT compatibility.
        private static readonly string NewLine = "\n";
        private static readonly Encoding ProtocolEncoding = Encoding.ASCII;
        private string Password;
        private TcpClient client;
        public bool IsConnected
        {
            get
            {
                if (client is null)
                    return false;
                else
                    return client.Connected;
            }
        }

        public string ServerVersion { get; private set; }
        public string ProtocolVersion { get; private set; }

        /// <summary>
        /// Creates an object allowing for communication with a NUT server.
        /// </summary>
        /// <param name="host">Must be running an instance of a NUT server.</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="port"></param>
        public NUTClient(string host, string username = "", string password = "", ushort port = 3493)
        {
            Host = host;
            Port = port;
            Username = username;
            Password = password;
        }

        public void Connect()
        {
            if (IsConnected)
                throw new InvalidOperationException("Cannot connect while client is still connected.");

            client = new TcpClient(Host, Port);
            // Verify that the client is allowed access by attempting to get basic data.
            GetBasicDetails();
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Cannot disconnect while client is disconnected.");

            Response attemptDisconnect = SendQuery("LOGOUT");
            client.Close();
        }

        /// <summary>
        /// Retrieve basic, static details from the NUT server. Also acts to verify that the client is allowed access
        /// to the server, otherwise an access denied error will be returned and we can disconnect.
        /// </summary>
        void GetBasicDetails()
        {
            try
            {
                Response getServerVersion = SendQuery("VER");
                ServerVersion = getServerVersion.Data;
                Response getProtocolVersion = SendQuery("NETVER");
                ProtocolVersion = getProtocolVersion.Data;
            }
            catch (NUTException nutEx)
            {
                /* Access denied error will be thrown right off the bat if the host isn't allowed.
                Specify a friendly error and pass along. */
                if (nutEx.ErrorCode == Response.Error.ACCESSDENIED)
                {
                    throw new Exception(
                        "Access is denied. This host, or username/password may not be allowed to run this command.",
                        nutEx);
                }
            }
        }

        /// <summary>
        /// Tells the NUT server that we're depending on it for power, so it will wait for us to disconnect before
        /// shutting down.
        /// </summary>
        private void Login()
        {

            //Response userAuth = SendQuery()
        }

        private Response SendQuery(string query)
        {
            if (!IsConnected)
                throw new Exception("Attempted to send a query while disconnected.");

            string response;
            DateTime querySent;
            DateTime responseReceived;

            NetworkStream stream = client.GetStream();
            StreamWriter streamWriter = new StreamWriter(stream, ProtocolEncoding);
            streamWriter.NewLine = NewLine;
            StreamReader streamReader = new StreamReader(stream, ProtocolEncoding);

            querySent = DateTime.Now;
            streamWriter.WriteLine(query);
            streamWriter.Flush();

            response = streamReader.ReadLine();
            responseReceived = DateTime.Now;

            return new Response(response, querySent, responseReceived);
        }
    }
}
