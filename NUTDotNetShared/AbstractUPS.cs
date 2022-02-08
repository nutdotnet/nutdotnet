using System;
using System.Collections.Generic;
using System.Linq;

namespace NUTDotNetShared
{
    public abstract class AbstractUPS<T> : IEquatable<AbstractUPS<T>> where T : BaseVariable
    {
        #region Properties

        /// <summary>
        /// Name of this UPS. The name should never change during the lifecycle of the object.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get => name;
        }

        public string Description
        {
            get => description;
        }

        public VariableList<T> Variables
        {
            get { return variables; }
        }

        public virtual Dictionary<string, string> InstantCommands
        {
            get => instantCommands;
        }

        #endregion

        #region Private fields

        private readonly string name;
        private string description;
        protected List<string> clients;
        protected VariableList<T> variables;
        protected Dictionary<string, string> instantCommands;

        #endregion

        public AbstractUPS(string name, string description = "Unavailable")
        {
            this.name = name;
            this.description = description;
            // variables = new VariableList<UPSVariable>();
            clients = new List<string>();
            instantCommands = new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns the first variable/state matching the given name.
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        //public virtual UPSVariable GetVariable(string varName)
        //{
        //    UPSVariable returnVar;
        //    returnVar = variables.Where(var => var.Name.Equals(varName)).First();

        //    return returnVar;
        //}

        // public abstract UPSVariable GetVariable(string varName);

        public enum VarList
        {
            Variables,
            Rewritables,
            Enumerations,
            Ranges
        }

        //public IEnumerable<UPSVariable> GetListOfVariables(VarList listType, string varName = null)
        //{
        //    if (listType == (VarList.Enumerations | VarList.Ranges) && string.IsNullOrWhiteSpace(varName))
        //        throw new ArgumentNullException("Must provide a variable name for enums or ranges.");
        //    switch (listType)
        //    {
        //        case VarList.Variables:
        //            return variables.Where(v => (v.Flags & (VarFlags.Number | VarFlags.String)) != 0);
        //        case VarList.Rewritables:
        //            return variables.Where(v => v.Flags == VarFlags.RW);
        //        case VarList.Enumerations:
        //            return variables.Where(
        //                v => !(v.Enumerations is null) && v.Name.Equals(varName) && v.Enumerations.Count > 0);
        //        case VarList.Ranges:
        //            return variables.Where(v => !(v.Ranges is null) && v.Name.Equals(varName) && v.Ranges.Count > 0);
        //        default:
        //            throw new ArgumentException("Incorrect list type provided.");
        //    }
        //}

        #region Base methods

        public bool Equals(AbstractUPS<T> obj)
        {
            if (obj is null)
                return false;
            return (Name == obj.Name &&
                Description == obj.Description);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 53;
                hash *= 23 + Name.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return Name + " \"" + Description + "\"" + NUTCommon.NewLine;
        }

        #endregion
    }
}
