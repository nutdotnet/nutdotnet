using System;
using System.Collections.Generic;
using System.Text;

namespace NUTDotNetClient
{
    class NUTException : Exception
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

        public static Error ErrorCode;
        // The full error response from the server.
        public static string RawError;
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
        /// Try to parse an error string, beginning with ERR, into the associated Error enumeration.
        /// </summary>
        /// <param name="rawError"></param>
        /// <returns></returns>
        private Error ParseErrorCode(string rawError)
        {
            // The rest of the string after ERR will be hyphens. Strip them out and parse the message into an enum.
            string errMessage = rawError.Substring(4).Replace("-", String.Empty);
            return (Error)Enum.Parse(typeof(Error), errMessage);
        }

        protected NUTException() : base("An undefined exception has occurred.") { }

        public NUTException(string rawError) : base(rawError)
        {
            RawError = rawError;
            ErrorCode = ParseErrorCode(rawError);
        }

        public NUTException(string rawError, string message) : base(message)
        {
            RawError = rawError;
            ErrorCode = ParseErrorCode(rawError);
        }

        public NUTException(string rawError, string message, Exception innerException)
            : base(message, innerException)
        {
            RawError = rawError;
            ErrorCode = ParseErrorCode(rawError);
        }
    }
}
