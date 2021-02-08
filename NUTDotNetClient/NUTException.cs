using System;

namespace NUTDotNetClient
{
    /// <summary>
    /// A thrown exception when a valid error code is returned from the NUT server.
    /// </summary>
    public class NUTException : Exception
    {
        public readonly Response.Error ErrorCode;
        // The full error response from the server.
        public readonly string RawError;
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

        //protected NUTException() : base("An undefined exception has occurred.") { }

        /*public NUTException(string rawError) : base(rawError)
        {
            RawError = rawError;
        }*/

        public NUTException(string rawError, Response.Error errorCode) : base()
        {
            RawError = rawError;
            ErrorCode = errorCode;
        }

        /*public NUTException(string rawError, Response.Error errorCode, Exception innerException)
            : base(String.Empty, innerException)
        {
            RawError = rawError;
            ErrorCode = errorCode;
        }*/
    }
}
