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
            testServer = new NUTServer();
            testClient = new NUTClient("localhost", testServer.ListenPort);
        }

        public void Dispose()
        {
            testClient.Dispose();
            testServer.Dispose();
        }
    }

}
