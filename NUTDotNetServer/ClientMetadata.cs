using System;
using System.Net;
using System.Net.Sockets;

namespace NUTDotNetServer
{
    /// <summary>
    /// Data that collectively represents a single client connection to the server. Copied from the WatsonTcp project:
    /// https://github.com/jchristn/WatsonTcp/blob/master/WatsonTcp/ClientMetadata.cs
    /// </summary>
    class ClientMetadata : IDisposable
    {
        #region Private Members

        private bool disposed = false;

        private TcpClient tcpClient;
        private NetworkStream networkStream;
        //private SslStream sslStream;

        #endregion

        #region Properties

        public TcpClient TcpClient
        {
            get { return tcpClient; }
        }

        public NetworkStream NetworkStream
        {
            get { return networkStream; }
        }

        //public SslStream SslStream
        //{
        //    get { return sslStream; }
        //    set { sslStream = value; }
        //}

        public IPAddress Ip { get; }

        #endregion

        public ClientMetadata(TcpClient client)
        {
            tcpClient = client ?? throw new ArgumentNullException(nameof(client) + " is null.");
            networkStream = tcpClient.GetStream();
            Ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
        }

        #region Private-Methods

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                //if (sslStream != null)
                //{
                //    sslStream.Close();
                //}

                if (networkStream != null)
                {
                    networkStream.Close();
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
            }

            disposed = true;
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
