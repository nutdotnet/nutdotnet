using NUTDotNetClient;
using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Threading;
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

        private bool VerifyUPSDetails(ServerUPS a, List<ClientUPS> clientList)
        {
            bool foundMatch = false;
            foreach (ClientUPS client in clientList)
            {
                foundMatch = client.Name.Equals(a.Name) && client.Description.Equals(a.Description);
                if (foundMatch)
                    break;
            }
            return foundMatch;
        }

        [Fact]
        public void TestGetUPSes()
        {
            SetupTestData();

            List<ServerUPS> testData = new List<ServerUPS> { new ServerUPS("testups1"), new ServerUPS("testups2", "test description"),
                    new ServerUPS("testups3", "test description")};
            testFixture.testServer.UPSs = testData;
            long startTicks = DateTime.Now.Ticks;
            List<ClientUPS> upses = testFixture.testClient.GetUPSes();
            long stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Full LIST UPS reponse took {0} ticks.", stopTicks - startTicks);
            startTicks = DateTime.Now.Ticks;
            upses = testFixture.testClient.GetUPSes();
            stopTicks = DateTime.Now.Ticks;
            output.WriteLine("Cached LIST UPS access took {0} ticks.", stopTicks - startTicks);

            Assert.All(testData, serverups => VerifyUPSDetails(serverups, upses));

            ClearTestData();
        }

        [Fact]
        public void TestLoggingIn()
        {
            SetupTestData();

            testFixture.testServer.UPSs.Add(new ServerUPS("LogInUPS"));
            testFixture.testClient.SetUsername("user");
            testFixture.testClient.SetPassword("pass");
            testFixture.testClient.GetUPSes()[0].Login();
            Assert.True(testFixture.testClient.GetUPSes()[0].IsLoggedIn);

            ClearTestData();
        }

        [Fact]
        public void TestGetNumLogins()
        {
            SetupTestData();

            testFixture.testServer.UPSs.Add(new ServerUPS("NumLoginsUPS"));
            Assert.Equal(0, testFixture.testClient.GetUPSes()[0].GetNumLogins());

            testFixture.testClient.SetUsername("user");
            testFixture.testClient.SetPassword("pass");
            testFixture.testClient.GetUPSes()[0].Login();
            Assert.True(testFixture.testClient.GetUPSes()[0].IsLoggedIn);
            Assert.Equal(1, testFixture.testClient.GetUPSes()[0].GetNumLogins());

            ClearTestData();
        }

        [Fact]
        public void TestBadInstCmdsQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("InstCmdTestUPS");
            testUPS.InstantCommands.Add("TestCmd", string.Empty);
            testFixture.testServer.UPSs.Add(testUPS);
            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];

            // Try running a command that's not in the range of available commands.
            Assert.Throws<NUTException>(() => clientUPS.DoInstantCommand("FooBar"));
            ClearTestData();
        }

        [Fact]
        public void TestGoodInstCmdQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("InstCmdTestUPS");
            testUPS.InstantCommands.Add("TestCmd", string.Empty);
            testFixture.testServer.UPSs.Add(testUPS);
            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];

            clientUPS.DoInstantCommand("TestCmd");
            ClearTestData();
        }

        [Fact]
        public void TestBadSetVarQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("SetVarTestUPS");
            UPSVariable testRW = new UPSVariable("testRW", VarFlags.RW);
            testRW.Value = "initialValue";
            testUPS.Variables.Add(testRW);
            testFixture.testServer.UPSs.Add(testUPS);

            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];
            Assert.Throws<NUTException>(() => clientUPS.SetVariable("badVar", "badVal"));
            ClearTestData();
        }

        [Fact]
        public void TestGoodSetVarQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("SetVarTestUPS");
            UPSVariable testRW = new UPSVariable("testRW", VarFlags.RW);
            testRW.Value = "initialValue";
            testUPS.Variables.Add(testRW);
            testFixture.testServer.UPSs.Add(testUPS);

            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];
            clientUPS.SetVariable("testRW", "newValue");
            UPSVariable compareVar = clientUPS.GetRewritables()[0];
            ClearTestData();
            Assert.Equal("newValue", compareVar.Value);
        }

        [Fact]
        public void TestGetVarQuery()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("GetVarTestUPS");
            UPSVariable testVar = new UPSVariable("testVar", VarFlags.String);
            testVar.Value = "initialValue";
            testUPS.Variables.Add(testVar);
            testFixture.testServer.UPSs.Add(testUPS);

            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];
            UPSVariable checkVar = clientUPS.GetVariable(testVar.Name);
            Assert.Equal(testVar, checkVar);

            // Modify the value on the server, see that the client picks it up.
            UPSVariable newTestVar = new UPSVariable(testVar.Name, testVar.Flags);
            newTestVar.Value = "newValue";
            testFixture.testServer.UPSs[0].Variables.RemoveWhere(var => var.Name == testVar.Name);
            testFixture.testServer.UPSs[0].Variables.Add(newTestVar);
            // This should still be the old value.
            checkVar = clientUPS.GetVariable(testVar.Name);
            Assert.Equal(testVar, checkVar);
            // Now force update.
            checkVar = clientUPS.GetVariable(testVar.Name, true);
            Assert.Equal(newTestVar, checkVar);

            ClearTestData();
        }

        [Fact]
        public void UpdateFlags()
        {
            SetupTestData();
            ServerUPS testUPS = new ServerUPS("GetTypeTestUPS");
            UPSVariable testNum = new UPSVariable("testNum", VarFlags.RW | VarFlags.Number);
            UPSVariable testRWEnum = new UPSVariable("testRWEnum", VarFlags.RW);
            testRWEnum.Enumerations.Add("firstEnum");
            testUPS.Variables.Add(testNum);
            testUPS.Variables.Add(testRWEnum);
            testFixture.testServer.UPSs.Add(testUPS);

            // Get the variables and verify that they aren't updated yet.
            ClientUPS clientUPS = testFixture.testClient.GetUPSes()[0];
            UPSVariable localTestNum = clientUPS.GetVariable("testNum");
            UPSVariable localTestRWEnum = clientUPS.GetVariable("testRWEnum");
            Assert.NotEqual(localTestNum.Flags, testNum.Flags);
            Assert.NotEqual(localTestRWEnum.Flags, testRWEnum.Flags);

            // Update the flags.
            bool doUpdate = clientUPS.UpdateFlags(ref localTestNum);
            Assert.True(doUpdate);
            doUpdate = clientUPS.UpdateFlags(ref localTestRWEnum);
            Assert.True(doUpdate);

            // Verify the flags are correct.
            Assert.Equal(localTestNum.Flags, testNum.Flags);
            Assert.Equal(localTestRWEnum.Flags, localTestRWEnum.Flags);

            // Try updating again to verify that no update was needed.
            Assert.False(clientUPS.UpdateFlags(ref localTestNum));

            ClearTestData();
        }

        [Fact]
        public void TestClientTimeout()
        {
            SetupTestData();
            testFixture.testServer.ClientTimeout = 2;
            Assert.True(testFixture.testClient.IsConnected);
            Timer waitTimeout = new Timer((object stateInfo) =>
                Assert.False(((NUTClient)stateInfo).IsConnected), testFixture.testClient, 3000, -1);
            ClearTestData();
        }

        [Fact]
        public void TestExtendedTimeout()
        {
            SetupTestData();
            testFixture.testServer.ClientTimeout = 2;
            Assert.True(testFixture.testClient.IsConnected);
            Thread.Sleep(1500);
            Assert.NotEmpty(testFixture.testClient.SendQuery("VER"));
            Thread.Sleep(1500);
            Assert.NotEmpty(testFixture.testClient.SendQuery("VER"));
            Thread.Sleep(1000);
            Assert.True(testFixture.testClient.IsConnected);
            ClearTestData();
        }
    }
}
