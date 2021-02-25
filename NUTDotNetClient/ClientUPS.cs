using System.Collections.Generic;
using NUTDotNetShared;

namespace NUTDotNetClient
{
    /// <summary>
    /// Represents a UPS that is managed by the NUT server. This class provides additional functionality to acquire
    /// data from the server.
    /// </summary>
    public class ClientUPS : UPS
    {
        private NUTClient Client;

        public ClientUPS(NUTClient client, string name, string description) : base(name, description)
        {
            Client = client;
        }
    }
}
