using System;
using System.Collections.Generic;
using System.Linq;

namespace NUTDotNetShared
{
    public abstract class AbstractUPS : IEquatable<AbstractUPS>
    {
        public readonly string Name;
        public readonly string Description;
        protected List<string> clients;
        public List<UPSVariable> Variables;
        public List<string> InstantCommands;

        public AbstractUPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
            Variables = new List<UPSVariable>();
            clients = new List<string>();
            InstantCommands = new List<string>();
        }

        /// <summary>
        /// Returns the first variable/state matching the given name.
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        public UPSVariable GetVariableByName(string varName)
        {
            UPSVariable returnVar;
            returnVar = Variables.Where(var => var.Name.Equals(varName)).First();

            return returnVar;
        }

        public enum VarList
        {
            Variables,
            Rewritables,
            Enumerations,
            Ranges
        }

        public IEnumerable<UPSVariable> GetListOfVariables(VarList listType, string varName = null)
        {
            if (listType == (VarList.Enumerations | VarList.Ranges) && string.IsNullOrWhiteSpace(varName))
                throw new ArgumentNullException("Must provide a variable name for enums or ranges.");
            switch (listType)
            {
                case VarList.Variables:
                    return Variables.Where(v => (v.Flags & (VarFlags.Number | VarFlags.String)) != 0);
                case VarList.Rewritables:
                    return Variables.Where(v => v.Flags == VarFlags.RW);
                case VarList.Enumerations:
                    return Variables.Where(
                        v => !(v.Enumerations is null) && v.Name.Equals(varName) && v.Enumerations.Count > 0);
                case VarList.Ranges:
                    return Variables.Where(v => !(v.Ranges is null) && v.Name.Equals(varName) && v.Ranges.Count > 0);
                default:
                    throw new ArgumentException("Incorrect list type provided.");
            }
        }

        #region Base methods

        public bool Equals(AbstractUPS obj)
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
                hash *= 23 + Description.GetHashCode();
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
