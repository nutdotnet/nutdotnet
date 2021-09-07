using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace NUTDotNetClient
{
    public class NUTClient
    {
        #region Properties
        public string Host { get; }
        public int Port { get; }
        public string Username { get; private set; }
        public string Password { get; private set; }
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
        private TcpClient client;
        private bool disposed;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        private readonly List<ClientUPS> upses;
        #endregion

        /// <summary>
        /// Creates an object allowing for communication with a NUT server.
        /// </summary>
        /// <param name="host">Must be running an instance of a NUT server.</param>
        /// <param name="port"></param>
        public NUTClient(string host, int port = 3493)
        {
            Host = host;
            Port = port;
            upses = new List<ClientUPS>();
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
                if (!(client is null))
                    client.Close();
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

            SendQuery("LOGOUT");
            Username = string.Empty;
            Password = string.Empty;
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
                ServerVersion = SendQuery("VER")[0];
                ProtocolVersion = SendQuery("NETVER")[0];
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
        public List<ClientUPS> GetUPSes(bool forceUpdate = false)
        {
            if (forceUpdate || upses.Count == 0)
            {
                List<string> listUpsResponse = SendQuery("LIST UPS");
                foreach (string line in listUpsResponse)
                {
                    if (line.StartsWith("UPS"))
                    {
                        // Strip out any extraneous quotes
                        string strippedLine = line.Replace("\"", string.Empty);
                        string[] splitLine = strippedLine.Split(new char[] { ' ' }, 3);
                        upses.Add(new ClientUPS(this, splitLine[1], splitLine[2]));
                    }
                }
            }
            
            return upses;
        }

        /// <summary>
        /// Tries to set the username of this connection on the server. Any errors will be thrown. Local Username
        /// property will be set on success. Note: You cannot change the username of this connection after it has
        /// already been set. Reconnect if it needs to be changed.
        /// </summary>
        /// <param name="username"></param>
        public void SetUsername(string username)
        {
            if (!string.IsNullOrEmpty(Username))
                throw new InvalidOperationException("Cannot change username after it's set. Reconnect and try again.");
            string response = SendQuery("USERNAME " + username)[0];
            if (response.Equals("OK"))
                Username = username;
        }

        /// <summary>
        /// Tries to set the password of this connection on the server. Similar to SetUsername, any encountered errors
        /// are thrown, and the local property is set on success. Cannot change this after it's been set.
        /// </summary>
        /// <param name="password"></param>
        public void SetPassword(string password)
        {
            if (!string.IsNullOrEmpty(Password))
                throw new InvalidOperationException("Cannot change password after it's set. Reconnect and try again.");
            string response = SendQuery("PASSWORD " + password)[0];
            if (response.Equals("OK"))
                Password = password;
        }

        /// <summary>
        /// Sends a query to the server, then decides how to handle the response. An error will be thrown if necessary.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<string> SendQuery(string query)
        {
            if (!IsConnected)
                throw new Exception("Attempted to send a query while disconnected.");

            streamWriter.WriteLine(query);
            string readData = streamReader.ReadLine();

            if (readData == null || readData.Equals(String.Empty))
                throw new ArgumentException("Unexpected null or empty response returned.");
            if (readData.StartsWith("ERR "))
            {
                throw new NUTException(readData, Response.ParseErrorCode(readData));
            }

            List<string> returnList = new List<string>() { readData };
            // Multiline response, begin reading in.
            if (readData.StartsWith("BEGIN"))
            {
                while (!readData.StartsWith("END"))
                {
                    readData = streamReader.ReadLine();
                    returnList.Add(readData);
                }
                
            }

            return returnList;
        }
    }
}
