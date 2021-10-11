using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUTDotNetClient
{
    /// <summary>
    /// Handle low level socket functionality. Based on WatsonTcp's client design. (https://github.com/jchristn/WatsonTcp)
    /// Connections are asynchronous, but sending & receiving queries is not at this point.
    /// </summary>
    internal class ClientSocket
    {
        #region Properties

        public bool Connected { get; private set; }

        #endregion

        #region Private members

        private TcpClient client;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        private string localIp;
        private int localPort;
        private string destIp;
        private int destPort;

        private NUTClientEvents events;

        // private static int tcpKeepAliveTime = 5, tcpKeepAliveInterval = 5, tcpKeepAliveRetryCount = 3;

        private int connectTimeoutSeconds = 5;

        // Allow only synchronized writing/reading from the datastream.
        private SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        private SemaphoreSlim readLock = new SemaphoreSlim(1, 1);

        private CancellationTokenSource tokenSource;
        private CancellationToken token;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the socket class, but does not connect yet.
        /// </summary>
        /// <param name="serverIP">The server ip.</param>
        /// <param name="serverPort">The server port. 0 (default) indicates autoselecting a port.</param>
        /// <exception cref="System.ArgumentNullException">serverIP</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">serverPort</exception>
        public ClientSocket(string serverIP, int serverPort = 0)
        {
            if (String.IsNullOrEmpty(serverIP)) throw new ArgumentNullException(nameof(serverIP));
            if (serverPort < 0) throw new ArgumentOutOfRangeException(nameof(serverPort));

            destIp = serverIP;
            destPort = serverPort;
        }

        #endregion

        #region Public methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Establishes connection to the server.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Already connected to server.</exception>
        /// <exception cref="System.TimeoutException">Timed out connecting to <see cref="localIp"/>:<see cref="localPort"/></exception>
        public void Connect()
        {
            if (Connected)
                throw new InvalidOperationException("Already connected to server.");

            IAsyncResult asyncConnectResult = null;
            WaitHandle waitHandle = null;
            bool connectSuccess = false;

            // Create a client that's waiting to connect.
            client = new TcpClient(new IPEndPoint(IPAddress.Any, localPort));

            asyncConnectResult = client.BeginConnect(destIp, destPort, null, null);
            waitHandle = asyncConnectResult.AsyncWaitHandle;

            try
            {
                connectSuccess = waitHandle.WaitOne(TimeSpan.FromSeconds(connectTimeoutSeconds), false);

                if (!connectSuccess)
                {
                    client.Close();
                    throw new TimeoutException("Timed out connecting to " + destIp + ":" + destPort);
                }

                client.EndConnect(asyncConnectResult);

                IPEndPoint locEp = (IPEndPoint)client.Client.LocalEndPoint;
                localIp = locEp.Address.ToString();
                localPort = locEp.Port;

                streamWriter = new StreamWriter(client.GetStream(), NUTCommon.PROTO_ENCODING)
                {
                    NewLine = NUTCommon.NewLine,
                    AutoFlush = true
                };
                streamReader = new StreamReader(client.GetStream(), NUTCommon.PROTO_ENCODING);

                Connected = true;
            }
            finally
            {
                waitHandle.Close();
            }

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            //_LastActivity = DateTime.Now;
            //_IsTimeout = false;

            //_DataReceiver = Task.Run(() => DataReceiver(), _Token);
            //_IdleServerMonitor = Task.Run(() => IdleServerMonitor(), _Token);
            //_MonitorSyncResponses = Task.Run(() => MonitorForExpiredSyncResponses(), _Token);
            //_Events.HandleServerConnected(this, new ConnectionEventArgs((_ServerIp + ":" + _ServerPort)));
            //events.HandleServerConnected(this, new EventArgs());
            //_Settings.Logger?.Invoke(Severity.Info, _Header + "connected to " + _ServerIp + ":" + _ServerPort);
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            if (!Connected)
                throw new InvalidOperationException("Cannot disconnect while client is disconnected.");

            // SendQuery("LOGOUT");

            if (tokenSource != null)
            {
                // Stop background tasks if request didn't come from TS.
                if (!tokenSource.IsCancellationRequested)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
            }

            client.Close();
        }

        /// <summary>
        /// Sends a simple query (non-list) to the server and returns the response. Any error is thrown.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>The response, split along spaces.</returns>
        public string[] SimpleQuery(string query)
        {
            Send(query);
            return Read().Split(' ');
        }

        /// <summary>
        /// Sends a LIST query, validates the response and breaks it down for further processing. Each line is broken
        /// into individual strings by spaces, with all doublequote characters removed.
        /// </summary>
        /// <param name="subquery">The second portion of the LIST query, such as VAR.</param>
        /// <param name="parameter">The parameter required for RANGE and ENUM subqueries.</param>
        /// <returns></returns>
        public List<string[]> ListQuery(string subquery, string upsName = null, string parameter = null)
        {
            if (string.IsNullOrEmpty(subquery))
                throw new ArgumentNullException(nameof(subquery));
            StringBuilder queryBuilder = new StringBuilder("LIST " + subquery);

            if (!string.IsNullOrEmpty(upsName))
            {
                queryBuilder.AppendFormat(" {0}", upsName);

                if (!string.IsNullOrEmpty(parameter))
                    queryBuilder.AppendFormat(" {0}", parameter);
            }

            Send(queryBuilder.ToString());

            List<string[]> returnList = new List<string[]>();
            string readData = Read();

            // Gather the response inside of the BEGIN and END lines.
            if (readData.StartsWith("BEGIN " + queryBuilder.ToString()))
            {
                while (!readData.StartsWith("END"))
                {
                    readData = streamReader.ReadLine();

                    // Strip out any double quotes.
                    readData = readData.Replace("\"", string.Empty);
                    string[] splitStr = readData.Split(' ');
                    if (!splitStr[0].Equals(subquery) && !splitStr[1].Equals(upsName) && (!(parameter is null) &&
                        splitStr[2].Equals(subquery)))
                        throw new Exception("Unexpected or invalid response from server: " + splitStr.ToString());
                    returnList.Add(splitStr);
                }

            }
            else
            {
                throw new Exception("Malformed header in response from server.");
            }

            return returnList;
        }

        #endregion

        #region Private methods

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (streamReader != null) streamReader.Dispose();
                if (streamWriter != null) streamWriter.Dispose();
                if (writeLock != null) writeLock.Dispose();
                if (readLock != null) readLock.Dispose();
                if (Connected) Disconnect();

                client = null;
            }
        }

        /// <summary>
        /// Sends a query to the server syncronously.
        /// </summary>
        /// <param name="data">The data.</param>
        private void Send(string data)
        {
            if (!Connected) throw new InvalidOperationException("Cannot send message when disconnected.");

            writeLock.Wait();
            streamWriter.WriteLine(data);
            streamWriter.Flush();
        }

        /// <summary>
        /// Reads in a line from the stream, and does some basic error checking.
        /// </summary>
        /// <returns>The read line from the stream.</returns>
        /// <exception cref="System.ArgumentException">Unexpected null or empty response returned.</exception>
        /// <exception cref="NUTDotNetClient.NUTException">The NUT server threw an error.</exception>
        private string Read()
        {
            string readData = streamReader.ReadLine();

            if (readData == null || readData.Equals(string.Empty))
                throw new ArgumentException("Unexpected null or empty response returned.");
            
            if (readData.StartsWith("ERR "))
            {
                throw new NUTException(readData, Response.ParseErrorCode(readData));
            }

            return readData;
        }

        // Currently not available: https://github.com/dotnet/standard/issues/1769
        //private void EnableKeepAlives()
        //{
        //    throw new NotImplementedException();
        //    try
        //    {
        //        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        //        client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, tcpKeepAliveTime);
        //        client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, tcpKeepAliveInterval);
        //        client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, tcpKeepAliveRetryCount);
        //    }
        //}

        //private async Task DataReceiver()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            if (client == null || !client.Connected)
        //            {
        //                throw new Exception("Attempted to send a query while disconnected.");
        //            }

        //            await readLock.WaitAsync(token);
        //            // Build message
        //        }
        //    }
        //}

        #endregion
    }
}
