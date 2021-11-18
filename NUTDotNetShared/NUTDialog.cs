using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NUTDotNetShared
{
    /// <summary>
    /// Represents a single conversation between client and server; a <see cref="NUTQuery"/> followed by a
    /// <see cref="Response"/>
    /// </summary>
    public class NUTDialog
    {
        // public enum NUTSubject { NUMLOGINS, UPSDESC, VAR, TYPE, DESC, CMDDESC, UPS, RW, CMD, ENUM, RANGE, CLIENT }

        #region Private Members

        #endregion

        #region Properties

        public NUTQuery Query { get; private set; }        
        /// <summary>
        /// The original Query of the dialog, broken down into components separated by spaces.
        /// </summary>
        // public string[] Query { get; private set; }

        
        //public NUTSubject Subject { get; private set; }
        //public string UPSName { get; private set; }
        //public string VarName { get; private set; }
        //public string Value { get; private set; }
        // public AbstractUPS UPS { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initiates a NUT dialog, beginning with the query.
        /// </summary>
        /// <param name="query"></param>
        public NUTDialog(string query)
        {
            Query = new NUTQuery(query);
        }

        public NUTDialog(NUTQuery query)
        {
            Query = query;
        }

        #endregion

        

        
    }
}
