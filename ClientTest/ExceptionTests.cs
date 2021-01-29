using Xunit;
using NUTDotNetClient;
using System;

namespace ClientTest
{
    public class ExceptionTests
    {
        /// <summary>
        /// Try parsing different valid raw error strings.
        /// </summary>
        /// <param name="rawError"></param>
        /// <param name="expectedEnum"></param>
        [Theory]
        [InlineData("ERR ACCESS-DENIED", NUTException.Error.ACCESSDENIED)]
        [InlineData("ERR UNKNOWN-UPS", NUTException.Error.UNKNOWNUPS)]
        [InlineData("ERR USERNAME-REQUIRED", NUTException.Error.USERNAMEREQUIRED)]
        public void TryParsingValidErrors(string rawError, NUTException.Error expectedEnum)
        {
            try
            {
                throw new NUTException(rawError);
            }
            catch (NUTException ex)
            {
                Assert.Equal(rawError, ex.RawError);
                Assert.Equal(expectedEnum, ex.ErrorCode);
            }
        }

        /// <summary>
        /// Try parsing different invalid raw error strings.
        /// </summary>
        /// <param name="rawError"></param>
        [Theory]
        [InlineData("ERRACCESS-DENIED")]
        [InlineData("ERR ACCESSDENIED")]
        [InlineData("ERR NOT-A-REAL-ERROR")]
        public void TryParsingInvalidErrors(string rawError)
        {
            try
        }

        /// <summary>
        /// Try different raw error strings, expecting some to pass while some should throw an error.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="shouldPass"></param>
        [Theory]
        [InlineData("ERR ACCESS-DENIED", true)]
        [InlineData("ERR UNKNOWN-UPS", true)]
        [InlineData("ERR USERNAME-REQUIRED", true)]
        [InlineData("ERRACCESS-DENIED", false)]
        [InlineData("ERR ACCESSDENIED", false)]
        [InlineData("ERR NOT-A-REAL-ERROR", false)]
        public void TryRawErrors(string errorStr, bool shouldPass, NUTException.Error errorEnum = null)
        {
            try
            {
                throw new NUTException(error);
            }
            catch (NUTException ex)
            {
                if (shouldPass)
                {
                    Assert.Equal(error, ex.RawError);
                    Assert.Equal((NUTException.Error)Enum.Parse(typeof(Error), ))
                }
            }
        }
    }
}
