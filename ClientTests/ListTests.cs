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
            List<UPSVariable> emptyVals = ups.GetVariables();
            Assert.Empty(emptyVals);
            ClearTestData();
        }

        [Fact]
        public void GetUPSVarsList()
        {
            SetupTestData();
            List<UPSVariable> testVars = new List<UPSVariable>()
            {
                new UPSVariable("varName1", VarFlags.String),
                new UPSVariable("varName2", VarFlags.String),
                new UPSVariable("varName3", VarFlags.String)
            };
            testFixture.testServer.UPSs[0].Variables.UnionWith(testVars);
            List<UPSVariable> getVars = testFixture.testClient.GetUPSes()[0].GetVariables();
            ClearTestData();
            Assert.All(getVars, var => testVars.ForEach(tVar => tVar.Name.Equals(var.Name)));
        }

        [Fact]
        public void GetUPSRWsList()
        {
            SetupTestData();
            List<UPSVariable> testVars = new List<UPSVariable>()
            {
                new UPSVariable("varName1", VarFlags.RW),
                new UPSVariable("varName2", VarFlags.RW),
                new UPSVariable("varName3", VarFlags.RW)
            };
            testFixture.testServer.UPSs[0].Variables.UnionWith(testVars);
            List<UPSVariable> getVars = testFixture.testClient.GetUPSes()[0].GetRewritables();
            ClearTestData();
            Assert.All(getVars, var => testVars.Contains(var));
        }

        [Fact]
        public void GetUPSCommandsList()
        {
            SetupTestData();
            Dictionary<string, string> testVars = new Dictionary<string, string>
            {
                { "cmdName1", null },
                { "cmdName2", null },
                { "cmdName3", null }
            };
            testFixture.testServer.UPSs[0].InstantCommands = testVars;
            Dictionary<string, string> getVars = testFixture.testClient.GetUPSes()[0].GetCommands();
            ClearTestData();
            Assert.All(getVars, var => testVars.ContainsKey(var.Key));
        }

        [Fact]
        public void GetUPSEnumerationssList()
        {
            SetupTestData();
            UPSVariable enumVar = new UPSVariable("testEnum", VarFlags.None);
            List<string> enumList = new List<string>
            {
                { "enumVal1" },
                { "enumVal2" },
                { "enumVal3" }
            };
            enumVar.Enumerations.AddRange(enumList);
            testFixture.testServer.UPSs[0].Variables.Add(enumVar);
            List<string> getEnums = testFixture.testClient.GetUPSes()[0].GetEnumerations(enumVar.Name);
            ClearTestData();
            Assert.Equal(enumList, getEnums);
        }

        [Fact]
        public void GetUPSRangesList()
        {
            SetupTestData();
            UPSVariable rangeVar = new UPSVariable("testRange", VarFlags.None);
            List<Tuple<int, int>> testVars = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(3, 4),
                new Tuple<int, int>(5, 6)
            };
            rangeVar.Ranges.AddRange(testVars);
            testFixture.testServer.UPSs[0].Variables.Add(rangeVar);
            List<Tuple<int, int>> getVars = testFixture.testClient.GetUPSes()[0].GetRanges(rangeVar.Name);
            ClearTestData();
            Assert.Equal(testVars, getVars);
        }
    }
}
