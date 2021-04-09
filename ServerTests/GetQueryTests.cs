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
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());
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
    }
}