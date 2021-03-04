using System;
using System.Collections.Generic;
using System.Text;

namespace NUTDotNetShared
{
    public class AbstractUPS : IEquatable<AbstractUPS>
    {
        public readonly string Name;
        public readonly string Description;
        public List<string> Clients;
        public List<string> Commands;
        public Dictionary<string, string> Variables;
        public Dictionary<string, string> Rewritables;
        public Dictionary<string, List<string>> Enumerations;
        public readonly Dictionary<string, List<string[]>> Ranges;

        public AbstractUPS(string name, string description = "Unavailable")
        {
            Name = name;
            Description = description;
            Variables = new Dictionary<string, string>();
            Rewritables = new Dictionary<string, string>();
            Commands = new List<string>();
            Enumerations = new Dictionary<string, List<string>>();
            Ranges = new Dictionary<string, List<string[]>>();
            Clients = new List<string>();
        }

        public void AddRange(string name, string[] values)
        {
            if (Ranges.ContainsKey(name))
                Ranges[name].Add(values);
            else
                Ranges[name] = new List<string[]>() { values };
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
    }
}
