using NUTDotNetClient;
using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Testing
{
    public class ClientTests : IClassFixture<TestFixture>
    {
        // Use a shared server instance for all tests that won't alter conflicting states.
        TestFixture serverFixture;
        readonly ITestOutputHelper output;

        public ClientTests(TestFixture fixture, ITestOutputHelper output)
        {
            serverFixture = fixture;
            this.output = output;
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
            long startTicks = DateTime.Now.Ticks;
            List<ClientUPS> upses = serverFixture.testClient.GetUPSes();
            long stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Empty LIST UPS reponse took {0} ticks.", stopTicks - startTicks);
            Assert.Empty(upses);
            serverFixture.testClient.Disconnect();
        }

        [Fact]
        public void TestGetUPSes()
        {
            serverFixture.testClient.Connect();

            List<AbstractUPS> testData = new List<AbstractUPS> { new AbstractUPS("testups1"), new AbstractUPS("testups2", "test description"),
                    new AbstractUPS("testups3", "test description")};
            serverFixture.testServer.UPSs = testData.ConvertAll(ups => (ServerUPS)ups);
            long startTicks = DateTime.Now.Ticks;
            List<ClientUPS> upses = serverFixture.testClient.GetUPSes();
            long stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Full LIST UPS reponse took {0} ticks.", stopTicks - startTicks);
            startTicks = DateTime.Now.Ticks;
            upses = serverFixture.testClient.GetUPSes();
            stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Cached LIST UPS access took {0} ticks.", stopTicks - startTicks);

            Assert.True(upses.TrueForAll(client => testData.Contains(client)));

            serverFixture.testServer.UPSs.Clear();
            serverFixture.testClient.Disconnect();
            // Reset the client.
            serverFixture.testClient.Dispose();
            serverFixture.testClient = new NUTClient("localhost", serverFixture.testServer.ListenPort);
        }
    }
}
