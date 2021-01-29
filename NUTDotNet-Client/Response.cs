using System;

namespace NUTDotNetClient
{
    public class Response
    {
        public enum Error
        {
            ACCESSDENIED,
            UNKNOWNUPS,
            VARNOTSUPPORTED,
            CMDNOTSUPPORTED,
            INVALIDARGUMENT,
            INSTCMDFAILED,
            SETFAILED,
            READONLY,
            TOOLONG,
            FEATURENOTSUPPORTED,
            FEATURENOTCONFIGURED,
            ALREADYSSLMODE,
            DRIVERNOTCONNECTED,
            DATASTALE,
            ALREADYLOGGEDIN,
            INVALIDPASSWORD,
            ALREADYSETPASSWORD,
            INVALIDUSERNAME,
            ALREADYSETUSERNAME,
            USERNAMEREQUIRED,
            PASSWORDREQUIRED,
            UNKNOWNCOMMAND,
            INVALIDVALUE
        }
        /// <summary>
        /// The time when the associated request was initiated.
        /// </summary>
        public DateTime TimeInitiated { get; private set; }
        /// <summary>
        /// The time when this response was received.
        /// </summary>
        public DateTime TimeReceived { get; private set; }
        /// <summary>
        /// How long it took this response to be received, starting from when the request was initiated.
        /// </summary>
        public TimeSpan RoundTripTime
        {
            get
            {
                if (TimeReceived == default | TimeInitiated == default)
                    throw new Exception("RTT cannot be calculated because no time information was provided.");
                else
                    return TimeReceived - TimeInitiated;
            }
        }
        /// <summary>
        /// Data provided from the response.
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// Parse the response from a NUT server, determine if it was successful or not, separate the information, and
        /// return a response object.
        /// </summary>
        /// <param name="responseData"></param>
        /// <param name="timeInitiated"></param>
        /// <param name="timeReceived"></param>
        /// <returns></returns>
        public Response(string responseData, DateTime? timeInitiated = null, DateTime? timeReceived = null)
        {
            if (responseData == null || responseData.Equals(String.Empty))
                throw new ArgumentException("Unexpected null or empty response returned.");

            if (timeInitiated.HasValue & timeReceived.HasValue)
            {
                TimeInitiated = timeInitiated.Value;
                TimeReceived = timeReceived.Value;

                if (TimeInitiated > TimeReceived)
                    throw new ArgumentException("The time initiated cannot be more recent than the time received.");
            }
            else if (!timeInitiated.HasValue & !timeReceived.HasValue)
            {
                TimeInitiated = default;
                TimeReceived = default;
            }
            else
                throw new ArgumentException("Either both or none of the time values must be provided.");

            if (responseData.StartsWith("OK"))
            {
                // If there's more to the string than just the response, get that as well.
                if (responseData.Length > 2)
                {
                    Data = responseData.Substring(3);
                }
            }
            else if(responseData.StartsWith("ERR "))
            {
                throw new NUTException(responseData, ParseErrorCode(responseData));
            }
            // Likely some complex data was returned. Dump it as-is and send it back for processing.
            else
            {
                Data = responseData;
            }
        }

        /// <summary>
        /// Try to parse an error string, beginning with ERR, into the associated Error enumeration.
        /// </summary>
        /// <param name="rawError"></param>
        /// <returns></returns>
        private Error ParseErrorCode(string rawError)
        {
            if (!rawError.StartsWith("ERR "))
                throw new ArgumentOutOfRangeException("rawError", rawError,
                    "Attempted to parse an error response that does not match the expected pattern.");

            // The rest of the string after ERR will be hyphens. Strip them out and parse the message into an enum.
            string errMessage = rawError.Substring(4).Replace("-", String.Empty);
            return (Error)Enum.Parse(typeof(Error), errMessage);
        }
    }
}
