using Xunit;
using NUTDotNetServer;
using NUTDotNetClient;
using NUTDotNetShared;
using System;
using System.Net;
using System.Collections.Generic;

namespace ClientSide
{
    public class ClientTests
    {
        /// <summary>
        /// Test the connection and disconnection logic.
        /// </summary>
        [Fact]
        public void TrySimpleConnection()
        {
            using (NUTServer server = new NUTServer())
            {
                NUTClient client = new NUTClient("localhost", server.ListenPort);
                Assert.Equal("localhost", client.Host);
                Assert.Equal(server.ListenPort, client.Port);
                Assert.False(client.IsConnected);

                // Attempt an incorrect disconnect since we haven't connected yet.
                Assert.Throws<InvalidOperationException>(() => client.Disconnect());

                // Now connect...
                client.Connect();
                Assert.True(client.IsConnected);
                Assert.Equal(server.ServerVersion, client.ServerVersion);
                Assert.Equal(NUTServer.NETVER, client.ProtocolVersion);

                // Try an invalid connect command.
                Assert.Throws<InvalidOperationException>(() => client.Connect());

                // Now disconnect.
                client.Disconnect();
                Assert.False(client.IsConnected);
            }
        }

        [Fact]
        public void TestGetEmptyUPSes()
        {
            using (NUTServer server = new NUTServer())
            {
                NUTClient client = new NUTClient("localhost", server.ListenPort);
                client.Connect();
                List<ClientUPS> upses = client.GetUPSes();
                Assert.Empty(upses);
            }
        }

        [Fact]
        public void TestGetUPSes()
        {
            using (NUTServer server = new NUTServer())
            {
                NUTClient client = new NUTClient("localhost", server.ListenPort);
                client.Connect();

                List<UPS> testData = new List<UPS> { new UPS("testups1"), new UPS("testups2", "test description"),
                    new UPS("testups3", "test description")};
                server.UPSs.AddRange(testData);
                List<ClientUPS> upses = client.GetUPSes();
                Assert.True(testData.TrueForAll((UPS item) => upses.Contains((ClientUPS)item)));
            }
        }
    }
}
