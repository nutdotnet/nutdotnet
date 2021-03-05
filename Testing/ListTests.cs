using NUTDotNetClient;
using NUTDotNetServer;
using NUTDotNetShared;
using System.Collections.Generic;
using Xunit;

namespace Testing
{
    public class ListTests : IClassFixture<TestFixture>
    {
        // Use a shared server instance for all tests that won't alter conflicting states.
        TestFixture testFixture;
        ServerUPS referenceUPS = new ServerUPS("TestUPS", "A testing UPS.");

        public ListTests(TestFixture fixture)
        {
            testFixture = fixture;
        }

        [Fact]
        public void GetEmptyVars()
        {
            testFixture.testServer.UPSs.Add(referenceUPS);
            testFixture.testClient.Connect();
            ClientUPS ups = testFixture.testClient.GetUPSes()[0];
            Dictionary<string, string> emptyVals = ups.GetVariables();
            Assert.Empty(emptyVals);
            testFixture.testClient.Disconnect();
            testFixture.testServer.UPSs.Clear();
        }

        [Fact]
        public void GetUPSVarsList()
        {
            Dictionary<string, string> testVars = new Dictionary<string, string>()
            {
                { "varName1", "varValue1" },
                { "varName2", "varValue2" },
                { "varName3", "varValue3" }
            };
            testFixture.testServer.UPSs.Add(referenceUPS);
            testFixture.testServer.UPSs[0].Variables = testVars;
            testFixture.testClient.Connect();
            Dictionary<string, string> getVars = testFixture.testClient.GetUPSes()[0].GetVariables();
            testFixture.testClient.Disconnect();
            testFixture.testClient.Dispose();
            testFixture.testServer.UPSs.Clear();
            testFixture.testClient = new NUTClient("localhost", testFixture.testServer.ListenPort);
            Assert.Equal(testVars, getVars);
        }
    }
}
