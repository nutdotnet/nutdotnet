using System;

namespace NUTDotNetShared
{
    /// <summary>
    /// Contains common information that may be useful throughout the protocol.
    /// </summary>
    public class NUTCommon
    {
        public const ushort DEFAULT_PORT = 3493;
        public static readonly System.Text.Encoding PROTO_ENCODING = System.Text.Encoding.ASCII;
        // NUT protocol uses the Unix newline representation.
        public const string NewLine = "\n";
        // Default string returned from the server when a text field is not filled out
        public const string NULL_TEXT = "Unavailable";
    }
}
