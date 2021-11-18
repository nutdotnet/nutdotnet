using System;
using System.Text;
using System.Text.RegularExpressions;
using static NUTDotNetShared.NUTCommon;

namespace NUTDotNetShared
{
    public class NUTQuery
    {
        // Match a set of words (seperated by spaces), handling quoted phrases as one unit.
        public static Regex QUERY_MATCH = new Regex(" (?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))", RegexOptions.Compiled);

        public NUTCommand Command { get; private set; }
        public string[] Arguments { get; private set; }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NUTQuery"/> class and parses a NUT protocol query.
        /// </summary>
        /// <param name="query">The query.</param>
        public NUTQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentNullException(nameof(query));

            // Parse the query into its components, including quotation-wrapped phrases.
            string[] splitQuery = QUERY_MATCH.Split(query);
            // Count the number of extra words (arguments) proceeding the command.
            int argumentCount = splitQuery.Length - 1;
            Arguments = new string[argumentCount];

            // Parse the Command section. Throw a nicer exception if something goes wrong.
            try
            {
                Command = (NUTCommand)Enum.Parse(typeof(NUTCommand), splitQuery[0]);
            }
            catch
            {
                throw new ArgumentException(splitQuery[0] + " is not a valid NUTCommand.");
            }

            // Move the arguments into the array property.
            if (argumentCount > 0)
            {
                Array.Copy(splitQuery, 1, Arguments, 0, argumentCount);
            }
        }

        public NUTQuery(NUTCommand command, string[] arguments = null)
        {
            Command = command;

            if (arguments is null)
                Arguments = Array.Empty<string>();
            else
                Arguments = arguments;
        }

        #endregion

        #region Public methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Command.ToString());

            foreach (string arg in Arguments)
            {
                sb.AppendFormat(" {0}", arg);
            }

            return sb.ToString();
            // return base.ToString();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Parses a valid NUT query and fills the object's NUTCommand and Arguments properties.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">The NUT command is invalid.</exception>
        private void ParseQuery(string query)
        {
            

            //if (Query.Length == 2)
            //{
            //    if (Command == NUTCommand.USERNAME || Command == NUTCommand.PASSWORD)
            //        Value = Query[1];
            //    else if (Command == NUTCommand.FSD || Command == NUTCommand.MASTER || Command == NUTCommand.LOGIN)
            //        UPSName = Query[1];
            //    else
            //        throw new Exception("Unrecognized second argument: " + Query[1]);
            //}

            //else if (Query.Length > 2)
            //{
            //    // Handle final non-subject related second arg
            //    if (Command == NUTCommand.INSTCMD)
            //    {
            //        UPSName = Query[1];
            //        Value = Query[2];
            //    }

            //    // Only remaining possbilities are a subject
            //    try
            //    {
            //        Subject = (NUTSubject)Enum.Parse(typeof(NUTSubject), Query[1]);

            //        // Every valid subject besides LIST UPS, 3rd arg is upsname
            //        if (Subject != NUTSubject.UPS)
            //            UPSName = Query[2];

            //        // Every valid subject with four (or five) arguments, 4th is a var(cmd)name.
            //        if (Query.Length > 4)
            //            VarName = Query[3];

            //    }
            //    catch
            //    {
            //        throw new ArgumentException(
            //            Query[1] + " is not a valid subject for the " + Query[0] + " command.");
            //    }
            //}

            //switch (Command)
            //{
            //    case NUTCommand.GET:

            //}
        }

        #endregion
    }
}
