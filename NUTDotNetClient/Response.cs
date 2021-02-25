using System;
using System.IO;
using System.Text;

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
        /// Generates a response object.
        /// </summary>
        /// <param name="responseData"></param>
        /// <param name="timeInitiated"></param>
        /// <param name="timeReceived"></param>
        /// <returns></returns>
        public Response(string responseData, DateTime timeInitiated, DateTime timeReceived)
        {
            Data = responseData;
            TimeInitiated = timeInitiated;
            TimeReceived = timeReceived;
        }

        /// <summary>
        /// Try to parse an error string, beginning with ERR, into the associated Error enumeration.
        /// </summary>
        /// <param name="rawError"></param>
        /// <returns></returns>
        public static Error ParseErrorCode(string rawError)
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
