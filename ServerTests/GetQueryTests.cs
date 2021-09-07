using NUTDotNetServer;
using NUTDotNetShared;
using Xunit;

namespace ServerMockupTests
{
    public class GetQueryTests
    {
        DisposableTestData testData;
        ServerUPS testUPS1 = new ServerUPS("TestUPS1", "Test description");
        UPSVariable testVar = new UPSVariable("TestVar1", VarFlags.String);

        public GetQueryTests()
        {
            testData = new DisposableTestData(false);

            testVar.Value = "Test var value";
            testVar.Description = "Test description";
            testUPS1.InstantCommands.Add("Test.instcmd", "Test instcmd desc");
            testUPS1.Variables.Add(testVar);
            testData.Server.UPSs.Add(testUPS1);
        }

        [Fact]
        public void TestBadGetQueries()
        {
            testData.Writer.WriteLine("GET");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());

            testData.Writer.WriteLine("GET FOO");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
        }

        [Fact]
        public void TestNumloginsQuery()
        {
            testData.Writer.WriteLine("USERNAME user");
            Assert.Equal("OK", testData.Reader.ReadLine());
            testData.Writer.WriteLine("PASSWORD pass");
            Assert.Equal("OK", testData.Reader.ReadLine());
            testData.Writer.WriteLine("LOGIN " + testUPS1.Name);
            Assert.Equal("OK", testData.Reader.ReadLine());
            Assert.Single(testData.Server.UPSs[0].Clients);
            testData.Writer.WriteLine("GET NUMLOGINS " + testUPS1.Name);
            Assert.Equal("NUMLOGINS " + testUPS1.Name + " 1", testData.Reader.ReadLine());
        }

        [Fact]
        public void TestUpsDescQuery()
        {
            testData.Writer.WriteLine("GET UPSDESC " + testUPS1.Name);
            string subResponse = testData.Reader.ReadLine().Split('"')[1];
            Assert.Equal(testUPS1.Description, subResponse);
        }

        [Fact]
        public void TestGetVarQueries()
        {
            // Execute bad queries first.
            testData.Writer.WriteLine("GET VAR");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET VAR FOO");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET VAR FOO BAR");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET VAR " + testUPS1.Name);
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET VAR " + testUPS1.Name + " foo");
            Assert.Equal("ERR VAR-NOT-SUPPORTED", testData.Reader.ReadLine());

            // Now test a valid query.

            testData.Writer.WriteLine("GET VAR " + testUPS1.Name + " " + testVar.Name);
            Assert.Equal("VAR " + testUPS1.Name + " " + testVar.Name + " \"" + testVar.Value + "\"",
                testData.Reader.ReadLine());
        }

        [Fact]
        public void TestGetTypeQueries()
        {
            // Execute bad queries first.
            testData.Writer.WriteLine("GET TYPE");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET TYPE FOO");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET TYPE FOO BAR");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET TYPE " + testUPS1.Name);
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET TYPE " + testUPS1.Name + " foo");
            Assert.Equal("ERR VAR-NOT-SUPPORTED", testData.Reader.ReadLine());

            // Valid queries
            testData.Writer.WriteLine("GET TYPE " + testUPS1.Name + " " + testVar.Name);
            Assert.Equal("TYPE " + testUPS1.Name + " " + testVar.Name + " STRING:" + testVar.Value.Length,
                testData.Reader.ReadLine());

            UPSVariable testVar2 = new UPSVariable("TestVar2", VarFlags.None);
            testVar2.Enumerations.Add("testEnum");
            testData.Server.UPSs[0].Variables.Add(testVar2);
            testData.Writer.WriteLine("GET TYPE " + testUPS1.Name + " " + testVar2.Name);
            Assert.Equal("TYPE " + testUPS1.Name + " " + testVar2.Name + " ENUM NUMBER",
                testData.Reader.ReadLine());

            UPSVariable testVar3 = new UPSVariable("TestVar3", VarFlags.Number);
            testVar3.Value = "123";
            testData.Server.UPSs[0].Variables.Add(testVar3);
            testData.Writer.WriteLine("GET TYPE " + testUPS1.Name + " " + testVar3.Name);
            Assert.Equal("TYPE " + testUPS1.Name + " " + testVar3.Name + " NUMBER",
                testData.Reader.ReadLine());
        }

        [Fact]
        public void TestGetDescQueries()
        {
            // Execute bad queries first.
            testData.Writer.WriteLine("GET DESC");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET DESC FOO");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET DESC FOO BAR");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET DESC " + testUPS1.Name);
            Assert.Equal("ERR VAR-NOT-SUPPORTED", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET DESC " + testUPS1.Name + " foo");
            Assert.Equal("ERR VAR-NOT-SUPPORTED", testData.Reader.ReadLine());

            // Now test a valid query.

            testData.Writer.WriteLine("GET DESC " + testUPS1.Name + " " + testVar.Name);
            Assert.Equal("DESC " + testUPS1.Name + " " + testVar.Name + " \"" + testVar.Description + "\"",
                testData.Reader.ReadLine());
        }

        [Fact]
        public void TestGetCmdDescQueries()
        {
            // Execute bad queries first.
            testData.Writer.WriteLine("GET CMDDESC");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET CMDDESC FOO");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET CMDDESC FOO BAR");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET CMDDESC " + testUPS1.Name);
            Assert.Equal("CMDDESC " + testUPS1.Name + "  \"Unavailable\"", testData.Reader.ReadLine());
            testData.Writer.WriteLine("GET CMDDESC " + testUPS1.Name + " foo");
            Assert.Equal("CMDDESC " + testUPS1.Name + " foo \"Unavailable\"", testData.Reader.ReadLine());

            // Now test a valid query.

            testData.Writer.WriteLine("GET CMDDESC " + testUPS1.Name + " Test.instcmd");
            Assert.Equal("CMDDESC " + testUPS1.Name + " Test.instcmd \"Test instcmd desc\"",
                testData.Reader.ReadLine());
        }
    }
}