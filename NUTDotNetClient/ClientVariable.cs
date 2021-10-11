using NUTDotNetShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NUTDotNetClient
{
    /// <summary>
    /// A simple variable that mirrors its NUT server counterpart. 
    /// </summary>
    public class ClientVariable : UPSVariable
    {
        #region Properties

        public override string Description
        {
            get
            {
                if (string.IsNullOrEmpty(description))
                    description = GetDescription();

                return description;
            }
        }

        public override VarFlags Flags
        {
            get
            {
                // enum is never null, using "None" for now. Potential problem is UPS vars are always None.
                if (flags == VarFlags.None)
                    flags = GetFlags();

                return flags;
            }
        }

        #endregion

        #region Private fields

        private readonly ClientSocket socket;
        private readonly ClientUPS ups;

        #endregion


        /// <summary>
        /// Instantiate a client-side variable by its name. Other properites should be acquired from the server.
        /// Making internal until I can think of a reason for external apps to be creating their own variables.
        /// </summary>
        /// <param name="name"></param>
        internal ClientVariable(ClientSocket socket, ClientUPS ups, string name) : base(name)
        {
            Debug.WriteLine("Creating new client variable: " + name);

            this.socket = socket;
            this.ups = ups;
        }

        #region Private methods

        /// <summary>
        /// Retrieves the description of a variable from the NUT server.
        /// </summary>
        /// <returns></returns>
        private string GetDescription()
        {
            // Response format: DESC <upsname> <varname> "<description>"
            string[] response = socket.SimpleQuery(string.Format("GET DESC {0} {1}", ups.Name, Name));

            if (response.Length != 4 || !response[0].Equals("DESC") || !response[1].Equals(ups.Name) ||
                    !response[2].Equals(Name))
                throw new Exception("Response from NUT server was unexpected or malformed: " + response.ToString());

            return response[3];
        }

        /// <summary>
        /// Gets the flags (properties) of this variable from the NUT server.
        /// </summary>
        private VarFlags GetFlags()
        {
            VarFlags newFlags = VarFlags.None;
            string[] response = socket.SimpleQuery(string.Format("GET TYPE {0} {1}", ups.Name, Name));

            // Valid response must have more than 3 words, and follow a TYPE ups_name var_name type_1 [type_2] [...] format.
            if (response.Length < 4 || !response[0].Equals("TYPE") || !response[1].Equals(ups.Name) || !response[2].Equals(Name))
                throw new Exception("Unexpected or invalid response from server: " + response.ToString());

            // We can expect at least one type from the server, begin iterating over what's left.
            for (int i = 3; i <= response.Length - 1; i++)
            {
                VarFlags parsedFlag = VarFlags.None;
                // Match the flag
                if (response[i].Equals("RW"))
                    parsedFlag = VarFlags.RW;
                if (response[i].Equals("NUMBER"))
                    parsedFlag = VarFlags.Number;
                // We don't care about string length for now.
                if (response[i].StartsWith("STRING"))
                    parsedFlag = VarFlags.String;

                // Combine parsed flag into our new flag.
                newFlags |= parsedFlag;
            }

            return newFlags;
        }

        #endregion
    }
}
