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
        TestFixture testFixture;
        readonly ITestOutputHelper output;

        public ClientTests(TestFixture fixture, ITestOutputHelper output)
        {
            testFixture = fixture;
            this.output = output;
        }

        private void SetupTestData()
        {
            testFixture.testClient.Connect();
        }

        private void ClearTestData()
        {
            testFixture.testClient.Disconnect();
            testFixture.testClient.Dispose();
            testFixture.testServer.UPSs.Clear();
            testFixture.testClient = new NUTClient("localhost", testFixture.testServer.ListenPort);
        }

        /// <summary>
        /// Test the connection and disconnection logic.
        /// </summary>
        [Fact]
        public void TrySimpleConnection()
        {
            Assert.Equal("localhost", testFixture.testClient.Host);
            Assert.Equal(testFixture.testServer.ListenPort, testFixture.testClient.Port);
            Assert.False(testFixture.testClient.IsConnected);

            // Attempt an incorrect disconnect since we haven't connected yet.
            Assert.Throws<InvalidOperationException>(() => testFixture.testClient.Disconnect());

            // Now connect...
            testFixture.testClient.Connect();
            Assert.True(testFixture.testClient.IsConnected);
            Assert.Equal(testFixture.testServer.ServerVersion, testFixture.testClient.ServerVersion);
            Assert.Equal(NUTServer.NETVER, testFixture.testClient.ProtocolVersion);

            // Try an invalid connect command.
            Assert.Throws<InvalidOperationException>(() => testFixture.testClient.Connect());

            // Now disconnect.
            testFixture.testClient.Disconnect();
            Assert.False(testFixture.testClient.IsConnected);
        }

        [Fact]
        public void TestUsernameSets()
        {
            string testUser = "testUser";
            SetupTestData();

            Assert.Throws<NUTException>(() => testFixture.testClient.SetUsername(string.Empty));
            testFixture.testClient.SetUsername(testUser);
            Assert.Throws<InvalidOperationException>(() => testFixture.testClient.SetUsername(testUser));
            Assert.Equal(testUser, testFixture.testClient.Username);
            ClearTestData();
        }

        [Fact]
        public void TestPasswordSets()
        {
            string testPass = "testPass";
            SetupTestData();

            Assert.Throws<NUTException>(() => testFixture.testClient.SetPassword(string.Empty));
            testFixture.testClient.SetPassword(testPass);
            Assert.Throws<InvalidOperationException>(() => testFixture.testClient.SetPassword(testPass));
            Assert.Equal(testPass, testFixture.testClient.Password);
            ClearTestData();
        }

        [Fact]
        public void TestGetEmptyUPSes()
        {
            SetupTestData();
            long startTicks = DateTime.Now.Ticks;
            List<ClientUPS> upses = testFixture.testClient.GetUPSes();
            long stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Empty LIST UPS reponse took {0} ticks.", stopTicks - startTicks);
            Assert.Empty(upses);
            ClearTestData();
        }

        [Fact]
        public void TestGetUPSes()
        {
            SetupTestData();

            List<AbstractUPS> testData = new List<AbstractUPS> { new AbstractUPS("testups1"), new AbstractUPS("testups2", "test description"),
                    new AbstractUPS("testups3", "test description")};
            testFixture.testServer.UPSs = testData.ConvertAll(ups => (ups as ServerUPS));
            long startTicks = DateTime.Now.Ticks;
            List<ClientUPS> upses = testFixture.testClient.GetUPSes();
            long stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Full LIST UPS reponse took {0} ticks.", stopTicks - startTicks);
            startTicks = DateTime.Now.Ticks;
            upses = testFixture.testClient.GetUPSes();
            stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Cached LIST UPS access took {0} ticks.", stopTicks - startTicks);

            Assert.True(upses.TrueForAll(client => testData.Contains(client)));

            ClearTestData();
        }

        [Fact]
        public void TestBadInstCmdsQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("InstCmdTestUPS");
            testUPS.Commands.Add("TestCmd", delegate { return; });
            testFixture.testServer.UPSs.Add(testUPS);
            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];

            // Try running a command that's not in the range of available commands.
            Assert.Throws<ArgumentOutOfRangeException>(() => clientUPS.DoInstantCommand(1));
            ClearTestData();
        }

        [Fact]
        public void TestGoodInstCmdQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("InstCmdTestUPS");
            testUPS.Commands.Add("TestCmd", delegate { return; });
            testFixture.testServer.UPSs.Add(testUPS);
            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];

            clientUPS.DoInstantCommand(0);
            ClearTestData();
        }

        [Fact]
        public void TestBadSetVarQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("SetVarTestUPS");
            testUPS.Rewritables.Add("testRW", "initialValue");
            testFixture.testServer.UPSs.Add(testUPS);

            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];
            Assert.Throws<KeyNotFoundException>(() => clientUPS.SetVariable("badVar", "badVal"));
            ClearTestData();
        }

        [Fact]
        public void TestGoodSetVarQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("SetVarTestUPS");
            testUPS.Rewritables.Add("testRW", "initialValue");
            testFixture.testServer.UPSs.Add(testUPS);

            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];
            clientUPS.SetVariable("testRW", "newValue");
            ClearTestData();
            Assert.Equal(testUPS.Rewritables["testRW"], clientUPS.GetRewritables()["testRW"]);
        }
    }
}
