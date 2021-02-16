using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace ServerMockupTests
{
    class DisposableTestData : IDisposable
    {
        public NUTServer Server;
        public UPS SampleUPS;
        public TcpClient Client;
        public StreamReader Reader;
        public StreamWriter Writer;

        private bool disposed = false;

        public DisposableTestData(bool singleQuery)
        {
            Server = new NUTServer(0, singleQuery);
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
                Server.Dispose();
            }

            disposed = true;
        }
    }

    public class BaseServerTests
    {
        [Fact]
        public void GetServerVersion()
        {
            using DisposableTestData testDat = new DisposableTestData(true);

            testDat.Writer.WriteLine("VER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal(testDat.Server.ServerVersion, result);
        }

        [Fact]
        public void GetNetworkProtocolVersion()
        {
            using DisposableTestData testDat = new DisposableTestData(true);

            testDat.Writer.WriteLine("NETVER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal(NUTServer.NETVER, result);
        }

        [Fact]
        public void AttemptIncorrectCommand()
        {
            using DisposableTestData testDat = new DisposableTestData(true);

            testDat.Writer.WriteLine("TRY UNKNOWN COMMAND");
            string result = testDat.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-COMMAND", result);
        }

        /// <summary>
        /// Try adding a bogus IP as authorized, and attempt a command.
        /// </summary>
        [Fact]
        public void TryUnauthedClient()
        {
            using DisposableTestData testDat = new DisposableTestData(false);
            testDat.Server.AuthorizedClients.Add(new IPAddress(new byte[] { 192, 0, 2, 0 }));

            testDat.Writer.WriteLine("VER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal("ERR ACCESS-DENIED", result);
            testDat.Writer.WriteLine("LOGOUT");
            result = testDat.Reader.ReadLine();
            Assert.Equal("OK Goodbye", result);
        }
    }

    public class BasicListTests
    {
        [Fact]
        public void TryEmptyListQuery()
        {
            using DisposableTestData testDat = new DisposableTestData(true);
            testDat.Writer.WriteLine("LIST ");
            string response = testDat.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-COMMAND", response);
        }

        [Fact]
        public void TryUnknownListQuery()
        {
            using DisposableTestData testData = new DisposableTestData(true);
            testData.Writer.WriteLine("LIST BADCOMMAND");
            string response = testData.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", response);
        }
    }

    public class ListUPSTests
    {
        [Fact]
        public void TestLegitimateListUPSQuery()
        {
            using DisposableTestData testData = new DisposableTestData(true);
            testData.Server.UPSs.Add(new UPS("SampleUPS", "A sample UPS."));
            testData.Writer.WriteLine("LIST UPS");
            List<string> response = new List<string>(3);
            for (int i = 0; i <= 2; i++)
            {
                response.Add(testData.Reader.ReadLine());
            }

            Assert.Equal("BEGIN LIST UPS", response[0]);
            Assert.Equal(testData.Server.UPSs[0].ToString(), response[1]);
            Assert.Equal("END LIST UPS", response[2]);
        }

        [Fact]
        public void TestMultipleListUPSResponses()
        {
            using DisposableTestData testData = new DisposableTestData(true);
            testData.Server.UPSs.Add(new UPS("TestUPS1", "Test description 1"));
            testData.Server.UPSs.Add(new UPS("TestUPS2", "Test description 2"));
            testData.Server.UPSs.Add(new UPS("TestUPS3", null));
            testData.Writer.WriteLine("LIST UPS");
            List<string> response = new List<string>(5);
            for (int i = 0; i <= 4; i++)
            {
                response.Add(testData.Reader.ReadLine());
            }
            Assert.Equal("BEGIN LIST UPS", response[0]);
            Assert.Equal("END LIST UPS", response[4]);
            for (int i = 1; i <= 3; i++)
            {
                Assert.Equal(testData.Server.UPSs[i - 1].ToString(), response[i]);
            }
        }
    }

    public class ListVarTests
    {
        [Fact]
        public void TestEmptyVarQuery()
        {
            using DisposableTestData testData = new DisposableTestData(true);
            testData.Writer.WriteLine("LIST VAR");
            string response = testData.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", response);
        }

        [Fact]
        public void TestInvalidUPSName()
        {
            using DisposableTestData testData = new DisposableTestData(true);
            testData.Writer.WriteLine("LIST VAR FOO");
            string response = testData.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-UPS", response);
        }

        [Fact]
        public void TestEmptyVars()
        {
            string expectedResponse = "BEGIN LIST VAR SampleUPS\n\nEND LIST VAR SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(true);
            UPS sampleUPS = new UPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST VAR " + sampleUPS.Name);
            string response = testData.Reader.ReadToEnd();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestValidVars()
        {
            string expectedResponse = "BEGIN LIST VAR SampleUPS\nVAR SampleUPS testvar \"testval\"\n" +
                "END LIST VAR SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(true);
            UPS sampleUPS = new UPS("SampleUPS");
            sampleUPS.Variables.Add("testvar", "testval");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST VAR " + sampleUPS.Name);
            string response = testData.Reader.ReadToEnd();
            Assert.Equal(expectedResponse, response);
        }
    }
}
