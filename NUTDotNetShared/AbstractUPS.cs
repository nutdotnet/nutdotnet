using System;
using System.Collections.Generic;
using System.Text;

namespace NUTDotNetShared
{
    public class AbstractUPS : IEquatable<AbstractUPS>
    {
        public readonly string Name;
        public readonly string Description;
        protected List<string> clients;
        protected List<string> commands;
        protected Dictionary<string, string> variables;
        protected Dictionary<string, string> rewritables;
        protected Dictionary<string, List<string>> enumerations;
        protected readonly Dictionary<string, List<string[]>> ranges;

        public AbstractUPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
            variables = new Dictionary<string, string>();
            rewritables = new Dictionary<string, string>();
            commands = new List<string>();
            enumerations = new Dictionary<string, List<string>>();
            ranges = new Dictionary<string, List<string[]>>();
            clients = new List<string>();
        }

        public void AddRange(string name, string[] values)
        {
            if (ranges.ContainsKey(name))
                ranges[name].Add(values);
            else
                ranges[name] = new List<string[]>() { values };
        }

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
    }
}
