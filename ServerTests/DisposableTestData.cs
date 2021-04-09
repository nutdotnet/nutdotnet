using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ServerMockupTests
{
    class DisposableTestData : IDisposable
    {
        public NUTServer Server;
        public TcpClient Client;
        public StreamReader Reader;
        public StreamWriter Writer;

        private bool disposed = false;

        public DisposableTestData(bool singleQuery)
        {
            Server = new NUTServer(0, singleQuery);
            Server.Start();
            // Wait for the TcpListener to find a port before we try connecting to it.
            while (!Server.IsListening)
            {
                System.Threading.Thread.Sleep(20);
            }
            Debug.WriteLine("Server started in test.");
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
                Server.Stop();
                Server.Dispose();
            }

            disposed = true;
        }

        public string ReadListResponse()
        {
            StringBuilder sb = new StringBuilder();
            string line;
            while (!Reader.EndOfStream)
            {
                line = Reader.ReadLine();
                sb.Append(line + NUTCommon.NewLine);
                if (line.StartsWith("END") || line.StartsWith("ERR"))
                    break;
            }
            return sb.ToString();
        }
    }
}