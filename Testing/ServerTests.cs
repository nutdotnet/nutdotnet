using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace ServerMockupTests
{
    class DisposableTestData : IDisposable
    {
        public NUTServer Server;
        public ServerUPS SampleUPS;
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
            testData.Server.UPSs.Add(new ServerUPS("SampleUPS", "A sample UPS."));
            testData.Writer.WriteLine("LIST UPS");
            List<string> response = new List<string>(3);
            for (int i = 0; i <= 2; i++)
            {
                response.Add(testData.Reader.ReadLine());
            }

            Assert.Equal("BEGIN LIST UPS", response[0]);
            Assert.Equal("UPS SampleUPS \"A sample UPS.\"", response[1]);
            Assert.Equal("END LIST UPS", response[2]);
        }

        [Fact]
        public void TestMultipleListUPSResponses()
        {
            using DisposableTestData testData = new DisposableTestData(true);
            List<ServerUPS> testUPSes = new List<ServerUPS>()
            {
                new ServerUPS("TestUPS1", "Test description 1"),
                new ServerUPS("TestUPS2", "Test description 2"),
                new ServerUPS("TestUPS3", null)
            };
            testData.Server.UPSs = testUPSes;
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
                Assert.Equal("UPS " + testUPSes[i - 1].Name + " \"" + testUPSes[i - 1].Description + "\"", response[i]);
            }
        }
    }

    /// <summary>
    /// Tests for the LIST query that utilize a common dictionary data structure.
    /// </summary>
    public class ListDictionaryTests
    {
        [Fact]
        public void TestEmptyQuery()
        {
            using DisposableTestData testData = new DisposableTestData(false);
            testData.Writer.WriteLine("LIST VAR");
            string response = testData.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", response);
            testData.Writer.WriteLine("LIST RW");
            response = testData.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", response);
        }

        [Fact]
        public void TestInvalidUPSName()
        {
            using DisposableTestData testData = new DisposableTestData(false);
            testData.Writer.WriteLine("LIST VAR FOO");
            string response = testData.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-UPS", response);
            testData.Writer.WriteLine("LIST RW FOO");
            response = testData.Reader.ReadLine();
            Assert.Equal("ERR UNKNOWN-UPS", response);
        }

        [Fact]
        public void TestEmptyDictionaries()
        {
            string expectedResponse = "BEGIN LIST VAR SampleUPS\nEND LIST VAR SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(false);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST VAR " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
            expectedResponse = "BEGIN LIST RW SampleUPS\nEND LIST RW SampleUPS\n";
            testData.Writer.WriteLine("LIST RW " + sampleUPS.Name);
            response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestValidDictionaries()
        {
            string expectedResponse = "BEGIN LIST VAR SampleUPS\nVAR SampleUPS testvar \"testval\"\n" +
                "END LIST VAR SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(false);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            sampleUPS.Variables.Add("testvar", "testval");
            sampleUPS.Rewritables.Add("testrw", "testrwval");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST VAR " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
            expectedResponse = "BEGIN LIST RW SampleUPS\nRW SampleUPS testrw \"testrwval\"\n" +
                "END LIST RW SampleUPS\n";
            testData.Writer.WriteLine("LIST RW " + sampleUPS.Name);
            response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }
    }

    /// <summary>
    /// Tests that use the LIST command to get a list of single-value results
    /// </summary>
    public class ListSingleTests
    {
        [Fact]
        public void TestEmptySingles()
        {
            string expectedResponse = "BEGIN LIST CMD SampleUPS\nEND LIST CMD SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(false);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST CMD " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestValidDictionaries()
        {
            string expectedResponse = "BEGIN LIST CMD SampleUPS\nCMD SampleUPS testcmd\n" +
                "END LIST CMD SampleUPS\n";
            string expectedClientResponse = "BEGIN LIST CLIENT SampleUPS\nCLIENT SampleUPS 127.0.0.1\n" +
                "CLIENT SampleUPS ::1\nEND LIST CLIENT SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(false);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            sampleUPS.Commands.Add("testcmd");
            sampleUPS.Clients.Add("127.0.0.1");
            sampleUPS.Clients.Add("::1");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST CMD " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
            testData.Writer.WriteLine("LIST CLIENT " + sampleUPS.Name);
            response = testData.ReadListResponse();
            Assert.Equal(expectedClientResponse, response);
        }
    }

    public class ListEnumTests
    {
        [Fact]
        public void TestEmptyEnumName()
        {
            string expectedResponse = "ERR INVALID-ARGUMENT\n";
            using DisposableTestData testData = new DisposableTestData(true);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST ENUM " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestInvalidEnumName()
        {
            string expectedResponse = "BEGIN LIST ENUM SampleUPS foobar\nEND LIST ENUM SampleUPS foobar\n";
            using DisposableTestData testData = new DisposableTestData(true);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST ENUM " + sampleUPS.Name + " foobar");
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestValidEnumName()
        {
            string expectedResponse = "BEGIN LIST ENUM SampleUPS testenum\nENUM SampleUPS testenum \"1\"\n" +
                "ENUM SampleUPS testenum \"2\"\nENUM SampleUPS testenum \"3\"\nEND LIST ENUM SampleUPS testenum\n";
            using DisposableTestData testData = new DisposableTestData(true);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            sampleUPS.Enumerations.Add("testenum", new List<string> { "1", "2", "3" });
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST ENUM " + sampleUPS.Name + " testenum");
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);

        }
    }

    public class ListRangeTests
    {
        [Fact]
        public void TestEmptyEnumName()
        {
            string expectedResponse = "ERR INVALID-ARGUMENT\n";
            using DisposableTestData testData = new DisposableTestData(true);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST RANGE " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestInvalidEnumName()
        {
            string expectedResponse = "BEGIN LIST RANGE SampleUPS foobar\nEND LIST RANGE SampleUPS foobar\n";
            using DisposableTestData testData = new DisposableTestData(true);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST RANGE " + sampleUPS.Name + " foobar");
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void TestValidEnumName()
        {
            string expectedResponse = "BEGIN LIST RANGE SampleUPS testrange\nRANGE SampleUPS testrange \"1\" \"2\"\n" +
                "RANGE SampleUPS testrange \"3\" \"4\"\nEND LIST RANGE SampleUPS testrange\n";
            using DisposableTestData testData = new DisposableTestData(true);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            sampleUPS.AddRange("testrange", new string[] { "1", "2" });
            sampleUPS.AddRange("testrange", new string[] { "3", "4" });
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST RANGE " + sampleUPS.Name + " testrange");
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);

        }
    }
}
