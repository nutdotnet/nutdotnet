using Xunit;
using NUTDotNetServer;
using NUTDotNetClient;
using System;
using System.Net;

namespace Testing
{
    public class ClientTests
    {
        private static NUTServer PrepareServer(bool isAuthorized)
        {
            NUTServer newServer = new NUTServer();

            if (isAuthorized)
                newServer.AuthorizedClients.Add(IPAddress.Loopback);

            return newServer;
        }
        /// <summary>
        /// Test the connection and disconnection logic.
        /// </summary>
        [Fact]
        public void TrySimpleConnection()
        {
            using (NUTServer server = PrepareServer(true))
            {
                NUTClient client = new NUTClient("localhost");
                Assert.Equal("localhost", client.Host);
                Assert.Equal(3493, client.Port);
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
    }
}
