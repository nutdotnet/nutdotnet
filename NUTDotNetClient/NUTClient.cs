using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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
                if (nutSocket is null)
                    return false;
                else
                    return nutSocket.Connected;
            }
        }
        public string ServerVersion { get; private set; }
        public string ProtocolVersion { get; private set; }

        /// <summary>
        /// Handle events that may be thrown by the client.
        /// </summary>
        public NUTClientEvents Events { 
            get
            {
                return events;
            }
            private set
            {
                events = value;
            }
        }

        #endregion

        #region Fields

        private bool disposed;
        private readonly List<ClientUPS> upses;
        internal ClientSocket nutSocket;
        private NUTClientEvents events = new NUTClientEvents();

        #endregion

        #region Constructor & Utilities

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
                Debug.WriteLine("NDN client is disposing...");

                if (IsConnected) Disconnect();

                if (!(nutSocket is null))
                    nutSocket.Dispose();
            }

            disposed = true;
        }

        #endregion

        #region Connection

        public void Connect()
        {
            nutSocket.Connect();

            // Verify that the client is allowed access by attempting to get basic data.
            GetBasicDetails();

            events.HandleServerConnected(this, new EventArgs());
        }

        public void Disconnect()
        {
            Username = string.Empty;
            Password = string.Empty;

            nutSocket.Disconnect();
            events.HandleServerDisconnected(this, new EventArgs());
        }

        #endregion

        /// <summary>
        /// Retrieve basic, static details from the NUT server. Also acts to verify that the client is allowed access
        /// to the server, otherwise an access denied error will be returned and we can disconnect.
        /// </summary>
        void GetBasicDetails()
        {
            try
            {
                ServerVersion = nutSocket.SimpleQuery("VER")[0];
                ProtocolVersion = nutSocket.SimpleQuery("NETVER")[0];
            }
            catch (NUTException nutEx)
            {
                /* Access denied error will be thrown right off the bat if the host isn't allowed.
                Specify a friendly error and pass along. */
                if (nutEx.ErrorCode == Response.ResponseStatus.ACCESSDENIED)
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
                List<string[]> listUpsResponse = nutSocket.ListQuery("UPS");
                foreach (string[] line in listUpsResponse)
                {
                    if (line[0].Equals("UPS"))
                        upses.Add(new ClientUPS(this, line[1], line[2]));
                    else
                        throw new Exception("Invalid LIST response line when gathering UPSs:\n" + line.ToString());
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
            string response = nutSocket.SimpleQuery("USERNAME " + username)[0];
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
            string response = nutSocket.SimpleQuery("PASSWORD " + password)[0];
            if (response.Equals("OK"))
                Password = password;
        }
    }
}
