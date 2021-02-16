using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace ServerMockupTests
{
    class DisposableTestData : IDisposable
    {
        public NUTServer Server;
        public UPS SampleUPS;
        public TcpClient Client;
        public StreamReader Reader;
        public StreamWriter Writer;

        private bool disposed = false;

        public DisposableTestData(bool utilizeSampleUPS)
        {
            SampleUPS = new UPS("SampleUPS", "A sample UPS.");
            Server = new NUTServer(0);
            Debug.WriteLine("Server started in test.");
            if (utilizeSampleUPS)
                Server.UPSs.Add(SampleUPS);

            Client = new TcpClient("localhost", Server.ListenPort);
            Reader = new StreamReader(Client.GetStream());
            Writer = new StreamWriter(Client.GetStream())
            {
                NewLine = NUTCommon.NewLine,
                AutoFlush = true
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Debug.WriteLine("Disposing test data.");
            if (this.disposed)
                return;

            if (disposing)
            {
                Reader.Dispose();
                Writer.Dispose();
                Client.Close();
                Server.Dispose();
            }

            disposed = true;
        }
    }

    public class BaseServerTests
    {
        [Fact]
        public void GetServerVersion()
        {
            Debug.WriteLine("Beginning GetServerVersion test.");
            using DisposableTestData testDat = new DisposableTestData(true);

            testDat.Writer.WriteLine("VER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal(testDat.Server.ServerVersion, result);
        }

        [Fact]
        public void GetNetworkProtocolVersion()
        {
            Debug.WriteLine("Beginning GetNetworkProtocolVersion test.");
            using DisposableTestData testDat = new DisposableTestData(true);

            testDat.Writer.WriteLine("NETVER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal(NUTServer.NETVER, result);
        }

        [Fact]
        public void AttemptIncorrectCommand()
        {
            Debug.WriteLine("Beginning IncorrectCommand test.");
            using DisposableTestData testDat = new DisposableTestData(true);

            testDat.Writer.WriteLine("TRY UNKNOWN COMMAND");
            string result = testDat.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-COMMAND", result);
        }

        /// <summary>
        /// Try adding a bogus IP as authorized, and attempt a command.
        /// </summary>
        [Fact]
        public void TryUnauthedClient()
        {
            Debug.WriteLine("Beginning unathed client test.");
            using DisposableTestData testDat = new DisposableTestData(true);
            testDat.Server.AuthorizedClients.Add(new IPAddress(new byte[] { 192, 0, 2, 0 }));

            testDat.Writer.WriteLine("VER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal("ERR ACCESS-DENIED", result);
            testDat.Writer.WriteLine("LOGOUT");
            result = testDat.Reader.ReadLine();
            Assert.Equal("OK Goodbye", result);
        }
    }

    public class ServerListTests
    {
        [Fact]
        public void TryEmptyListQuery()
        {
            Debug.WriteLine("Beginning LIST Empty query test.");
            using DisposableTestData testDat = new DisposableTestData(true);
            testDat.Writer.WriteLine("LIST ");
            string response = testDat.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-COMMAND", response);
        }
    }
}
