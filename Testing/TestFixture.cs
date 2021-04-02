using NUTDotNetClient;
using NUTDotNetServer;
using System;

namespace Testing
{
    /// <summary>
    /// Creates a mockup server and connection only once that is available for all tests in this class.
    /// </summary>
    public class TestFixture : IDisposable
    {
        public NUTServer testServer { get; private set; }
        public NUTClient testClient;

        public TestFixture()
        {
            testServer = new NUTServer(0);
            testServer.Start();
            // Just like in the server tests - we need to wait for the TcpListener to find a port before we can use it.
            while (!testServer.IsListening)
            {
                System.Threading.Thread.Sleep(20);
            }
            testClient = new NUTClient("localhost", testServer.ListenPort);
        }

        public void Dispose()
        {
            testClient.Dispose();
            testServer.Stop();
            testServer.Dispose();
        }
    }

}
