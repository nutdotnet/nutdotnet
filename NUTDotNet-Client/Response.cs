using System;

namespace NUTDotNetClient
{
    class Response
    {
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
        /// Data provided from the response, if applicable. Note this may be null.
        /// </summary>
        public Object Data { get; private set; }

        /// <summary>
        /// Parse the response from a NUT server, determine if it was successful or not, separate the information, and
        /// return a response object.
        /// </summary>
        /// <param name="timeInitiated"></param>
        /// <param name="timeReceived"></param>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public Response(DateTime timeInitiated, DateTime timeReceived, string responseData)
        {
            TimeInitiated = timeInitiated;
            TimeReceived = timeReceived;

            if (responseData == null || responseData.Equals(String.Empty))
                throw new Exception("Unexpected null or empty response returned.");

            if (responseData.StartsWith("OK"))
            {
                // If there's more to the string than just the response, get that as well.
                if (responseData.Length > 2)
                {
                    Data = responseData.Substring(3);
                }
            }
            else if(responseData.StartsWith("ERR"))
            {
                throw new NUTException(responseData);
            }
            // Likely some complex data was returned. Dump it as-is and send it back for processing.
            else
            {
                Data = responseData;
            }
        }
    }
}
