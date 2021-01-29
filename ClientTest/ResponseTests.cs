using NUTDotNetClient;
using System;
using Xunit;

namespace ClientTest
{
    public class ResponseTests
    {

        /// <summary>
        /// Try to parse an empty response from the server.
        /// </summary>
        [Fact]
        public void TryParsingEmptyResponse()
        {
            Assert.Throws<Exception>(() => new Response("", DateTime.Now, DateTime.Now));
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
        /// Verify that null times are handled correctly (default to minimums)
        /// </summary>
        [Fact]
        public void TryNullTimes()
        {
            Response nullTimesResponse = new Response("OK", null, null);
            Assert.Equal(nullTimesResponse.TimeInitiated, DateTime.MinValue);
            Assert.Equal(nullTimesResponse.TimeReceived, DateTime.MinValue);
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
        /// Try parsing error responses and see what the results are.
        /// </summary>
        [Theory]
        [InlineData("ERR ACCESS-DENIED", true)]
        [InlineData("ERR UNKNOWN-UPS", true)]
        [InlineData("ERR INVALID-USERNAME", true)]
        [InlineData("ERR NOT-A-REAL-MESSAGE", false)]
        public void TryErrorResponse(string response, bool shouldPass)
        {

        }
    }
}
