using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NUTDotNetShared
{
    /// <summary>
    /// Provides a searchable list of UPS variables,
    /// with functionality based on the <see cref="KeyedCollection{TKey, TItem}"/>.
    /// List items must derive from the <see cref="BaseVariable"/> type.
    /// </summary>
    public class VariableList<T> : KeyedCollection<string, T> where T : BaseVariable
    {
        /// <summary>
        /// Required override that determines how the KeyedCollection retrieves its keys.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The name of the T-type <see cref="BaseVariable"/>.</returns>
        protected override string GetKeyForItem(T item)
        {
            return item.Name;
        }

        public VariableList<T> GetRewritables()
        {
            return (VariableList<T>)this.Where(v => v.Flags == VarFlags.RW);
        }

        public VariableList<T> GetEnums()
        {
            return (VariableList<T>)this.Where(v => v.Enumerations.Count > 0);
        }

        public VariableList<T> GetRanges()
        {
            return (VariableList<T>)this.Where(v => v.Ranges.Count > 0);
        }
    }
}
