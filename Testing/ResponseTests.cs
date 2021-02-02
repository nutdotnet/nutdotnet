using NUTDotNetClient;
using System;
using Xunit;

namespace Testing
{
    public class ResponseTests
    {

        /// <summary>
        /// Try to parse an empty response from the server.
        /// </summary>
        [Fact]
        public void TryParsingEmptyResponse()
        {
            Assert.Throws<ArgumentException>(() => new Response("", DateTime.Now, DateTime.Now));
        }

        /// <summary>
        /// Verify that different time parameters behave as expected.
        /// </summary>
        [Fact]
        public void VerifyTimesBehaviors()
        {
            Response testResponse;
            DateTime sampleTimeStamp = DateTime.Now;

            // Start with null times. Should be default values.
            testResponse = new Response("OK", null, null);
            Assert.Equal(default, testResponse.TimeInitiated);
            Assert.Equal(default, testResponse.TimeReceived);
            // RTT should give an exception here.
            Assert.Throws<Exception>(() => testResponse.RoundTripTime);

            // Try alternating null and valid values for time parameters. Both cases should throw an arg. exception.
            Assert.Throws<ArgumentException>(() => new Response("OK", sampleTimeStamp, null));
            Assert.Throws<ArgumentException>(() => new Response("OK", null, sampleTimeStamp));

            // Try supplying a more recent time for the initiated argument. Should result in arg. exception.
            Assert.Throws<ArgumentException>(() => new Response("OK", sampleTimeStamp.Add(new TimeSpan(1)), sampleTimeStamp));

            // Now try a legitimate set of arguments, and verify the difference.
            testResponse = new Response("OK", sampleTimeStamp, sampleTimeStamp.AddSeconds(1));
            Assert.Equal(sampleTimeStamp, testResponse.TimeInitiated);
            Assert.Equal(sampleTimeStamp.AddSeconds(1), testResponse.TimeReceived);
            Assert.Equal(sampleTimeStamp.AddSeconds(1) - sampleTimeStamp, testResponse.RoundTripTime);
        }

        /// <summary>
        /// Test for basic "OK" response returned from the server. Data object should be null.
        /// </summary>
        [Fact]
        public void TryOKResponse()
        {
            Response okResponse = new Response("OK", DateTime.Now, DateTime.Now);
            Assert.Null(okResponse.Data);
        }

        /// <summary>
        /// Verify that an OK response with extra data is handled correctly.
        /// </summary>
        [Fact]
        public void TryExtraOKResponse()
        {
            // Response received using LOGOUT query
            Response okExtraResponse = new Response("OK Goodbye");
            Assert.NotNull(okExtraResponse.Data);
            Assert.Equal("Goodbye", okExtraResponse.Data);
        }

        /// <summary>
        /// Try sending different valid raw error strings.
        /// </summary>
        /// <param name="rawError"></param>
        /// <param name="expectedEnum"></param>
        [Theory]
        [InlineData("ERR ACCESS-DENIED", Response.Error.ACCESSDENIED)]
        [InlineData("ERR UNKNOWN-UPS", Response.Error.UNKNOWNUPS)]
        [InlineData("ERR USERNAME-REQUIRED", Response.Error.USERNAMEREQUIRED)]
        public void TryValidErrors(string rawError, Response.Error expectedEnum)
        {
            try
            {
                new Response(rawError);
            }
            catch (NUTException ex)
            {
                Assert.Equal(rawError, ex.RawError);
                Assert.Equal(expectedEnum, ex.ErrorCode);
            }
        }

        /// <summary>
        /// Try sending an invalid raw error string.
        /// </summary>
        /// <param name="rawError"></param>
        [Fact]
        public void TryParsingInvalidError()
        {
            Assert.Throws<ArgumentException>(() => new Response("ERR NOT-A-REAL-ERROR"));
        }
    }
}
