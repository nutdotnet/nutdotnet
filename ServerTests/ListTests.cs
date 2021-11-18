using NUTDotNetServer;
using NUTDotNetShared;
using System;
using System.Collections.Generic;
using Xunit;

namespace ServerMockupTests
{
    public class BasicListTests
    {
        [Fact]
        public void TryEmptyListQuery()
        {
            using DisposableTestData testDat = new DisposableTestData(true);
            testDat.Writer.WriteLine("LIST ");
            string response = testDat.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", response);
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
            UPSVariable sampleVar = new UPSVariable("testvar");
            sampleVar.Value = "testval";
            sampleVar.Flags = VarFlags.String;
            sampleUPS.Variables.Add(sampleVar);
            sampleVar = new UPSVariable("testrw");
            sampleVar.Value = "testrwval";
            sampleVar.Flags = VarFlags.RW;
            sampleUPS.Variables.Add(sampleVar);
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
        public void TestLISTCMD()
        {
            string expectedResponse = "BEGIN LIST CMD SampleUPS\nCMD SampleUPS testcmd\n" +
                "END LIST CMD SampleUPS\n";
            using DisposableTestData testData = new DisposableTestData(false);
            ServerUPS sampleUPS = new ServerUPS("SampleUPS");
            sampleUPS.InstantCommands.Add("testcmd", string.Empty);
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST CMD " + sampleUPS.Name);
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);
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
            UPSVariable sampleVar = new UPSVariable("testenum");
            sampleVar.Enumerations.AddRange(new string[] { "1", "2", "3" });
            sampleUPS.Variables.Add(sampleVar);
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
            UPSVariable sampleVar = new UPSVariable("testrange");
            sampleVar.Ranges.AddRange(new Tuple<int, int>[] { new Tuple<int, int>(1, 2), new Tuple<int, int>(3, 4) });
            sampleUPS.Variables.Add(sampleVar);
            testData.Server.UPSs.Add(sampleUPS);
            testData.Writer.WriteLine("LIST RANGE " + sampleUPS.Name + " testrange");
            string response = testData.ReadListResponse();
            Assert.Equal(expectedResponse, response);

        }
    }
}