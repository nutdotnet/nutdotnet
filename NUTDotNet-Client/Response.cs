using System;

namespace NUTDotNetClient
{
    class Response
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
                return TimeReceived - TimeInitiated;
            }
        }

        /// <summary>
        /// If the response received was successful ("OK" or data is returned) this is true. False if ERR is returned.
        /// </summary>
        public bool Success { get; private set; }
        public Error ErrorCode { get; private set; }
        /// <summary>
        /// Data provided from the response, if applicable. Note this may be null.
        /// </summary>
        public Object Data { get; private set; }
        /// <summary>
        /// Extra data provided by the error message. Currently unimplemented in the NUT protocol.
        /// </summary>
        public string ErrorExtraData
        {
            get
            {
                throw new NotImplementedException("This protocol feature is not yet implemented.");
            }
        }

        /// <summary>
        /// Parse the response from a NUT server, determine if it was successful or not, separate the information, and
        /// return a response object.
        /// </summary>
        /// <param name="timeInitiated"></param>
        /// <param name="timeReceived"></param>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public Response ParseResponse(DateTime timeInitiated, DateTime timeReceived, string responseData)
        {
            TimeInitiated = timeInitiated;
            TimeReceived = timeReceived;

            if (responseData == null || responseData.Equals(String.Empty))
                throw new Exception("Unexpected null or empty response returned.");

            if (responseData.StartsWith("OK"))
            {
                Success = true;

                // If there's more to the string than just the response, get that as well.
                if (responseData.Length > 2)
                {
                    Data = responseData.Substring(3);
                }
            }
            else if(responseData.StartsWith("ERR"))
            {
                Success = false;

                // The rest of the string will be hyphens. Strip them out and parse the message into an enum.
                string errMessage = responseData.Substring(4).Replace("-", String.Empty);
                ErrorCode = (Error)Enum.Parse(typeof(Error), errMessage);
            }
            // Likely some complex data was returned. Dump it as-is and send it back for processing.
            else
            {
                Success = true;
                Data = responseData;
            }

            return this;
        }
    }
}
