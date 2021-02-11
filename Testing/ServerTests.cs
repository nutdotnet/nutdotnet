using NUTDotNetServer;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Testing
{
    /// <summary>
    /// Creates a mockup server and connection only once that is available for all tests in this class.
    /// </summary>
    /*public class ServerFixture : IDisposable
    {
        public NUTServer testServer { get; private set; }
        private Task serverTask;
        public TcpClient testClient { get; private set; }

        public ServerFixture()
        {
            testServer = new NUTServer();
            testServer.AuthorizedClients.Add(IPAddress.Loopback);
            serverTask = new Task(() => testServer.BeginListening());
            serverTask.Start();
            testClient = new TcpClient("localhost", testServer.ListenPort);
        }

        public void Dispose()
        {
            Stream baseStream = testClient.GetStream();
            StreamReader sr = new StreamReader(baseStream);
            StreamWriter sw = new StreamWriter(baseStream);

            sw.WriteLine("LOGOUT");
            sw.Flush();
            string result = sr.ReadLine();
            Assert.Equal("OK Goodbye", result);

            sr.Close();
            sw.Close();
            testClient.Close();
            serverTask.Wait();
        }
    }*/

    public class ServerTests // : IClassFixture<ServerFixture>
    {
        // Make the public fixture variables accessible
        //ServerFixture serverFixture;

        /*public ServerTests(ServerFixture fixture)
        {
            serverFixture = fixture;
        }*/

        private static NUTServer PrepareServer()
        {
            NUTServer server = new NUTServer();
            server.AuthorizedClients.Add(IPAddress.Loopback);
            return server;
        }

        private static TcpClient PrepareClient(NUTServer server)
        {
            TcpClient client = new TcpClient("localhost", server.ListenPort);
            return client;
        }

        [Fact]
        public void GetServerVersion()
        {
            using (NUTServer server = PrepareServer())
            {
                TcpClient client = PrepareClient(server);
                Stream baseStream = client.GetStream();
                StreamReader sr = new StreamReader(baseStream);
                StreamWriter sw = new StreamWriter(baseStream);

                sw.WriteLine("VER");
                sw.Flush();
                string result = sr.ReadLine();
                Assert.Equal(server.ServerVersion, result);
                client.Close();
            }
        }

        [Fact]
        public void GetNetworkProtocolVersion()
        {
            using (NUTServer server = PrepareServer())
            {
                TcpClient client = PrepareClient(server);
                Stream baseStream = client.GetStream();
                StreamReader sr = new StreamReader(baseStream);
                StreamWriter sw = new StreamWriter(baseStream);

                sw.WriteLine("NETVER");
                sw.Flush();
                string result = sr.ReadLine();
                Assert.Equal(NUTServer.NETVER, result);
                client.Close();
            }
        }

        [Fact]
        public void AttemptIncorrectCommand()
        {
            using (NUTServer server = PrepareServer())
            {
                TcpClient client = PrepareClient(server);
                Stream baseStream = client.GetStream();
                StreamReader sr = new StreamReader(baseStream);
                StreamWriter sw = new StreamWriter(baseStream);

                sw.WriteLine("TRY UNKNOWN COMMAND");
                sw.Flush();
                string result = sr.ReadLine();
                Assert.Equal("ERR UNKNOWN-COMMAND", result);
                client.Close();
            }
        }

        /// <summary>
        /// Try to reestablish the connection after removing localhost as authenticated, and check results.
        /// </summary>
        [Fact]
        public void TryUnauthedClient()
        {
            using (NUTServer server = PrepareServer())
            {
                server.AuthorizedClients.Clear();
                TcpClient client = PrepareClient(server);
                Stream baseStream = client.GetStream();
                StreamReader sr = new StreamReader(baseStream);
                StreamWriter sw = new StreamWriter(baseStream);

                sw.WriteLine("VER");
                sw.Flush();
                string result = sr.ReadLine();
                Assert.Equal("ERR ACCESS-DENIED", result);
                sw.WriteLine("LOGOUT");
                sw.Flush();
                result = sr.ReadLine();
                Assert.Equal("OK Goodbye", result);
                client.Close();
            }
        }
    }
}
