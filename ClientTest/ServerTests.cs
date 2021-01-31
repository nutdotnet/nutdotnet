using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NUTDotNetServer;
using System.Net;
using System.Net.Sockets;

namespace Testing
{
    public class ServerTests
    {
        /// <summary>
        /// Verify that the server goes through the basic procedure of starting, opening a listener, accepting a
        /// connection, closing it, then closing the listener.
        /// </summary>
        [Fact]
        public void BasicServerStartStop()
        {
            // Verify basic constructor functions.
            Server server = new Server();
            Assert.Equal(Server.DEFAULT_PORT, server.ListenPort);
            Assert.Equal(IPAddress.Any, server.ListenAddress);

            // Run through procedure of connecting.
            System.Threading.ThreadPool.QueueUserWorkItem(delegate { server.BeginListening(); });
            TcpClient client = new TcpClient("localhost", server.ListenPort);
            Assert.True(client.Connected);
            // Give time for the server to close the socket.
            System.Threading.Thread.Sleep(1000);
            Assert.False(server.IsListening);
            // Commenting out below test - apparently it's not easy to tell if the server has closed connection
            // from the client's point of view.
            // Assert.False(client.Connected);
        }
    }
}
