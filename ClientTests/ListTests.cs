using NUTDotNetClient;
using NUTDotNetServer;
using NUTDotNetShared;
using System;
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

        private void SetupTestData()
        {
            testFixture.testServer.UPSs.Add(referenceUPS);
            testFixture.testClient.Connect();
        }

        private void ClearTestData()
        {
            testFixture.testClient.Disconnect();
            testFixture.testClient.Dispose();
            testFixture.testServer.UPSs.Clear();
            testFixture.testClient = new NUTClient("localhost", testFixture.testServer.ListenPort);
        }

        [Fact]
        public void GetEmptyVars()
        {
            SetupTestData();
            ClientUPS ups = testFixture.testClient.GetUPSes()[0];
            Dictionary<string, string> emptyVals = ups.GetVariables();
            Assert.Empty(emptyVals);
            ClearTestData();
        }

        [Fact]
        public void GetUPSVarsList()
        {
            SetupTestData();
            Dictionary<string, string> testVars = new Dictionary<string, string>()
            {
                { "varName1", "varValue1" },
                { "varName2", "varValue2" },
                { "varName3", "varValue3" }
            };
            testFixture.testServer.UPSs[0].Variables = testVars;
            Dictionary<string, string> getVars = testFixture.testClient.GetUPSes()[0].GetVariables();
            ClearTestData();
            Assert.Equal(testVars, getVars);
        }

        [Fact]
        public void GetUPSRWsList()
        {
            SetupTestData();
            Dictionary<string, string> testVars = new Dictionary<string, string>()
            {
                { "rwName1", "rwValue1" },
                { "rwName2", "rwValue2" },
                { "rwName3", "rwValue3" }
            };
            testFixture.testServer.UPSs[0].Rewritables = testVars;
            Dictionary<string, string> getVars = testFixture.testClient.GetUPSes()[0].GetRewritables();
            ClearTestData();
            Assert.Equal(testVars, getVars);
        }

        [Fact]
        public void GetUPSCommandsList()
        {
            SetupTestData();
            Dictionary<string, Action> testVars = new Dictionary<string, Action>
            {
                { "cmdName1", null },
                { "cmdName2", null },
                { "cmdName3", null }
            };
            testFixture.testServer.UPSs[0].Commands = testVars;
            List<string> getVars = testFixture.testClient.GetUPSes()[0].GetCommands();
            ClearTestData();
            Assert.All(getVars, var => testVars.ContainsKey(var));
        }

        [Fact]
        public void GetUPSEnumerationssList()
        {
            SetupTestData();
            string propName = "testEnum";
            List<string> testVars = new List<string>
            {
                { "enumVal1" },
                { "enumVal2" },
                { "enumVal3" }
            };
            testFixture.testServer.UPSs[0].Enumerations[propName] = testVars;
            List<string> getVars = testFixture.testClient.GetUPSes()[0].GetEnumerations(propName);
            ClearTestData();
            Assert.Equal(testVars, getVars);
        }

        [Fact]
        public void GetUPSRangesList()
        {
            SetupTestData();
            string propName = "testRange";
            List<string[]> testVars = new List<string[]>
            {
                new string[] { "rangeVal1", "rangeVal2" },
                new string[] { "rangeVal3", "rangeVal4" },
                new string[] { "rangeVal5", "rangeVal6" }
            };
            testVars.ForEach(val => testFixture.testServer.UPSs[0].AddRange(propName, val));
            List<string[]> getVars = testFixture.testClient.GetUPSes()[0].GetRanges(propName);
            ClearTestData();
            Assert.Equal(testVars, getVars);
        }
    }
}
