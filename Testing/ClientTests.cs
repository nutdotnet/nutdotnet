using Xunit;
using NUTDotNetServer;
using NUTDotNetClient;
using NUTDotNetShared;
using System;
using System.Net;
using System.Collections.Generic;

namespace Testing
{
    public class ClientTests : IClassFixture<ServerFixture>
    {
        // Use a shared server instance for all tests that won't alter conflicting states.
        ServerFixture serverFixture;

        public ClientTests(ServerFixture fixture)
        {
            serverFixture = fixture;
        }

        /// <summary>
        /// Test the connection and disconnection logic.
        /// </summary>
        [Fact]
        public void TrySimpleConnection()
        {
            Assert.Equal("localhost", serverFixture.testClient.Host);
            Assert.Equal(serverFixture.testServer.ListenPort, serverFixture.testClient.Port);
            Assert.False(serverFixture.testClient.IsConnected);

            // Attempt an incorrect disconnect since we haven't connected yet.
            Assert.Throws<InvalidOperationException>(() => serverFixture.testClient.Disconnect());

            // Now connect...
            serverFixture.testClient.Connect();
            Assert.True(serverFixture.testClient.IsConnected);
            Assert.Equal(serverFixture.testServer.ServerVersion, serverFixture.testClient.ServerVersion);
            Assert.Equal(NUTServer.NETVER, serverFixture.testClient.ProtocolVersion);

            // Try an invalid connect command.
            Assert.Throws<InvalidOperationException>(() => serverFixture.testClient.Connect());

            // Now disconnect.
            serverFixture.testClient.Disconnect();
            Assert.False(serverFixture.testClient.IsConnected);
        }

        [Fact]
        public void TestGetEmptyUPSes()
        {
            serverFixture.testClient.Connect();
            List<ClientUPS> upses = serverFixture.testClient.GetUPSes();
            Assert.Empty(upses);
            serverFixture.testClient.Disconnect();
        }

        [Fact]
        public void TestGetUPSes()
        {
            serverFixture.testClient.Connect();

            List<UPS> testData = new List<UPS> { new UPS("testups1"), new UPS("testups2", "test description"),
                    new UPS("testups3", "test description")};
            serverFixture.testServer.UPSs.AddRange(testData);
            List<ClientUPS> upses = serverFixture.testClient.GetUPSes();

            Assert.True(upses.TrueForAll(client => testData.Contains(client)));

            serverFixture.testServer.UPSs.Clear();
            serverFixture.testClient.Disconnect();
        }
    }
}
