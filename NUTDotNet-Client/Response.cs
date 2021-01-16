using System;
using System.Collections.Generic;
using System.Text;

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
        public DateTime TimeInitiated { get; }
        /// <summary>
        /// The time when this response was received.
        /// </summary>
        public DateTime TimeReceived { get; }
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
        public bool Success { get; }
        public Error ErrorCode { get; }
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
        /// Data provided from the response, if applicable. Note this may be null.
        /// </summary>
        public Object Data { get; }

        public Response ParseResponse(DateTime timeInitiated, DateTime timeReceived, string responseData)
        {

        }
    }
}
