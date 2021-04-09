using NUTDotNetServer;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Xunit;

namespace ServerMockupTests
{
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
            testDat.Server.AuthorizedClientAddresses.Add(new IPAddress(new byte[] { 192, 0, 2, 0 }));

            testDat.Writer.WriteLine("VER");
            string result = testDat.Reader.ReadLine();
            Assert.Equal("ERR ACCESS-DENIED", result);
            testDat.Writer.WriteLine("LOGOUT");
            result = testDat.Reader.ReadLine();
            Assert.Equal("OK Goodbye", result);
        }

        [Fact]
        public void TestUsernameQuery()
        {
            using DisposableTestData testDat = new DisposableTestData(false);
            string username = "TestUser";
            string result;

            // Attempt a username query with nothing else.
            testDat.Writer.WriteLine("USERNAME");
            result = testDat.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", result);
            // Attempt username with spaces (invalid).
            testDat.Writer.WriteLine("USERNAME a user");
            result = testDat.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", result);
            // Attempt a valid set.
            testDat.Writer.WriteLine("USERNAME " + username);
            result = testDat.Reader.ReadLine();
            Assert.Equal("OK", result);
            // Attempt to set the username again.
            testDat.Writer.WriteLine("USERNAME " + username);
            result = testDat.Reader.ReadLine();
            Assert.Equal("ERR ALREADY-SET-USERNAME", result);
        }

        [Fact]
        public void TestPasswordQuery()
        {
            using DisposableTestData testDat = new DisposableTestData(false);
            string password = "TestPassword";
            string result;

            // Attempt a password query with nothing else.
            testDat.Writer.WriteLine("PASSWORD");
            result = testDat.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", result);
            // Attempt password with spaces (invalid).
            testDat.Writer.WriteLine("PASSWORD a pass");
            result = testDat.Reader.ReadLine();
            Assert.Equal("ERR INVALID-ARGUMENT", result);
            // Attempt a valid set.
            testDat.Writer.WriteLine("PASSWORD " + password);
            result = testDat.Reader.ReadLine();
            Assert.Equal("OK", result);
            // Attempt to set the password again.
            testDat.Writer.WriteLine("PASSWORD " + password);
            result = testDat.Reader.ReadLine();
            Assert.Equal("ERR ALREADY-SET-PASSWORD", result);
        }

        [Fact]
        public void TestLogins()
        {
            using DisposableTestData testData = new DisposableTestData(false);
            testData.Server.UPSs.Add(new ServerUPS("BadLoginUPS"));

            // Try logging in with no user, pass, or UPS.
            testData.Writer.WriteLine("LOGIN");
            Assert.Equal("ERR INVALID-ARGUMENT", testData.Reader.ReadLine());

            // Try with just user set.
            testData.Writer.WriteLine("USERNAME user");
            Assert.Equal("OK", testData.Reader.ReadLine());
            testData.Writer.WriteLine("LOGIN BadLoginUPS");
            Assert.Equal("ERR ACCESS-DENIED", testData.Reader.ReadLine());

            // Try valid user and pass, but invalid UPS.
            testData.Writer.WriteLine("PASSWORD pass");
            Assert.Equal("OK", testData.Reader.ReadLine());
            testData.Writer.WriteLine("LOGIN foo");
            Assert.Equal("ERR UNKNOWN-UPS", testData.Reader.ReadLine());

            // Now try a valid login.
            testData.Writer.WriteLine("LOGIN BadLoginUPS");
            Assert.Equal("OK", testData.Reader.ReadLine());
            Assert.Equal(((IPEndPoint)testData.Client.Client.LocalEndPoint).Address.ToString(),
                testData.Server.UPSs[0].Clients[0]);

            // Try logging in again.
            testData.Writer.WriteLine("LOGIN BadLoginUPS");
            Assert.Equal("ERR ALREADY-LOGGED-IN", testData.Reader.ReadLine());
        }

        [Fact]
        public void TestLoginAndLogout()
        {
            using DisposableTestData testData = new DisposableTestData(false);
            string localAddress = ((IPEndPoint)testData.Client.Client.LocalEndPoint).Address.ToString();
            testData.Server.UPSs.Add(new ServerUPS("LoginLogout"));

            testData.Writer.WriteLine("USERNAME user");
            Assert.Equal("OK", testData.Reader.ReadLine());
            testData.Writer.WriteLine("PASSWORD pass");
            Assert.Equal("OK", testData.Reader.ReadLine());
            testData.Writer.WriteLine("LOGIN LoginLogout");
            Assert.Equal("OK", testData.Reader.ReadLine());
            Assert.Equal(localAddress, testData.Server.UPSs[0].Clients[0]);
            testData.Writer.WriteLine("LOGOUT");
            Assert.Equal("OK Goodbye", testData.Reader.ReadLine());
            Assert.Empty(testData.Server.UPSs[0].Clients);
        }

        [Fact]
        public void TestTimeout()
        {
            using DisposableTestData testData = new DisposableTestData(false);
            testData.Server.ClientTimeout = 2; //Set to two seconds so we don't need to wait for long.
            Assert.True(testData.Client.Connected);
            System.Threading.Timer waitTimeout = new System.Threading.Timer((object stateInfo) =>
                Assert.False(((TcpClient)stateInfo).Connected), testData.Client, 3000, -1);
        }

        /// <summary>
        /// Ensure that the client isn't kicked while still executing commands.
        /// </summary>
        [Fact]
        public void TestExtendedTimeout()
        {
            using DisposableTestData testData = new DisposableTestData(false);
            testData.Server.ClientTimeout = 2;
            Assert.True(testData.Client.Connected);
            Thread.Sleep(1500);
            testData.Writer.WriteLine("VER");
            Assert.False(string.IsNullOrEmpty(testData.Reader.ReadLine()));
            Thread.Sleep(1500);
            testData.Writer.WriteLine("VER");
            Assert.False(string.IsNullOrEmpty(testData.Reader.ReadLine()));
            Thread.Sleep(1000);
            Assert.True(testData.Client.Connected);
        }
    }
}
