using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace NUTDotNetClient
{
    public class NUTClient
    {
        #region Properties
        public string Host { get; }
        public int Port { get; }
        public string Username { get; }
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
        #endregion

        #region Fields
        private string Password;
        private TcpClient client;
        private bool disposed;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        #endregion

        /// <summary>
        /// Creates an object allowing for communication with a NUT server.
        /// </summary>
        /// <param name="host">Must be running an instance of a NUT server.</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="port"></param>
        public NUTClient(string host, int port = 3493, string username = "", string password = "")
        {
            Host = host;
            Port = port;
            Username = username;
            Password = password;
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
                streamReader.Close();
                streamWriter.Close();
            }

            disposed = true;
        }

        public void Connect()
        {
            if (IsConnected)
                throw new InvalidOperationException("Cannot connect while client is still connected.");

            client = new TcpClient(Host, Port);
            streamWriter = new StreamWriter(client.GetStream(), NUTCommon.PROTO_ENCODING)
            {
                NewLine = NUTCommon.NewLine,
                AutoFlush = true
            };
            streamReader = new StreamReader(client.GetStream(), NUTCommon.PROTO_ENCODING);
            // Verify that the client is allowed access by attempting to get basic data.
            GetBasicDetails();
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Cannot disconnect while client is disconnected.");

            Response attemptDisconnect = SendQuery("LOGOUT", false);
            streamReader.Close();
            streamWriter.Close();
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
                Response getServerVersion = SendQuery("VER", false);
                ServerVersion = getServerVersion.Data;
                Response getProtocolVersion = SendQuery("NETVER", false);
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
        /// Queries the server for a list of managed UPSes.
        /// </summary>
        /// <returns>A list of UPS objects found on the server, or an empty list.</returns>
        public List<ClientUPS> GetUPSes()
        {
            List<ClientUPS> upses = new List<ClientUPS>();
            Response getQuery = SendQuery("LIST UPS");
            string[] splitResponse = getQuery.Data.Split(Environment.NewLine.ToCharArray());
            foreach (string line in splitResponse)
            {
                if (line.StartsWith("UPS"))
                {
                    string[] splitLine = line.Split(' ');
                    upses.Add(new ClientUPS(this, splitLine[1], splitLine[2]));
                }
            }
            return upses;
        }

        /// <summary>
        /// Tells the NUT server that we're depending on it for power, so it will wait for us to disconnect before
        /// shutting down.
        /// </summary>
        private void Login()
        {

            //Response userAuth = SendQuery()
        }

        private Response SendQuery(string query, bool recordTiming = false)
        {
            if (!IsConnected)
                throw new Exception("Attempted to send a query while disconnected.");
            DateTime timeInitiated = default;
            DateTime timeReceived = default;

            if (recordTiming)
                timeInitiated = DateTime.Now;
            streamWriter.WriteLine(query);
            string readData = streamReader.ReadLine();

            if (readData == null || readData.Equals(String.Empty))
                throw new ArgumentException("Unexpected null or empty response returned.");

            if (readData.StartsWith("OK"))
            {
                // If there's more to the string than just the response, get that as well.
                if (readData.Length > 2)
                {
                    readData = readData.Substring(3);
                }
            }
            else if (readData.StartsWith("ERR "))
            {
                throw new NUTException(readData, Response.ParseErrorCode(readData));
            }
            // Multiline response, begin reading in.
            else if (readData.StartsWith("BEGIN"))
            {
                StringBuilder sb = new StringBuilder(readData);
                while (!readData.StartsWith("END"))
                {
                    readData = streamReader.ReadLine();
                    sb.AppendLine(readData);
                }
                readData = sb.ToString();
            }

            if (recordTiming)
                timeReceived = DateTime.Now;

            return new Response(readData, timeInitiated, timeReceived);
        }
    }
}
